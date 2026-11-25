using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Common.Visuals;
using Terraria;
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
            // A comet: round glowing head with a soft tail that tapers away behind it.
            // The tail grows over the first few updates so it doesn't appear behind the bow.
            float length = StreakLength * MathHelper.Clamp(Projectile.localAI[0] / 6f, 0.2f, 1f);
            Vector2 head = Projectile.Center - Main.screenPosition;
            float back = Projectile.rotation + MathHelper.Pi;
            BeamRenderer.Capsule(head, back, length, StreakWidth * 2.6f, StreakColor * 0.35f);
            BeamRenderer.Capsule(head, back, length * 0.75f, StreakWidth * 1.4f, StreakColor * 0.9f);
            BeamRenderer.Capsule(head, back, length * 0.45f, StreakWidth * 0.6f, Color.White);
            BeamRenderer.Glow(head, StreakWidth * 5f, StreakColor * 0.6f);
            BeamRenderer.Glow(head, StreakWidth * 2.2f, Color.White);
            return false;
        }
    }
}
