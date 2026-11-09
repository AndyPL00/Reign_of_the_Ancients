using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses
{
    // Every arrow Volodar (and his clones) fire. ai[0] picks the behaviour:
    //   Normal   straight aimed arrow
    //   Up       harmless flourish fired upward before the arrow rain
    //   Rain     falls from the ceiling
    //   Piercing fast charged shot that passes through everything
    //   Homing   magic arrow that curves toward ai[1] (player index) for a while
    //   Split    magic arrow that bursts into a fan of normal arrows
    //   Rune     lobbed magic arrow; leaves an exploding rune where it lands
    public class VolodarArrow : ModProjectile
    {
        public const float ModeNormal = 0f;
        public const float ModeUp = 1f;
        public const float ModeRain = 2f;
        public const float ModePiercing = 3f;
        public const float ModeHoming = 4f;
        public const float ModeSplit = 5f;
        public const float ModeRune = 6f;

        private const int HomingTicks = 90;
        private const float HomingTurn = 0.045f;  // radians per tick
        private const int SplitTicks = 25;
        private const int SplitCount = 5;
        private const float SplitSpread = 0.32f;  // radians between split arrows
        private const float RuneGravity = 0.25f;

        private static readonly Color ArrowColor = new Color(190, 210, 255);
        private static readonly Color PiercingColor = new Color(200, 245, 255);
        private static readonly Color MagicColor = new Color(190, 120, 255);

        private float Mode => Projectile.ai[0];
        private bool IsMagic => Mode == ModeHoming || Mode == ModeSplit || Mode == ModeRune;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Mode == ModeUp)
                {
                    Projectile.hostile = false;
                    Projectile.timeLeft = 40;
                }
                else if (Mode == ModePiercing)
                {
                    Projectile.penetrate = -1;
                    Projectile.tileCollide = false;
                    Projectile.timeLeft = 120;
                    Projectile.extraUpdates = 1;
                }
            }

            Projectile.localAI[1]++;

            if (Mode == ModeHoming && Projectile.localAI[1] < HomingTicks)
            {
                Player target = Main.player[(int)Projectile.ai[1]];
                if (target.active && !target.dead)
                {
                    float current = Projectile.velocity.ToRotation();
                    float wanted = (target.Center - Projectile.Center).ToRotation();
                    float turned = current.AngleTowards(wanted, HomingTurn);
                    Projectile.velocity = turned.ToRotationVector2() * Projectile.velocity.Length();
                }
            }
            else if (Mode == ModeSplit && Projectile.localAI[1] >= SplitTicks)
            {
                Split();
                return;
            }
            else if (Mode == ModeRune)
            {
                Projectile.velocity.Y += RuneGravity;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Color color = ColorForMode();
            Lighting.AddLight(Projectile.Center, color.ToVector3() * (IsMagic || Mode == ModePiercing ? 0.6f : 0.25f));
            if (IsMagic || Mode == ModePiercing)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, IsMagic ? DustID.PurpleTorch : DustID.IceTorch,
                    -Projectile.velocity * 0.2f, 100, default, 1.1f);
                d.noGravity = true;
            }
        }

        private void Split()
        {
            SoundEngine.PlaySound(SoundID.Item9, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float start = -SplitSpread * (SplitCount - 1) / 2f;
                for (int i = 0; i < SplitCount; i++)
                {
                    Vector2 velocity = Projectile.velocity.RotatedBy(start + SplitSpread * i);
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, velocity, Type,
                        Projectile.damage, Projectile.knockBack, Main.myPlayer, ModeNormal);
                }
            }
            Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Mode == ModeRune && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<VolodarRune>(), Projectile.damage, 0f, Main.myPlayer);
            }
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Mode == ModeUp)
            {
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, IsMagic ? DustID.PurpleTorch : DustID.IceTorch,
                    Main.rand.NextVector2Circular(2f, 2f), 100, default, 0.9f);
                d.noGravity = true;
            }
        }

        private Color ColorForMode()
        {
            if (Mode == ModePiercing)
            {
                return PiercingColor;
            }
            return IsMagic ? MagicColor : ArrowColor;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() / 2f;
            Color color = ColorForMode();
            bool glowing = IsMagic || Mode == ModePiercing;

            // Afterimage trail.
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                {
                    continue;
                }
                float fade = 1f - (i + 1f) / (Projectile.oldPos.Length + 1f);
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, pos, null, (color * 0.45f * fade) with { A = 0 }, Projectile.rotation,
                    origin, Projectile.scale, SpriteEffects.None);
            }

            Color main = glowing ? color with { A = 0 } : Color.Lerp(lightColor, color, 0.5f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, main, Projectile.rotation,
                origin, Projectile.scale, SpriteEffects.None);
            if (glowing)
            {
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, (Color.White * 0.6f) with { A = 0 },
                    Projectile.rotation, origin, Projectile.scale * 0.8f, SpriteEffects.None);
            }
            return false;
        }
    }
}
