using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses
{
    // Magic circle that grows as a warning, then detonates.
    // ai[0] = 1 skips the warning (used by Volodar's orbs when they burst).
    public class VolodarRune : ModProjectile
    {
        public const int TelegraphTicks = 55;
        public const int ActiveTicks = 12;
        public const float Radius = 56f;

        private static readonly Color RuneColor = new Color(180, 110, 255);

        private int Age => (int)Projectile.localAI[0];
        private bool Active => Age >= TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2);
            Projectile.height = (int)(Radius * 2);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTicks + ActiveTicks;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.ai[0] == 1f && Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = TelegraphTicks;
                Projectile.timeLeft = ActiveTicks;
            }

            if (Age == TelegraphTicks)
            {
                Detonate();
            }

            Projectile.localAI[0]++;
            Projectile.rotation += Active ? 0.2f : 0.03f;
            Lighting.AddLight(Projectile.Center, RuneColor.ToVector3() * (Active ? 1f : 0.4f));
        }

        private void Detonate()
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center);
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            for (int i = 0; i < 24; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(7f, 7f), 100, default, 1.6f);
                d.noGravity = true;
            }
            if (Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center) < 600f)
            {
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 3f, 6f, 10, 600f, FullName));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Active)
            {
                return false;
            }
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.Distance(closest, Projectile.Center) <= Radius;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float baseScale = Radius * 2f / texture.Width;

            if (!Active)
            {
                float progress = Age / (float)TelegraphTicks;
                float pulse = 0.75f + 0.25f * (float)Math.Sin(Age * 0.4f);
                Color color = (RuneColor * (0.35f + 0.5f * progress) * pulse) with { A = 0 };
                Main.EntitySpriteDraw(texture, pos, null, color, Projectile.rotation, origin, baseScale * (0.3f + 0.7f * progress), SpriteEffects.None);
            }
            else
            {
                float t = (Age - TelegraphTicks) / (float)ActiveTicks;
                Color color = (Color.Lerp(Color.White, RuneColor, t) * (1f - t)) with { A = 0 };
                Main.EntitySpriteDraw(texture, pos, null, color, Projectile.rotation, origin, baseScale * (1f + 0.3f * t), SpriteEffects.None);
            }
            return false;
        }
    }
}
