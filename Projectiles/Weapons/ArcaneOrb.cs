using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Volodar's Grimoire: a slow arcane orb that explodes on contact (or when it fizzles out),
    // hitting everything nearby and scattering homing shards.
    public class ArcaneOrb : ModProjectile
    {
        private const int Lifetime = 70;
        private const int ExplosionSize = 96;     // pixels
        private const int ExplosionTicks = 3;
        private const int ShardCount = 4;
        private const float ShardDamageRatio = 0.4f;

        private static readonly Color OrbColor = new Color(170, 100, 255);

        public override string Texture => "Reign_of_the_Ancients/Projectiles/Bosses/VolodarOrb"; // placeholder art

        private bool Exploding => Projectile.localAI[0] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;  // each enemy is hit once (orb or explosion)
        }

        public override void AI()
        {
            if (!Exploding && Projectile.timeLeft <= ExplosionTicks)
            {
                Explode();
            }

            Projectile.rotation += 0.15f;
            Lighting.AddLight(Projectile.Center, OrbColor.ToVector3() * 0.8f);
            if (!Exploding && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.PurpleTorch, Vector2.Zero, 100, default, 1.1f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Explode();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return false;
        }

        // Grows the hitbox for a few ticks so the blast hits everything around it.
        private void Explode()
        {
            if (Exploding)
            {
                return;
            }
            Projectile.localAI[0] = 1f;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = ExplosionTicks;
            Projectile.Resize(ExplosionSize, ExplosionSize);
            Projectile.alpha = 255;

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.6f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 25; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(6f, 6f), 100, default, 1.6f);
                d.noGravity = true;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < ShardCount; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / ShardCount + Main.rand.NextFloat(0.5f)).ToRotationVector2() * 7f;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<ArcaneShard>(),
                        (int)(Projectile.damage * ShardDamageRatio), 1f, Projectile.owner, 0f, 1f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Exploding)
            {
                return false;
            }
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, pos, null, (OrbColor * 0.5f) with { A = 0 }, -Projectile.rotation, origin, 1.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, pos, null, OrbColor with { A = 0 }, Projectile.rotation, origin, 1f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, pos, null, (Color.White * 0.8f) with { A = 0 }, Projectile.rotation, origin, 0.5f, SpriteEffects.None);
            return false;
        }
    }
}
