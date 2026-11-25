using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses.Titan
{
    // Ground shockwave from the Titan's slam: a low wave of fire and dust running along the floor until it hits a wall.
    // Players jump over it. ai[0] = the floor's Y (pixels); the wave keeps its bottom there.
    public class TitanShockwave : ModProjectile
    {
        private const int LifeTicks = 210;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTicks;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Projectile.Bottom = new Vector2(Projectile.Center.X, Projectile.ai[0]);
            Projectile.direction = Projectile.velocity.X >= 0f ? 1 : -1;

            // Stops at the first wall it runs into.
            int frontX = (int)((Projectile.Center.X + Projectile.direction * (Projectile.width / 2f + 2f)) / 16f);
            int footY = (int)((Projectile.ai[0] - 8f) / 16f);
            if (WorldGen.InWorld(frontX, footY) && WorldGen.SolidTile(frontX, footY))
            {
                Projectile.Kill();
                return;
            }

            if (Main.dedServ)
            {
                return;
            }
            Dust dust = Dust.NewDustDirect(Projectile.BottomLeft - new Vector2(0f, 10f), Projectile.width, 10, DustID.Smoke, -Projectile.velocity.X * 0.2f, -2f, 100, default, 1.6f);
            dust.noGravity = true;
            if (Main.rand.NextBool(2))
            {
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, -1.5f, 100, default, 1.3f);
                fire.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.3f, 0.05f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            float fade = Projectile.timeLeft < 20 ? Projectile.timeLeft / 20f : 1f;
            SpriteEffects effects = Projectile.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height);
            Main.EntitySpriteDraw(texture, Projectile.Bottom - Main.screenPosition, null, (Color.White * 0.9f * fade) with { A = 0 }, 0f, origin, 1f, effects, 0);
            return false;
        }
    }
}
