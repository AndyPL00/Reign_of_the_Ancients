using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Elves
{
    public class ElvenFireball : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = 0;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.light = 0.4f;
            Projectile.alpha = 0;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frameCounter++;
            if(Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            }
            Dust.NewDust(Projectile.position,Projectile.width,Projectile.height,DustID.PurpleTorch,0f,0f,100,default,0.6f);

        }
        public override void OnKill(int timeLeft)
        {
            for(int i = 0 ; i < 8 ; i++)
            {
                Dust.NewDust(Projectile.position,Projectile.width,Projectile.height,DustID.PurpleTorch);
            }
        }
    }
}