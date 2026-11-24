using System;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Pets
{
    // Floats above the player's shoulder, bobs, flips its pages and glows faintly pale blue.
    public class TinySealedTomePet : ModProjectile
    {
        private const int FrameCount = 4;
        private const int FrameDelay = 8;
        private const float OffsetX = 28f;
        private const float OffsetY = -36f;
        private const float BobHeight = 4f;
        private const float BobPeriodTicks = 120f;
        private const float FollowStrength = 0.15f;
        private const float TeleportDistance = 1200f;
        private const int SparkleChance = 20; // 1 in N ticks
        private static readonly Vector3 GlowLight = new Vector3(0.25f, 0.45f, 0.7f);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
            Main.projPet[Type] = true;
            ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, FrameCount, FrameDelay)
                .WithOffset(-2, -22f);
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EyeOfCthulhuPet);
            Projectile.aiStyle = -1;
            Projectile.width = 22;
            Projectile.height = 24;
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(lightColor, Color.White, 0.5f) * Projectile.Opacity;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.dead && player.HasBuff(ModContent.BuffType<TinySealedTomeBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            int dir = player.direction;
            Projectile.direction = Projectile.spriteDirection = dir;

            Vector2 target = player.MountedCenter + new Vector2(-dir * OffsetX, OffsetY);
            target.Y += (float)Math.Sin(Main.GameUpdateCount / BobPeriodTicks * MathHelper.TwoPi) * BobHeight;
            Vector2 toTarget = target - Projectile.Center;
            if (toTarget.LengthSquared() > TeleportDistance * TeleportDistance)
            {
                Projectile.Center = target;
                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.velocity = toTarget * FollowStrength;
            }
            Projectile.rotation = Projectile.velocity.X * 0.04f;

            if (++Projectile.frameCounter >= FrameDelay)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            Lighting.AddLight(Projectile.Center, GlowLight);
            if (!Main.dedServ && Main.rand.NextBool(SparkleChance))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch);
                dust.noGravity = true;
                dust.velocity *= 0.3f;
                dust.scale = 0.8f;
            }
        }
    }
}
