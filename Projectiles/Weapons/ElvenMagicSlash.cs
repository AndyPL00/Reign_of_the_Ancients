using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reign_of_the_Ancients.DamageClasses;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Crescent of magic released by the Elven Greatsword's right-click swing.
    // Scales with melee + magic damage via MeleeMagicDamageClass.
    public class ElvenMagicSlash : ModProjectile
    {
        private const int Lifetime = 48;      // ~13 px/tick * ~45 ticks = roughly 35-40 tiles of range
        private const int FadeTicks = 14;
        private const int Pierce = 3;
        private const float Drag = 0.985f;
        private const float StartScale = 0.8f;
        private const float EndScale = 1.2f;
        private const float HitLineWidth = 24f;

        private static readonly Color SlashColor = new Color(150, 215, 255);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = ModContent.GetInstance<MeleeMagicDamageClass>();
            Projectile.penetrate = Pierce;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= Drag;

            float life = 1f - Projectile.timeLeft / (float)Lifetime;
            Projectile.scale = MathHelper.Lerp(StartScale, EndScale, life);
            Projectile.Opacity = Projectile.timeLeft < FadeTicks ? Projectile.timeLeft / (float)FadeTicks : 1f;

            Lighting.AddLight(Projectile.Center, SlashColor.ToVector3() * 0.7f * Projectile.Opacity);

            if (Main.rand.NextBool(2))
            {
                Vector2 edge = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2()
                    * Main.rand.NextFloat(-30f, 30f) * Projectile.scale;
                Dust d = Dust.NewDustPerfect(Projectile.Center + edge, DustID.IceTorch,
                    Projectile.velocity * 0.3f, 100, default, 1.1f);
                d.noGravity = true;
            }
        }

        // Hit along the crescent's length (perpendicular to its travel), not just a small box.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 halfLength = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2()
                * texture.Height * 0.5f * Projectile.scale;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - halfLength, Projectile.Center + halfLength, HitLineWidth * Projectile.scale, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 8; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center, DustID.IceTorch, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.2f);
                d.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1f);
                d.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 offset = Projectile.Size * 0.5f - Main.screenPosition;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float fade = 1f - (i + 1f) / (Projectile.oldPos.Length + 1f);
                Color trail = (SlashColor * 0.4f * fade * Projectile.Opacity) with { A = 0 };
                Main.EntitySpriteDraw(texture, Projectile.oldPos[i] + offset, null, trail, Projectile.oldRot[i],
                    origin, Projectile.scale, SpriteEffects.None);
            }

            Color main = (SlashColor * Projectile.Opacity) with { A = 0 };
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, main, Projectile.rotation,
                origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, (Color.White * 0.6f * Projectile.Opacity) with { A = 0 },
                Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None);

            return false;
        }
    }
}
