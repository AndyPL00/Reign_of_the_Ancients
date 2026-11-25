using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Dwarves
{
    // Hostile hot-metal shot.
    //   ai[0] = 0: blunderbuss pellet: short range, drops a little.
    //   ai[0] = 1: rusted mech bullet: straight and longer range.
    public class DwarvenPellet : ModProjectile
    {
        public const float PelletMode = 0f;
        public const float BulletMode = 1f;

        private const int PelletLifetime = 40;
        private const int BulletLifetime = 120;
        private const float PelletGravity = 0.08f;
        private const float PelletDrag = 0.985f;

        private static readonly Vector3 GlowLight = new Vector3(0.5f, 0.3f, 0.1f);

        private bool IsPellet => Projectile.ai[0] == PelletMode;

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = BulletLifetime;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (IsPellet)
            {
                Projectile.velocity.Y += PelletGravity;
                Projectile.velocity *= PelletDrag;
                if (Projectile.localAI[0] >= PelletLifetime * (1 + Projectile.extraUpdates))
                {
                    Projectile.Kill();
                    return;
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, GlowLight);
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, -Projectile.velocity * 0.1f, 100, default, 0.8f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 3; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, default, 0.9f);
                d.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;
    }
}
