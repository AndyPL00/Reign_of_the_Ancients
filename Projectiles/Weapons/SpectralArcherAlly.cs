using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Spectral Sigil: a ghostly archer appears behind the player, fires one arcane arrow
    // at the nearest enemy, and fades away. Harmless to touch.
    public class SpectralArcherAlly : ModProjectile
    {
        private const int Lifetime = 45;
        private const int ShotTick = 15;
        private const int FadeTicks = 10;
        private const float TargetRange = 700f;
        private const float ArrowSpeed = 13f;
        private const int FrameDelay = 8;

        private static readonly Color SpectralColor = new Color(140, 210, 255);

        public override string Texture => "Reign_of_the_Ancients/NPCs/Bosses/Volodar"; // placeholder art

        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 64;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            NPC target = FindTarget();
            if (target != null)
            {
                Projectile.direction = target.Center.X > Projectile.Center.X ? 1 : -1;
            }
            Projectile.spriteDirection = -Projectile.direction; // art faces right

            float fadeIn = Age / (float)FadeTicks;
            float fadeOut = Projectile.timeLeft / (float)FadeTicks;
            Projectile.Opacity = MathHelper.Clamp(System.Math.Min(fadeIn, fadeOut), 0f, 1f);

            if (Age == ShotTick && target != null)
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Pitch = 0.3f }, Projectile.Center);
                if (Projectile.owner == Main.myPlayer)
                {
                    Vector2 velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * ArrowSpeed;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<ArcaneArrow>(),
                        Projectile.damage, 2f, Projectile.owner, 1f); // no split
                }
            }

            if (++Projectile.frameCounter >= FrameDelay)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
            Lighting.AddLight(Projectile.Center, SpectralColor.ToVector3() * 0.3f * Projectile.Opacity);
        }

        private NPC FindTarget()
        {
            NPC best = null;
            float bestDistance = TargetRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || !Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                {
                    continue;
                }
                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }
            return best;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameHeight = texture.Height / Main.projFrames[Type];
            var frame = new Rectangle(0, frameHeight * Projectile.frame, texture.Width, frameHeight);
            SpriteEffects effects = Projectile.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, (SpectralColor * 0.6f * Projectile.Opacity) with { A = 0 },
                0f, frame.Size() / 2f, 0.85f, effects);
            return false;
        }
    }
}
