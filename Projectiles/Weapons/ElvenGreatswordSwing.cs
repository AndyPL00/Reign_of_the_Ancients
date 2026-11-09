using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Held swing for the Elven Greatsword.
    // ai[0] = swing direction (+1 / -1, alternates each use)
    // ai[1] = 1 if this is the right-click magic swing
    // ai[2] = total swing duration in ticks (player.itemAnimationMax, so melee speed applies)
    // velocity = locked aim direction (normalized)
    public class ElvenGreatswordSwing : ModProjectile
    {
        // Swing shape, as fractions of the swing duration and angles (degrees) relative to the aim.
        private const float WindupEnd = 0.30f;      // slow pull-back
        private const float SwingEnd = 0.62f;       // explosive arc
        private const float StartAngle = -100f;
        private const float PullbackAngle = -135f;
        private const float SwingEndAngle = 115f;
        private const float FollowThroughAngle = 130f;

        private const float HitStart = 0.32f;       // hitbox active window
        private const float HitEnd = 0.70f;
        private const float SlashReleaseAt = 0.42f; // when the magic slash leaves the blade

        private const float BladeLength = 96f;      // handle-to-tip reach in pixels at scale 1
        private const float BladeWidth = 22f;
        private const float HandleOffset = 6f;      // pixels of grip inside the hand
        private const float WindupMoveSlow = 0.96f; // horizontal velocity multiplier per tick during windup

        private const int TrailLength = 8;
        private const float SwingShake = 3f;
        private const float HitShake = 5f;

        private static readonly Color TrailColor = new Color(120, 255, 200);

        // Set by the item on the owner's client only, for magic swings.
        public int SlashDamage;
        public int SlashCrit;
        public float SlashKnockback;

        private int timer;
        private bool playedSwingSound;
        private bool releasedSlash;
        private readonly float[] oldRotations = new float[TrailLength];

        private float SwingDirection => Projectile.ai[0];
        private bool IsMagicSwing => Projectile.ai[1] == 1f;
        private float Duration => Math.Max(Projectile.ai[2], 1f);
        private float Progress => timer / Duration;
        private bool IsHitActive => Progress >= HitStart && Progress <= HitEnd;

        public override string Texture => "Reign_of_the_Ancients/Items/Weapons/ElvenGreatsword";

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1; // each NPC can only be hit once per swing
            Projectile.timeLeft = 2;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.noItems || owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            if (Progress >= 1f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            if (timer == 0)
            {
                Projectile.rotation = CurrentRotation(0f);
                for (int i = 0; i < TrailLength; i++)
                {
                    oldRotations[i] = Projectile.rotation;
                }
            }

            Projectile.scale = owner.GetAdjustedItemScale(owner.HeldItem);

            owner.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);
            owner.heldProj = Projectile.whoAmI;

            for (int i = TrailLength - 1; i > 0; i--)
            {
                oldRotations[i] = oldRotations[i - 1];
            }
            oldRotations[0] = Projectile.rotation;

            Projectile.rotation = CurrentRotation(Progress);
            Projectile.Center = owner.RotatedRelativePoint(owner.MountedCenter);
            owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);

            if (Progress < WindupEnd)
            {
                owner.velocity.X *= WindupMoveSlow;
            }

            if (!playedSwingSound && Progress >= WindupEnd)
            {
                playedSwingSound = true;
                SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Pitch = -0.6f, Volume = 1.1f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.8f }, Projectile.Center);
                Shake(owner, SwingShake, 10);
            }

            if (IsMagicSwing && !releasedSlash && Progress >= SlashReleaseAt)
            {
                releasedSlash = true;
                ReleaseSlash(owner);
            }

            if (IsHitActive)
            {
                Vector2 tip = Projectile.Center + Projectile.rotation.ToRotationVector2() * BladeLength * Projectile.scale;
                Lighting.AddLight(tip, TrailColor.ToVector3() * (IsMagicSwing ? 0.6f : 0.3f));
                if (Main.rand.NextBool(IsMagicSwing ? 1 : 3))
                {
                    Dust d = Dust.NewDustPerfect(tip, DustID.GemEmerald, Vector2.Zero, 100, default, 1.1f);
                    d.noGravity = true;
                }
            }

            timer++;
        }

        // Angle of the blade at a given swing progress (0..1).
        private float CurrentRotation(float t)
        {
            float offset;
            if (t < WindupEnd)
            {
                float k = t / WindupEnd;
                offset = MathHelper.Lerp(StartAngle, PullbackAngle, EaseOutQuad(k));
            }
            else if (t < SwingEnd)
            {
                float k = (t - WindupEnd) / (SwingEnd - WindupEnd);
                offset = MathHelper.Lerp(PullbackAngle, SwingEndAngle, EaseInOutQuart(k));
            }
            else
            {
                float k = (t - SwingEnd) / (1f - SwingEnd);
                offset = MathHelper.Lerp(SwingEndAngle, FollowThroughAngle, EaseOutCubic(k));
            }

            int facing = Projectile.velocity.X >= 0f ? 1 : -1;
            return Projectile.velocity.ToRotation() + MathHelper.ToRadians(offset) * SwingDirection * facing;
        }

        private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
        private static float EaseOutCubic(float x) => 1f - (float)Math.Pow(1f - x, 3);
        private static float EaseInOutQuart(float x) => x < 0.5f ? 8f * x * x * x * x : 1f - (float)Math.Pow(-2f * x + 2f, 4) / 2f;

        private void ReleaseSlash(Player owner)
        {
            SoundEngine.PlaySound(SoundID.Item60 with { Pitch = -0.2f }, Projectile.Center);

            if (Projectile.owner != Main.myPlayer)
            {
                return;
            }

            Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), owner.MountedCenter + aim * 40f,
                aim * Items.Weapons.ElvenGreatsword.SlashSpeed, ModContent.ProjectileType<ElvenMagicSlash>(),
                SlashDamage, SlashKnockback, Projectile.owner);
            Main.projectile[index].CritChance = SlashCrit;
        }

        private void Shake(Player owner, float strength, int frames)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(owner.Center,
                    Projectile.rotation.ToRotationVector2(), strength, 6f, frames, 1000f, FullName));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsHitActive)
            {
                return false;
            }

            // Check the current blade line and one halfway back to the previous frame, so the fast part
            // of the arc doesn't skip over small targets.
            float midRotation = Projectile.rotation - MathHelper.WrapAngle(Projectile.rotation - oldRotations[0]) * 0.5f;
            return BladeHits(targetHitbox, Projectile.rotation) || BladeHits(targetHitbox, midRotation);
        }

        private bool BladeHits(Rectangle targetHitbox, float rotation)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + rotation.ToRotationVector2() * BladeLength * Projectile.scale;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end,
                BladeWidth * Projectile.scale, ref collisionPoint);
        }

        public override bool? CanCutTiles() => IsHitActive;

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * BladeLength * Projectile.scale;
            Utils.PlotTileLine(Projectile.Center, end, BladeWidth * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = target.Center.X >= Main.player[Projectile.owner].Center.X ? 1 : -1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Shake(Main.player[Projectile.owner], HitShake, 12);
            SoundEngine.PlaySound(SoundID.Item70 with { Pitch = -0.3f, Volume = 0.7f }, target.Center);

            for (int i = 0; i < 12; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(5f, 5f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.GemEmerald, speed, 100, default, 1.3f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            // Sprite points up-right (45 degrees) with the handle in the bottom-left corner.
            Vector2 origin = new Vector2(HandleOffset, texture.Height - HandleOffset);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            if (IsHitActive)
            {
                for (int i = TrailLength - 1; i >= 0; i--)
                {
                    float fade = 1f - (i + 1f) / (TrailLength + 1f);
                    Color trail = (TrailColor * (IsMagicSwing ? 0.6f : 0.35f) * fade) with { A = 0 };
                    Main.EntitySpriteDraw(texture, drawPos, null, trail, oldRotations[i] + MathHelper.PiOver4,
                        origin, Projectile.scale, SpriteEffects.None);
                }
            }

            Main.EntitySpriteDraw(texture, drawPos, null, lightColor, Projectile.rotation + MathHelper.PiOver4,
                origin, Projectile.scale, SpriteEffects.None);

            if (IsMagicSwing)
            {
                Color glow = (TrailColor * 0.45f) with { A = 0 };
                Main.EntitySpriteDraw(texture, drawPos, null, glow, Projectile.rotation + MathHelper.PiOver4,
                    origin, Projectile.scale * 1.04f, SpriteEffects.None);
            }

            return false;
        }
    }
}
