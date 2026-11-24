using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Fast piercing streak of pale light. Shared by:
    //   ai[0] = 0: Elven Bow (wooden arrows become this), ranged damage
    //   ai[0] = 1: Elven Shortsword light-dash, melee damage, shorter range
    // Drawn with plain stretched pixels, so it needs no real texture.
    public class ElvenLightStreak : ModProjectile
    {
        public const float BowMode = 0f;
        public const float DashMode = 1f;

        private const int BowLifetime = 90;      // updates (extraUpdates = 2, so ~30 ticks)
        private const int DashLifetime = 36;
        private const int BowPierce = 3;
        private const int DashPierce = 2;
        private const float StreakLength = 46f;
        private const float StreakWidth = 4f;

        private static readonly Color StreakColor = new Color(150, 215, 255);

        public override string Texture => "Reign_of_the_Ancients/Projectiles/Bosses/VolodarArrow"; // unused, drawn manually

        private bool IsDash => Projectile.ai[0] == DashMode;

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = BowPierce;
            Projectile.timeLeft = BowLifetime;
            Projectile.extraUpdates = 2;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.localAI[0]++; // age in updates
            if (IsDash)
            {
                if (Projectile.localAI[0] == 1f)
                {
                    Projectile.DamageType = DamageClass.Melee;
                    Projectile.penetrate = DashPierce;
                }
                if (Projectile.localAI[0] >= DashLifetime)
                {
                    Projectile.Kill();
                    return;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, StreakColor.ToVector3() * 0.6f);
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, -Projectile.velocity * 0.1f, 100, default, 0.9f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, 1f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Fade in the tail over the first few updates so it doesn't appear behind the bow.
            float age = Projectile.localAI[0];
            float length = StreakLength * MathHelper.Clamp(age / 6f, 0.2f, 1f);
            Vector2 tail = Projectile.Center - Main.screenPosition;
            float back = Projectile.rotation + MathHelper.Pi;
            DrawPixel(tail, back, length, StreakWidth * 2.2f, (StreakColor * 0.35f) with { A = 0 });
            DrawPixel(tail, back, length, StreakWidth, (StreakColor * 0.9f) with { A = 0 });
            DrawPixel(tail, back, length * 0.6f, StreakWidth * 0.4f, Color.White with { A = 0 });
            return false;
        }

        private static void DrawPixel(Vector2 start, float rotation, float length, float thickness, Color color)
        {
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start, new Rectangle(0, 0, 1, 1), color, rotation,
                new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None);
        }
    }
}
