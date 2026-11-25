using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reign_of_the_Ancients.Common.Visuals;
using Reign_of_the_Ancients.Items.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // The Elven Sceptre's beam: telegraph line -> BOOM -> hold (left click) -> forced stop.
    //   ai[0] = age in ticks
    //   ai[2] = age when the beam started fading (0 = still going)
    // Velocity holds the aim direction (synced); the owner steers it toward the cursor.
    public class ElvenSceptreBeam : ModProjectile
    {
        public const int TelegraphTicks = 30;      // 0.5 s aiming line, no damage
        public const int MaxActiveTicks = 90;      // the beam lasts at most 1.5 s after the BOOM
        public const int MinActiveTicks = 15;      // the BOOM always lasts at least this long, even if you let go
        public const int FadeTicks = 10;
        public const int BurstTicks = 12;          // hits in this window after the BOOM get the burst bonus
        public const float BurstDamageMultiplier = 1.5f;
        public const int HitCooldownTicks = 12;
        public const int DrainIntervalTicks = 10;  // while held past the minimum, drain mana every N ticks...
        public const int DrainAmount = 3;          // ...this much

        private const float MaxLength = 800f;      // 50 tiles
        private const float Width = 18f;
        private const float TipOffset = 30f;       // beam starts at the sceptre tip, not the player's chest
        private const float HoldTurnSpeed = 0.08f; // how fast the beam follows the cursor while firing
        private const int AimSyncInterval = 6;
        private const float TelegraphWidth = 2f;
        private const float ExtendTicks = 3f;      // the beam shoots out to full length over this many ticks

        private static readonly Color CoreColor = new Color(150, 215, 255);
        private static readonly Color OuterColor = new Color(70, 120, 255);

        public override string Texture => "Reign_of_the_Ancients/Projectiles/Bosses/VolodarArrow"; // unused, drawn manually

        private ref float Age => ref Projectile.ai[0];
        private ref float FadeStart => ref Projectile.ai[2];
        private bool Fading => FadeStart > 0f;
        private bool Active => Age >= TelegraphTicks && !Fading;
        private float ActiveAge => Age - TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitCooldownTicks;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<ElvenSceptre>())
            {
                Projectile.Kill();
                return;
            }

            Age++;
            if (Projectile.owner == Main.myPlayer)
            {
                OwnerControl(player);
            }

            if (Fading && Age - FadeStart >= FadeTicks)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;

            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.Center = player.MountedCenter + dir * TipOffset;
            Projectile.rotation = dir.ToRotation();
            Projectile.localAI[1] = BeamLength(dir);

            // Keep the player in the casting pose while the beam is up.
            player.ChangeDir(dir.X >= 0f ? 1 : -1);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.itemRotation = (dir * player.direction).ToRotation();

            if (Age == TelegraphTicks)
            {
                Boom(player, dir);
            }
            if (Active)
            {
                for (float d = 0f; d < Projectile.localAI[1]; d += 48f)
                {
                    Lighting.AddLight(Projectile.Center + dir * d, CoreColor.ToVector3() * 0.6f);
                }
                Vector2 impact = Projectile.Center + dir * Projectile.localAI[1];
                Dust spark = Dust.NewDustPerfect(impact, DustID.IceTorch, (-dir).RotatedByRandom(1.1f) * Main.rand.NextFloat(2f, 5f), 100, default, 1.2f);
                spark.noGravity = true;
                if (Main.rand.NextBool(2))
                {
                    Vector2 along = Projectile.Center + dir * Main.rand.NextFloat(Projectile.localAI[1]);
                    Dust d = Dust.NewDustPerfect(along, DustID.IceTorch, Main.rand.NextVector2Circular(1.5f, 1.5f), 100, default, 1.1f);
                    d.noGravity = true;
                }
            }
        }

        // Aim, release, mana drain and the forced stop are decided by the owning client and synced.
        private void OwnerControl(Player player)
        {
            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 current = Projectile.velocity.SafeNormalize(aim);
            Projectile.velocity = Age < TelegraphTicks ? aim : Vector2.Lerp(current, aim, HoldTurnSpeed).SafeNormalize(aim);
            if (Age % AimSyncInterval == 0)
            {
                Projectile.netUpdate = true;
            }

            if (!Active || ActiveAge < MinActiveTicks)
            {
                return;
            }

            bool stop = !player.channel || ActiveAge >= MaxActiveTicks;
            if (!stop && ActiveAge % DrainIntervalTicks == 0)
            {
                stop = !player.CheckMana(player.HeldItem, DrainAmount, pay: true);
                player.manaRegenDelay = (int)player.maxRegenDelay;
            }
            if (stop)
            {
                FadeStart = Age;
                Projectile.netUpdate = true;
            }
        }

        private void Boom(Player player, Vector2 dir)
        {
            SoundEngine.PlaySound(SoundID.Item72 with { Pitch = 0.1f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f, Pitch = 0.4f }, Projectile.Center);
            for (int i = 0; i < 30; i++)
            {
                Vector2 along = Projectile.Center + dir * Main.rand.NextFloat(Projectile.localAI[1]);
                Dust d = Dust.NewDustPerfect(along, DustID.IceTorch, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.5f);
                d.noGravity = true;
            }
            if (Projectile.owner == Main.myPlayer)
            {
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(player.Center, dir, 3f, 6f, 12, 1000f, "ElvenSceptreBoom"));
            }
        }

        // Stops at the first solid block.
        private float BeamLength(Vector2 dir)
        {
            for (float d = 0f; d < MaxLength; d += 8f)
            {
                Point tile = (Projectile.Center + dir * d).ToTileCoordinates();
                if (WorldGen.SolidTile(tile.X, tile.Y))
                {
                    return d;
                }
            }
            return MaxLength;
        }

        // Start the sceptre's cooldown when the beam ends (owner only; the cooldown is local to the item).
        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.owner == Main.myPlayer && player.HeldItem.ModItem is ElvenSceptre sceptre)
            {
                sceptre.cooldownUntil = Main.GameUpdateCount + (uint)ElvenSceptre.CooldownTicks;
            }
        }

        public override bool? CanDamage() => Active;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.localAI[1];
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, Width, ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (ActiveAge < BurstTicks)
            {
                modifiers.SourceDamage *= BurstDamageMultiplier;
            }
        }

        public override void CutTiles()
        {
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.localAI[1];
            DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;
            Utils.PlotTileLine(Projectile.Center, end, Width, DelegateMethods.CutTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float length = Projectile.localAI[1];
            if (Age < TelegraphTicks)
            {
                BeamRenderer.DrawTelegraph(Projectile.Center, Projectile.rotation, length, TelegraphWidth, CoreColor, Age / (float)TelegraphTicks);
                return false;
            }

            // The beam shoots out to full length over a few ticks, overshoots its width (BOOM), settles, then collapses when it stops.
            float reach = MathHelper.Clamp((ActiveAge + 1f) / ExtendTicks, 0f, 1f);
            float widthMult = BeamRenderer.FireEnvelope(ActiveAge);
            float opacity = 1f;
            if (Fading)
            {
                float t = (Age - FadeStart) / FadeTicks;
                widthMult *= BeamRenderer.CloseEnvelope(t);
                opacity = 1f - t * 0.5f;
            }
            BeamRenderer.DrawBeam(Projectile.Center, Projectile.rotation, length * reach, Width * widthMult, OuterColor, CoreColor, opacity);
            return false;
        }
    }
}
