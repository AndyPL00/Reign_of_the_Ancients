using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses
{
    // Volodar's sweeping beam. Follows him, shows a thin warning line, then sweeps through an arc.
    // ai[0] = Volodar's NPC index, ai[1] = starting angle, ai[2] = sweep direction (+1 / -1).
    public class VolodarBeam : ModProjectile
    {
        public const int TelegraphTicks = 35;
        public const int SweepTicks = 70;
        public const int FadeTicks = 10;
        public const int TotalTicks = TelegraphTicks + SweepTicks + FadeTicks;
        public static readonly float SweepArc = MathHelper.ToRadians(110f);

        private const float MaxLength = 1600f;
        private const float Width = 24f;

        // Uses the arrow sprite only as a placeholder; the beam is drawn with plain rectangles.
        public override string Texture => "Reign_of_the_Ancients/Projectiles/Bosses/VolodarArrow";

        private int Age => TotalTicks - Projectile.timeLeft;
        private bool Sweeping => Age >= TelegraphTicks && Age < TelegraphTicks + SweepTicks;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (!owner.active || owner.type != ModContent.NPCType<NPCs.Bosses.Volodar>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            float sweep = MathHelper.Clamp((Age - TelegraphTicks) / (float)SweepTicks, 0f, 1f);
            Projectile.rotation = Projectile.ai[1] + SweepArc * Projectile.ai[2] * sweep;
            Projectile.localAI[1] = BeamLength();

            if (Age == TelegraphTicks)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Pitch = -0.3f }, Projectile.Center);
            }

            if (Sweeping)
            {
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                for (float d = 0f; d < Projectile.localAI[1]; d += 64f)
                {
                    Lighting.AddLight(Projectile.Center + dir * d, 0.5f, 0.3f, 0.9f);
                }
            }
        }

        // Stops at the first solid block.
        private float BeamLength()
        {
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            for (float d = 0f; d < MaxLength; d += 8f)
            {
                Point tile = (Projectile.Center + dir * d).ToTileCoordinates();
                if (WorldGen.SolidTile(tile.X, tile.Y))
                {
                    return d;
                }
            }
            return MaxLength;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Sweeping)
            {
                return false;
            }
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.localAI[1];
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, Width, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float length = Projectile.localAI[1];
            if (Age < TelegraphTicks)
            {
                float progress = Age / (float)TelegraphTicks;
                DrawLine(length, 2f + 2f * progress, new Color(200, 140, 255, 0) * (0.3f + 0.5f * progress));
            }
            else
            {
                float fade = Sweeping ? 1f : 1f - (Age - TelegraphTicks - SweepTicks) / (float)FadeTicks;
                DrawLine(length, Width * 1.7f, new Color(140, 70, 255, 0) * 0.45f * fade);
                DrawLine(length, Width, new Color(200, 130, 255, 0) * 0.85f * fade);
                DrawLine(length, Width * 0.35f, new Color(255, 255, 255, 0) * fade);
            }
            return false;
        }

        private void DrawLine(float length, float thickness, Color color)
        {
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, Projectile.Center - Main.screenPosition, new Rectangle(0, 0, 1, 1),
                color, Projectile.rotation, new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None);
        }
    }
}
