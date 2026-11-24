using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Small, short-lived crescent from the Elven Broadsword swing. Reuses the Greatsword magic slash art, smaller.
    public class ElvenCrescent : ModProjectile
    {
        private const int Lifetime = 22;      // ~9 px/tick with drag: roughly 10 tiles of range
        private const int FadeTicks = 8;
        private const int Pierce = 2;
        private const float Drag = 0.95f;
        private const float DrawScale = 0.55f;
        private const float HitLineWidth = 16f;

        private static readonly Color CrescentColor = new Color(150, 215, 255);

        public override string Texture => "Reign_of_the_Ancients/Projectiles/Weapons/ElvenMagicSlash";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = Pierce;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= Drag;
            Projectile.Opacity = Projectile.timeLeft < FadeTicks ? Projectile.timeLeft / (float)FadeTicks : 1f;
            Lighting.AddLight(Projectile.Center, CrescentColor.ToVector3() * 0.5f * Projectile.Opacity);
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), DustID.IceTorch,
                    Projectile.velocity * 0.2f, 100, default, 0.9f);
                d.noGravity = true;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 halfLength = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * texture.Height * 0.5f * DrawScale;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - halfLength, Projectile.Center + halfLength, HitLineWidth, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, pos, null, (CrescentColor * Projectile.Opacity) with { A = 0 }, Projectile.rotation, origin, DrawScale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, pos, null, (Color.White * 0.5f * Projectile.Opacity) with { A = 0 }, Projectile.rotation, origin, DrawScale * 0.8f, SpriteEffects.None);
            return false;
        }
    }
}
