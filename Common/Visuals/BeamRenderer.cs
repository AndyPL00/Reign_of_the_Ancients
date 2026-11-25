using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common.Visuals
{
    // Shared drawing for every beam and aiming line in the mod (Sceptre, Volodar, Archmage, light streaks).
    // Each layer is a soft capsule: BeamCap (rounded end) + stretched BeamBody + BeamCap, so ends are round, never square.
    // Textures are white with an alpha falloff; colours are drawn with A = 0 so layers add up like light.
    public class BeamRenderer : ModSystem
    {
        // Layer widths relative to the beam's nominal width (the soft textures look ~half as wide as they are).
        private const float HazeScale = 2.8f;
        private const float GlowScale = 1.7f;
        private const float BodyScale = 1.05f;
        private const float CoreScale = 0.45f;

        // Motes that stream along the beam from start to end.
        private const float MoteSpacing = 52f;
        private const float MoteSpeed = 700f;     // px per second
        private const float MoteWobble = 0.18f;   // sideways sway, relative to width

        private const int BodyTexWidth = 16, TexHeight = 64, CapTexWidth = 32, GlowTexSize = 64;

        private static Asset<Texture2D> body, cap, glow;

        public override void Load()
        {
            if (Main.dedServ)
            {
                return;
            }
            body = ModContent.Request<Texture2D>("Reign_of_the_Ancients/Common/Visuals/BeamBody");
            cap = ModContent.Request<Texture2D>("Reign_of_the_Ancients/Common/Visuals/BeamCap");
            glow = ModContent.Request<Texture2D>("Reign_of_the_Ancients/Common/Visuals/BeamGlow");
        }

        public override void Unload()
        {
            body = cap = glow = null;
        }

        /// <summary>
        /// A full beam: soft outer haze, coloured glow, body, white-hot core, flowing motes and bright round flares
        /// at the source and at the impact point. <paramref name="start"/> is in world coordinates.
        /// </summary>
        public static void DrawBeam(Vector2 start, float rotation, float length, float width, Color outer, Color core, float opacity, bool flow = true, bool impactFlare = true)
        {
            if (width < 0.5f || opacity <= 0f || length <= 0f)
            {
                return;
            }
            Vector2 s = start - Main.screenPosition;
            Vector2 dir = rotation.ToRotationVector2();
            float time = Main.GlobalTimeWrappedHourly;
            float shimmer = 1f + 0.06f * (float)Math.Sin(time * 25f);

            Capsule(s, rotation, length, width * HazeScale * shimmer, outer * 0.3f * opacity);
            Capsule(s, rotation, length, width * GlowScale, outer * 0.7f * opacity);
            Capsule(s, rotation, length, width * BodyScale, core * 0.9f * opacity);
            Capsule(s, rotation, length, width * CoreScale * shimmer, Color.White * opacity);

            if (flow)
            {
                Vector2 side = dir.RotatedBy(MathHelper.PiOver2);
                float offset = time * MoteSpeed % MoteSpacing;
                for (float d = offset; d < length; d += MoteSpacing)
                {
                    float sway = (float)Math.Sin(d * 0.045f + time * 9f) * width * MoteWobble;
                    float edgeFade = MathHelper.Clamp(Math.Min(d, length - d) / (width * 2f), 0f, 1f);
                    Glow(s + dir * d + side * sway, width * 1.1f, core * 0.55f * opacity * edgeFade);
                }
            }

            // Source flare: the beam erupts from a bright, pulsing orb.
            float pulse = 1f + 0.12f * (float)Math.Sin(time * 18f);
            Glow(s, width * 3.2f * pulse, outer * 0.6f * opacity);
            Glow(s, width * 1.8f * pulse, core * 0.9f * opacity);
            Glow(s, width * 0.9f, Color.White * opacity);

            if (impactFlare)
            {
                Vector2 e = s + dir * length;
                float flicker = 1f + 0.15f * (float)Math.Sin(time * 31f + 1.3f);
                Glow(e, width * 2.6f * flicker, outer * 0.55f * opacity);
                Glow(e, width * 1.3f * flicker, core * 0.8f * opacity);
                Glow(e, width * 0.6f, Color.White * 0.8f * opacity);
            }
        }

        /// <summary>
        /// A thin aiming line with a glowing source and a spark that runs along it toward the target.
        /// <paramref name="progress"/> 0..1 is how close the attack is to firing: the line thickens and brightens.
        /// </summary>
        public static void DrawTelegraph(Vector2 start, float rotation, float length, float width, Color color, float progress, float opacity = 1f)
        {
            if (opacity <= 0f || length <= 0f)
            {
                return;
            }
            Vector2 s = start - Main.screenPosition;
            Vector2 dir = rotation.ToRotationVector2();
            float time = Main.GlobalTimeWrappedHourly;
            float brightness = (0.35f + 0.65f * progress) * opacity;
            float w = width * (0.6f + 0.6f * progress);

            Capsule(s, rotation, length, w * 4f, color * 0.25f * brightness);
            Capsule(s, rotation, length, w * 1.6f, color * brightness);
            Capsule(s, rotation, length, w * 0.6f, Color.White * 0.6f * brightness);

            // Gathering orb at the source that grows as the shot charges.
            Glow(s, w * (5f + 9f * progress), color * 0.7f * brightness);
            Glow(s, w * (2f + 4f * progress), Color.White * 0.8f * brightness);

            // Sparks running down the line show where it is going.
            for (int i = 0; i < 2; i++)
            {
                float t = (time * (1.2f + progress) + i * 0.5f) % 1f;
                Glow(s + dir * length * t, w * 6f, color * 0.8f * brightness * (1f - t));
            }
        }

        /// <summary>
        /// Width multiplier for a beam that just fired: shoots out past full width (the "BOOM"),
        /// then eases back to 1 with a gentle breathing pulse.
        /// </summary>
        public static float FireEnvelope(float ticksSinceFire, float openTicks = 4f, float settleTicks = 10f, float overshoot = 1.8f)
        {
            if (ticksSinceFire < openTicks)
            {
                float t = ticksSinceFire / openTicks;
                return MathHelper.Lerp(0.25f, overshoot, 1f - (1f - t) * (1f - t)); // ease-out
            }
            float settle = MathHelper.Clamp((ticksSinceFire - openTicks) / settleTicks, 0f, 1f);
            float breathing = 1f + 0.05f * (float)Math.Sin(ticksSinceFire * 0.35f);
            return MathHelper.Lerp(overshoot, 1f, settle * settle * (3f - 2f * settle)) * breathing;
        }

        /// <summary>Width multiplier while a beam closes: thins out quickly, like a beam collapsing. t goes 0..1.</summary>
        public static float CloseEnvelope(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return (1f - t) * (1f - t);
        }

        /// <summary>Soft round glow centred on a screen position.</summary>
        public static void Glow(Vector2 screenPos, float diameter, Color color)
        {
            if (glow == null || diameter <= 0f)
            {
                return;
            }
            Main.EntitySpriteDraw(glow.Value, screenPos, null, color with { A = 0 }, 0f, new Vector2(GlowTexSize / 2f),
                diameter / GlowTexSize, SpriteEffects.None);
        }

        /// <summary>One soft capsule layer from <paramref name="s"/> (screen position) along <paramref name="rotation"/>.</summary>
        public static void Capsule(Vector2 s, float rotation, float length, float width, Color color)
        {
            if (body == null || width <= 0f || length <= 0f)
            {
                return;
            }
            color = color with { A = 0 };
            Vector2 dir = rotation.ToRotationVector2();
            float capLength = Math.Min(width * 0.5f, length * 0.5f);
            float yScale = width / TexHeight;
            Vector2 capOrigin = new Vector2(CapTexWidth, TexHeight / 2f); // right edge centre, where it joins the body
            Vector2 capScale = new Vector2(capLength / CapTexWidth, yScale);

            // Rounded start (extends backward from the join point to the start).
            Main.EntitySpriteDraw(cap.Value, s + dir * capLength, null, color, rotation, capOrigin, capScale, SpriteEffects.None);

            float middle = length - capLength * 2f;
            if (middle > 0f)
            {
                Main.EntitySpriteDraw(body.Value, s + dir * capLength, null, color, rotation, new Vector2(0f, TexHeight / 2f),
                    new Vector2(middle / BodyTexWidth, yScale), SpriteEffects.None);
            }

            // Rounded end: same cap turned around.
            Main.EntitySpriteDraw(cap.Value, s + dir * (length - capLength), null, color, rotation + MathHelper.Pi, capOrigin, capScale, SpriteEffects.None);
        }
    }
}
