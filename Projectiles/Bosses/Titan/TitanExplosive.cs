using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses.Titan
{
    // Shared by the Titan's shells and rockets: on impact the hitbox grows into a round blast for a few ticks,
    // with fire, smoke and a bang. Hurts players only; never breaks blocks.
    public abstract class TitanExplosive : ModProjectile
    {
        protected abstract int BlastRadius { get; }
        private const int ExplosionTicks = 3;

        protected bool Exploded => Projectile.localAI[0] == 1f;

        protected void Explode()
        {
            if (Exploded)
            {
                return;
            }
            Projectile.localAI[0] = 1f;
            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = ExplosionTicks;
            Projectile.Resize(BlastRadius * 2, BlastRadius * 2);

            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < BlastRadius / 2; i++)
            {
                Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 100, default, 1.7f);
                smoke.velocity *= 1.8f;
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 100, default, 2.3f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }
            for (int i = 0; i < 3; i++)
            {
                Gore.NewGore(Projectile.GetSource_Death(), Projectile.Center - new Vector2(16f), Main.rand.NextVector2Circular(2f, 2f), GoreID.Smoke1 + Main.rand.Next(3));
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

        // Round blast: distance from the centre to the nearest point of the target.
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

    // Arm cannon shell: arcs under gravity and explodes where it lands.
    public class TitanShell : TitanExplosive
    {
        public const float Gravity = 0.22f;
        private const int FuseTicks = 240;

        protected override int BlastRadius => 56;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
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
            Projectile.velocity.Y += Gravity;
            Projectile.rotation += Projectile.velocity.X * 0.04f;
            if (Main.rand.NextBool(2))
            {
                Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, -Projectile.velocity * 0.1f, 120, default, 1.1f);
                trail.noGravity = true;
            }
            if (Projectile.timeLeft <= 4)
            {
                Explode();
            }
        }
    }

    // Steam rocket from the Titan's back pod: launches upward, then slowly homes in on its target (ai[0]).
    public class TitanRocket : TitanExplosive
    {
        private const int LaunchTicks = 25;
        private const float HomingSpeed = 5.5f;
        private const float HomingTurn = 0.035f;
        private const int LifeTicks = 360;

        protected override int BlastRadius => 40;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTicks;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            if (Exploded)
            {
                return;
            }
            Projectile.ai[1]++;
            if (Projectile.ai[1] < LaunchTicks)
            {
                Projectile.velocity *= 0.97f;
            }
            else
            {
                Projectile.tileCollide = true;
                Player target = Main.player[(int)Projectile.ai[0]];
                if (target.active && !target.dead)
                {
                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * HomingSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, HomingTurn);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Vector2 tail = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 14f;
            Dust steam = Dust.NewDustPerfect(tail, DustID.Smoke, -Projectile.velocity * 0.2f, 140, default, 1.3f);
            steam.noGravity = true;
            if (Main.rand.NextBool(2))
            {
                Dust flame = Dust.NewDustPerfect(tail, DustID.Torch, -Projectile.velocity * 0.3f, 100, default, 1.1f);
                flame.noGravity = true;
            }
            if (Projectile.timeLeft <= 4)
            {
                Explode();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Exploded)
            {
                return false;
            }
            var texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() / 2f, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            return false;
        }
    }
}
