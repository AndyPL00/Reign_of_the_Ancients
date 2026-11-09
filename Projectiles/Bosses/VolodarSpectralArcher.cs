using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses
{
    // Translucent copy of Volodar that fades in, fires a few aimed arrows at ai[0] (player index), and fades out.
    // Harmless to touch; only its arrows hurt (they use this projectile's damage).
    public class VolodarSpectralArcher : ModProjectile
    {
        private const int Lifetime = 100;
        private const int FadeTicks = 15;
        private static readonly int[] ShotTicks = { 30, 50, 70 };
        private const float ArrowSpeed = 10f;
        private const float PredictFactor = 10f;
        private const int FrameDelay = 8;

        private static readonly Color SpectralColor = new Color(140, 210, 255);

        public override string Texture => "Reign_of_the_Ancients/NPCs/Bosses/Volodar";

        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 64;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player target = Main.player[(int)Projectile.ai[0]];
            Projectile.direction = target.Center.X > Projectile.Center.X ? 1 : -1;
            Projectile.spriteDirection = -Projectile.direction; // art faces right

            if (Age < FadeTicks)
            {
                Projectile.Opacity = Age / (float)FadeTicks;
            }
            else if (Projectile.timeLeft < FadeTicks)
            {
                Projectile.Opacity = Projectile.timeLeft / (float)FadeTicks;
            }
            else
            {
                Projectile.Opacity = 1f;
            }

            if (Age == 0)
            {
                Burst();
            }

            foreach (int tick in ShotTicks)
            {
                if (Age == tick && target.active && !target.dead)
                {
                    SoundEngine.PlaySound(SoundID.Item5, Projectile.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 aim = (target.Center + target.velocity * PredictFactor - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, aim * ArrowSpeed,
                            ModContent.ProjectileType<VolodarArrow>(), Projectile.damage, 0f, Main.myPlayer, VolodarArrow.ModeNormal);
                    }
                }
            }

            if (++Projectile.frameCounter >= FrameDelay)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Lighting.AddLight(Projectile.Center, SpectralColor.ToVector3() * 0.4f * Projectile.Opacity);
        }

        public override void OnKill(int timeLeft)
        {
            Burst();
        }

        private void Burst()
        {
            for (int i = 0; i < 16; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 30f), DustID.IceTorch,
                    Main.rand.NextVector2Circular(2f, 2f), 100, default, 1.3f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            var frame = new Rectangle(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            Vector2 origin = frame.Size() / 2f;
            SpriteEffects effects = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 pos = Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(texture, pos, frame, (SpectralColor * 0.35f * Projectile.Opacity) with { A = 0 }, 0f, origin, 1.12f, effects);
            Main.EntitySpriteDraw(texture, pos, frame, (SpectralColor * 0.75f * Projectile.Opacity) with { A = 0 }, 0f, origin, 1f, effects);
            return false;
        }
    }
}
