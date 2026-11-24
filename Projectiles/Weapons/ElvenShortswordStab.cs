using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Items.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // The Elven Shortsword's stab (vanilla shortsword pattern from ExampleMod).
    // Counts stabs that land; every Nth one also fires a piercing light-dash.
    public class ElvenShortswordStab : ModProjectile
    {
        private const int FadeInDuration = 7;
        private const int FadeOutDuration = 4;
        private const int TotalDuration = 16;
        private const int SpriteSize = 32;

        private float CollisionWidth => 10f * Projectile.scale;
        private bool countedHit;

        public override string Texture => "Reign_of_the_Ancients/Items/Weapons/ElvenShortsword";

        public override void SetDefaults()
        {
            Projectile.Size = new Vector2(18);
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ownerHitCheck = true;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 360;
            Projectile.hide = true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.ai[0]++;
            if (Projectile.ai[0] >= TotalDuration)
            {
                Projectile.Kill();
                return;
            }
            player.heldProj = Projectile.whoAmI;

            Projectile.Opacity = Utils.GetLerpValue(0f, FadeInDuration, Projectile.ai[0], clamped: true)
                * Utils.GetLerpValue(TotalDuration, TotalDuration - FadeOutDuration, Projectile.ai[0], clamped: true);

            Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: false, addGfxOffY: false);
            Projectile.Center = playerCenter + Projectile.velocity * (Projectile.ai[0] - 1f);
            Projectile.spriteDirection = (Vector2.Dot(Projectile.velocity, Vector2.UnitX) >= 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 - MathHelper.PiOver4 * Projectile.spriteDirection;

            int half = SpriteSize / 2;
            DrawOriginOffsetX = 0;
            DrawOffsetX = -(half - Projectile.width / 2);
            DrawOriginOffsetY = -(half - Projectile.height / 2);
        }

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity.SafeNormalize(-Vector2.UnitY) * 10f;
            Utils.PlotTileLine(start, end, CollisionWidth, DelegateMethods.CutTiles);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * 6f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, CollisionWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Hits are processed by the owner; count each stab once, even if it hits several enemies.
            Player player = Main.player[Projectile.owner];
            if (countedHit || Projectile.owner != Main.myPlayer || player.HeldItem.ModItem is not ElvenShortsword sword)
            {
                return;
            }
            countedHit = true;
            sword.hitCounter++;
            if (sword.hitCounter % ElvenShortsword.HitsPerDash != 0)
            {
                return;
            }

            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.6f, Pitch = 0.4f }, player.Center);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.MountedCenter, dir * ElvenShortsword.DashSpeed,
                ModContent.ProjectileType<ElvenLightStreak>(), (int)(Projectile.damage * ElvenShortsword.DashDamageRatio),
                Projectile.knockBack, Projectile.owner, ElvenLightStreak.DashMode);
        }
    }
}
