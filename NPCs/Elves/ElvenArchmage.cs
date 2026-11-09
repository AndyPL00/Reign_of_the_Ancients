using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Common;
using Reign_of_the_Ancients.Projectiles.Elves;

namespace Reign_of_the_Ancients.NPCs.Elves
{
    // Floating caster. Each cycle it winds up, then randomly casts either a
    // 3-way spread of ElvenArcaneBolts or a telegraphed instant-hit beam.
    // Teleports to a safe, open spot near the player every few seconds,
    // but only while idle or cooling down (never mid-cast).
    public class ElvenArchmage : ModNPC
    {
        private const float LibrarySpawnChance = 0.5f; // relative weight vs. other elves on the same floor
        // Movement
        private const float DetectRange = 800f;
        private const float PreferredDistance = 380f;
        private const float HoverBuffer = 60f;

        // Attack timing (ticks, 60 = 1 second)
        private const int IdleTicks = 50;
        private const int WindupTicks = 35;
        private const int BeamTelegraphTicks = 45;
        private const int BeamFlashTicks = 10;
        private const int CooldownTicks = 70;

        // Spread attack
        private const int SpreadProjectileCount = 3;
        private const float SpreadAngleStep = MathHelper.Pi / 12f; // 15 degrees between bolts
        private const float ProjectileSpeed = 7f;
        private const int SpreadDamage = 18;

        // Beam attack
        private const float BeamLength = 900f;
        private const float BeamWidth = 18f;
        private const int BeamDamage = 34;

        // Teleport
        private const int TeleportCooldownTicks = 300;
        private const float TeleportMinRadius = 200f;
        private const float TeleportMaxRadius = 450f;
        private const int TeleportAttempts = 30;
        private const int NoSightTeleportCooldownTicks = 90; // teleports sooner when it can't see the player

        // Collision
        private const float ObstacleClimbSpeed = 2f;

        private const int FrameDelay = 8;

        // States (NPC.ai[0])
        private const float StateIdle = 0f;
        private const float StateWindup = 1f;
        private const float StateSpreadFire = 2f;
        private const float StateBeamTelegraph = 3f;
        private const float StateBeamFire = 4f;
        private const float StateCooldown = 5f;

        private const int AttackSpread = 0;
        private const int AttackBeam = 1;

        // NPC.ai[1] = state timer
        // NPC.ai[2] = chosen attack (synced so all clients agree)
        // NPC.ai[3] = locked aim angle in radians (synced)
        private ref float State => ref NPC.ai[0];
        private ref float Timer => ref NPC.ai[1];
        private ref float PendingAttack => ref NPC.ai[2];
        private ref float AimAngle => ref NPC.ai[3];

        private int teleportTimer;
        private bool hasHitThisBeam;

        private Vector2 AimDir => AimAngle.ToRotationVector2();

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 48;
            NPC.damage = 8;
            NPC.defense = 8;
            NPC.lifeMax = 110;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 180f;
            NPC.knockBackResist = 0.4f;
            NPC.aiStyle = -1;
            SpawnModBiomes = new int[] { ModContent.GetInstance<MysticalLibraryBiome>().Type }; // bestiary biome
            NPC.noGravity = true;
            NPC.noTileCollide = false; // flies, but can't pass through blocks
        }

        public override void AI()
        {
            if (NPC.target < 0 ||
                NPC.target == 255 ||
                !Main.player[NPC.target].active ||
                Main.player[NPC.target].dead ||
                Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) > DetectRange)
            {
                NPC.TargetClosest();
            }

            Player player = Main.player[NPC.target];

            if (!player.active || player.dead)
            {
                NPC.velocity *= 0.98f;
                return;
            }

            bool committed = State == StateWindup ||
                             State == StateSpreadFire ||
                             State == StateBeamTelegraph ||
                             State == StateBeamFire;

            // Face the player while free; face the locked aim while casting.
            int facing;
            if (committed)
            {
                facing = AimDir.X >= 0f ? 1 : -1;
            }
            else
            {
                facing = player.Center.X > NPC.Center.X ? 1 : -1;
            }

            NPC.direction = facing;
            NPC.spriteDirection = -facing; // art faces right by default, so flip

            TryTeleport(player);

            switch (State)
            {
                case StateIdle:
                    HandleIdle(player);
                    break;

                case StateWindup:
                    HandleWindup();
                    break;

                case StateSpreadFire:
                    HandleSpreadFire();
                    break;

                case StateBeamTelegraph:
                    HandleBeamTelegraph();
                    break;

                case StateBeamFire:
                    HandleBeamFire();
                    break;

                case StateCooldown:
                    HandleCooldown(player);
                    break;
            }

            Lighting.AddLight(NPC.Center, 0.35f, 0.15f, 0.5f);
        }

        private void Hover(Player player)
        {
            Vector2 toPlayer = player.Center - NPC.Center;
            float distance = toPlayer.Length();
            Vector2 dir = toPlayer.SafeNormalize(Vector2.Zero);

            if (distance > PreferredDistance)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, dir * 2.5f, 0.04f);
            }
            else if (distance < PreferredDistance - HoverBuffer)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, -dir * 1.5f, 0.04f);
            }
            else
            {
                NPC.velocity *= 0.96f;
            }

            if (NPC.collideX)
            {
                NPC.velocity.Y = -ObstacleClimbSpeed;
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = -NPC.oldVelocity.Y * 0.5f;
            }
        }

        private bool CanSee(Player player)
        {
            return Collision.CanHitLine(NPC.position, NPC.width, NPC.height, player.position, player.width, player.height);
        }

        // How far the beam reaches before hitting a solid block.
        private float CurrentBeamLength()
        {
            const float step = 8f;
            for (float d = 0f; d < BeamLength; d += step)
            {
                Point tile = (NPC.Center + AimDir * d).ToTileCoordinates();
                if (WorldGen.SolidTile(tile.X, tile.Y))
                {
                    return d;
                }
            }
            return BeamLength;
        }

        private void HandleIdle(Player player)
        {
            Hover(player);

            Timer = Math.Min(Timer + 1, IdleTicks);

            // Only starts a cast when it has a clear line of sight.
            if (Timer >= IdleTicks && CanSee(player))
            {
                SetState(StateWindup);

                // Server decides the attack and aim, then syncs to clients.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 toPlayer = player.Center - NPC.Center;
                    AimAngle = toPlayer.SafeNormalize(Vector2.UnitX * NPC.direction).ToRotation();
                    PendingAttack = Main.rand.Next(2);
                    NPC.netUpdate = true;
                }
            }
        }

        private void HandleWindup()
        {
            NPC.velocity *= 0.9f;
            Timer++;

            // Charging sparkles at the staff
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(24f, 24f),
                    DustID.PurpleTorch,
                    Vector2.Zero,
                    100,
                    default,
                    1.1f
                );
                d.noGravity = true;
                d.velocity = (NPC.Center - d.position) * 0.08f;
            }

            if (Timer >= WindupTicks)
            {
                if ((int)PendingAttack == AttackSpread)
                {
                    SetState(StateSpreadFire);
                }
                else
                {
                    SetState(StateBeamTelegraph);
                    SoundEngine.PlaySound(SoundID.Item15, NPC.Center);
                }
            }
        }

        private void HandleSpreadFire()
        {
            NPC.velocity *= 0.9f;

            SoundEngine.PlaySound(SoundID.Item20, NPC.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int type = ModContent.ProjectileType<ElvenArcaneBolt>();
                float startOffset = -SpreadAngleStep * (SpreadProjectileCount - 1) / 2f;

                for (int i = 0; i < SpreadProjectileCount; i++)
                {
                    Vector2 velocity = AimDir.RotatedBy(startOffset + SpreadAngleStep * i) * ProjectileSpeed;

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        velocity,
                        type,
                        SpreadDamage,
                        1f,
                        Main.myPlayer
                    );
                }
            }

            SetState(StateCooldown);
        }

        private void HandleBeamTelegraph()
        {
            NPC.velocity *= 0.85f;
            Timer++;

            if (Timer >= BeamTelegraphTicks)
            {
                SetState(StateBeamFire);
                hasHitThisBeam = false;
                SoundEngine.PlaySound(SoundID.Item72, NPC.Center);

                // Burst of dust along the beam
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        Vector2 pos = NPC.Center + AimDir * Main.rand.NextFloat(CurrentBeamLength());
                        Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Main.rand.NextVector2Circular(2f, 2f), 100, default, 1.4f);
                        d.noGravity = true;
                    }
                }
            }
        }

        private void HandleBeamFire()
        {
            NPC.velocity = Vector2.Zero;
            Timer++;

            Vector2 start = NPC.Center;
            Vector2 end = start + AimDir * CurrentBeamLength();

            // Light up the beam's path
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                Lighting.AddLight(Vector2.Lerp(start, end, t), 0.6f, 0.25f, 0.9f);
            }

            // Each client checks only its own player, which is the
            // multiplayer-correct way to deal NPC damage from custom hitboxes.
            if (!hasHitThisBeam && Main.netMode != NetmodeID.Server)
            {
                Player local = Main.LocalPlayer;

                if (local.active && !local.dead)
                {
                    float collisionPoint = 0f;

                    if (Collision.CheckAABBvLineCollision(
                            local.position,
                            local.Size,
                            start,
                            end,
                            BeamWidth,
                            ref collisionPoint))
                    {
                        local.Hurt(
                            PlayerDeathReason.ByNPC(NPC.whoAmI),
                            BeamDamage,
                            AimDir.X >= 0f ? 1 : -1,
                            false,
                            false
                        );

                        hasHitThisBeam = true;
                    }
                }
            }

            if (Timer >= BeamFlashTicks)
            {
                SetState(StateCooldown);
            }
        }

        private void HandleCooldown(Player player)
        {
            Hover(player);
            Timer++;

            if (Timer >= CooldownTicks)
            {
                SetState(StateIdle);
            }
        }

        private void SetState(float newState)
        {
            State = newState;
            Timer = 0f;
        }

        private void TryTeleport(Player player)
        {
            teleportTimer++;

            int cooldown = CanSee(player) ? TeleportCooldownTicks : NoSightTeleportCooldownTicks;
            if (teleportTimer < cooldown)
            {
                return;
            }

            if (State != StateIdle && State != StateCooldown)
            {
                return;
            }

            // Only the server (or singleplayer) picks the spot; position syncs via netUpdate.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int attempt = 0; attempt < TeleportAttempts; attempt++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(TeleportMinRadius, TeleportMaxRadius);
                Vector2 newCenter = player.Center + angle.ToRotationVector2() * radius;
                Vector2 newPosition = newCenter - NPC.Size / 2f;

                if (!IsValidTeleportSpot(newPosition, newCenter, player))
                {
                    continue;
                }

                SpawnTeleportDust(NPC.Center);

                NPC.position = newPosition;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;

                SpawnTeleportDust(NPC.Center);
                SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                teleportTimer = 0;
                return;
            }

            // No valid spot found this tick; try again shortly instead of every frame.
            teleportTimer = TeleportCooldownTicks - 20;
        }

        private bool IsValidTeleportSpot(Vector2 position, Vector2 center, Player player)
        {
            // Stay inside the world with a safety margin
            float margin = 16f * 10f;
            if (position.X < margin || position.Y < margin ||
                position.X > Main.maxTilesX * 16f - margin ||
                position.Y > Main.maxTilesY * 16f - margin)
            {
                return false;
            }

            // No blinking into solid tiles (padded by a few pixels)
            if (Collision.SolidCollision(position - new Vector2(4f), NPC.width + 8, NPC.height + 8))
            {
                return false;
            }

            // No blinking into liquid
            if (Collision.WetCollision(position, NPC.width, NPC.height))
            {
                return false;
            }

            // Must have line of sight to the player so it can't attack from inside a wall pocket
            if (!Collision.CanHitLine(center, 1, 1, player.Center, 1, 1))
            {
                return false;
            }

            return true;
        }

        private static void SpawnTeleportDust(Vector2 center)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }

            for (int i = 0; i < 20; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    center,
                    DustID.PurpleTorch,
                    Main.rand.NextVector2Circular(4f, 4f),
                    100,
                    default,
                    1.5f
                );
                d.noGravity = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (State == StateBeamTelegraph)
            {
                // Thin, flickering line that grows brighter as the beam gets closer to firing
                float progress = Timer / BeamTelegraphTicks;
                float flicker = 0.6f + 0.4f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 30f);
                Color color = new Color(200, 120, 255) * (0.25f + 0.55f * progress) * flicker;
                DrawBeamLine(spriteBatch, screenPos, 2f + 2f * progress, color);
            }
            else if (State == StateBeamFire)
            {
                // Thick glowing core that fades out over the flash duration
                float fade = 1f - Timer / BeamFlashTicks;
                DrawBeamLine(spriteBatch, screenPos, BeamWidth * 1.6f, new Color(150, 60, 255, 0) * 0.5f * fade);
                DrawBeamLine(spriteBatch, screenPos, BeamWidth, new Color(200, 120, 255, 0) * 0.85f * fade);
                DrawBeamLine(spriteBatch, screenPos, BeamWidth * 0.35f, new Color(255, 255, 255, 0) * fade);
            }

            return true; // draw the NPC sprite normally on top
        }

        private void DrawBeamLine(SpriteBatch spriteBatch, Vector2 screenPos, float thickness, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;

            spriteBatch.Draw(
                pixel,
                NPC.Center - screenPos,
                new Rectangle(0, 0, 1, 1),
                color,
                AimAngle,
                new Vector2(0f, 0.5f),
                new Vector2(CurrentBeamLength(), thickness),
                SpriteEffects.None,
                0f
            );
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            if (NPC.frameCounter >= FrameDelay)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;

                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return MysticalLibrarySpawns.Chance(spawnInfo, MysticalLibrary.ForbiddenArchive, LibrarySpawnChance);
        }
    }
}
