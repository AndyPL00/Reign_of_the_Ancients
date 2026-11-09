using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Glowing arcane arrow fired by Volodar's Bow every few shots. Splits into homing shards when it hits
    // something or after flying a short distance. ai[0] = 1 disables splitting (Spectral Sigil's archer).
    public class ArcaneArrow : ModProjectile
    {
        private const int SplitAfterTicks = 30;
        private const int ShardCount = 3;
        private const float ShardSpread = 0.5f;          // radians between shards
        private const float ShardDamageRatio = 0.5f;

        private static readonly Color ArrowColor = new Color(190, 130, 255);

        public override string Texture => "Reign_of_the_Ancients/Projectiles/Bosses/VolodarArrow"; // placeholder art

        private bool CanSplit => Projectile.ai[0] != 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.arrow = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, ArrowColor.ToVector3() * 0.7f);
            Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, -Projectile.velocity * 0.2f, 100, default, 1.2f);
            d.noGravity = true;

            if (CanSplit && ++Projectile.localAI[0] >= SplitAfterTicks)
            {
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.6f }, Projectile.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(3f, 3f), 100, default, 1.3f);
                d.noGravity = true;
            }

            if (!CanSplit || Projectile.owner != Main.myPlayer)
            {
                return;
            }
            float start = -ShardSpread * (ShardCount - 1) / 2f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 8f;
            for (int i = 0; i < ShardCount; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, forward.RotatedBy(start + ShardSpread * i),
                    ModContent.ProjectileType<ArcaneShard>(), (int)(Projectile.damage * ShardDamageRatio), 1f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() / 2f;
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float fade = 1f - (i + 1f) / (Projectile.oldPos.Length + 1f);
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, pos, null, (ArrowColor * 0.5f * fade) with { A = 0 }, Projectile.rotation, origin, 1f, SpriteEffects.None);
            }
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(texture, center, null, ArrowColor with { A = 0 }, Projectile.rotation, origin, 1f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, center, null, (Color.White * 0.7f) with { A = 0 }, Projectile.rotation, origin, 0.8f, SpriteEffects.None);
            return false;
        }
    }
}
