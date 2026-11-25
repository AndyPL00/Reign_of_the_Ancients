using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Dwarves
{
    // Hostile bomb.
    //   ai[0] = 0: lobbed by the Sapper; arcs under gravity and explodes on hitting a block or a player, or when the fuse runs out.
    //   ai[0] = 1: the Sapper's own last-stand explosion, bigger, goes off immediately.
    // Explosions hurt players only; they never break blocks.
    public class DwarvenBomb : ModProjectile
    {
        public const float ThrownMode = 0f;
        public const float KamikazeMode = 1f;
        public const float Gravity = 0.2f;

        private const int FuseTicks = 150;
        private const int ThrownRadius = 48;
        private const int KamikazeRadius = 76;
        private const int ExplosionTicks = 3;

        private bool Exploded => Projectile.localAI[0] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FuseTicks;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            if (Exploded)
            {
                return;
            }
            if (Projectile.ai[0] == KamikazeMode)
            {
                Explode();
                return;
            }
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = 1f;
                SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
            }

            Projectile.velocity.Y += Gravity;
            Projectile.rotation += Projectile.velocity.X * 0.05f;

            // Burning fuse.
            if (Main.rand.NextBool(2))
            {
                Vector2 fuse = Projectile.Center + new Vector2(0f, -Projectile.height / 2f).RotatedBy(Projectile.rotation);
                Dust d = Dust.NewDustPerfect(fuse, DustID.Torch, Main.rand.NextVector2Circular(1f, 1f), 100, default, 1f);
                d.noGravity = true;
            }
            if (Projectile.timeLeft <= ExplosionTicks + 1)
            {
                Explode();
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Explode();
        }

        // Grows the hitbox into the blast for a few ticks, with fire, smoke and a bang.
        private void Explode()
        {
            if (Exploded)
            {
                return;
            }
            Projectile.localAI[0] = 1f;
            int radius = Projectile.ai[0] == KamikazeMode ? KamikazeRadius : ThrownRadius;
            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = ExplosionTicks;
            Projectile.Resize(radius * 2, radius * 2);

            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            int amount = radius / 2;
            for (int i = 0; i < amount; i++)
            {
                Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.6f);
                smoke.velocity *= 1.6f;
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2.2f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }
            if (!Main.dedServ)
            {
                for (int i = 0; i < 3; i++)
                {
                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center - new Vector2(16f), Main.rand.NextVector2Circular(2f, 2f), GoreID.Smoke1 + Main.rand.Next(3));
                }
            }
        }

        // Only the blast (and the bomb itself) hurts; check by distance to the centre so the square hitbox acts round.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Exploded)
            {
                return null;
            }
            Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.Distance(closest, Projectile.Center) <= Projectile.width / 2f;
        }

        public override bool PreDraw(ref Color lightColor) => !Exploded;
    }
}
