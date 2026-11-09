using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses
{
    // Slow arcane orb that drifts toward ai[0] (player index), then bursts:
    // an instant rune explosion plus a ring of arrows.
    public class VolodarOrb : ModProjectile
    {
        private const int Lifetime = 110;
        private const float DriftSpeed = 3.5f;
        private const float DriftTurn = 0.03f;
        private const int BurstArrows = 8;
        private const float BurstArrowSpeed = 6f;
        private const float BurstArrowDamageRatio = 0.75f;

        private static readonly Color OrbColor = new Color(170, 100, 255);

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[(int)Projectile.ai[0]];
            if (target.active && !target.dead)
            {
                Vector2 wanted = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * DriftSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, DriftTurn);
            }

            Projectile.rotation += 0.1f;
            // Pulses faster as it's about to burst.
            float urgency = 1f - Projectile.timeLeft / (float)Lifetime;
            Projectile.scale = 1f + 0.15f * (float)Math.Sin(Projectile.timeLeft * (0.15f + urgency * 0.5f));

            Lighting.AddLight(Projectile.Center, OrbColor.ToVector3() * 0.8f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(12f, 12f), DustID.PurpleTorch, Vector2.Zero, 100, default, 1.2f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.6f }, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            var source = Projectile.GetSource_FromAI();
            Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<VolodarRune>(),
                Projectile.damage, 0f, Main.myPlayer, 1f);

            int arrowDamage = (int)(Projectile.damage * BurstArrowDamageRatio);
            for (int i = 0; i < BurstArrows; i++)
            {
                Vector2 velocity = (MathHelper.TwoPi * i / BurstArrows).ToRotationVector2() * BurstArrowSpeed;
                Projectile.NewProjectile(source, Projectile.Center, velocity, ModContent.ProjectileType<VolodarArrow>(),
                    arrowDamage, 0f, Main.myPlayer, VolodarArrow.ModeNormal);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, pos, null, (OrbColor * 0.5f) with { A = 0 }, -Projectile.rotation, origin, Projectile.scale * 1.6f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, pos, null, OrbColor with { A = 0 }, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, pos, null, (Color.White * 0.8f) with { A = 0 }, Projectile.rotation, origin, Projectile.scale * 0.5f, SpriteEffects.None);
            return false;
        }
    }
}
