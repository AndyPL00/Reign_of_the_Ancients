using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Common;
using Reign_of_the_Ancients.Items.Bosses;
using Reign_of_the_Ancients.Items.Placeable;
using Reign_of_the_Ancients.Items.Vanity;
using Reign_of_the_Ancients.Items.Weapons;
using Terraria.Chat;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Reign_of_the_Ancients.NPCs.Elves;
using Reign_of_the_Ancients.Projectiles.Bosses;
using Reign_of_the_Ancients.WorldGeneration;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Bosses
{
    // Volodar, the mage-archer boss of the Mystical Library's throne room.
    //
    // Phase 1 (archer): aimed volleys, arrow rain, telegraphed piercing shot, homing magic arrows, rune arrows.
    // Phase 2 (below Phase2LifeRatio): adds splitting arrows, explosive orbs, a sweeping beam and spectral
    //   archer clones; volleys and rain get denser, and elf guards march in through the entrance.
    //
    // Multiplayer: the server picks attacks and spawns projectiles; state lives in ai[] and syncs via netUpdate.
    [AutoloadBossHead]
    public class Volodar : ModNPC
    {
        // ---- Stats ----
        private const int BaseLife = 5000;
        private const int BaseDefense = 12;
        private const int ContactDamage = 30;
        private const float Phase2LifeRatio = 0.5f;
        private const int Phase1Music = MusicID.Boss1;
        private const int Phase2Music = MusicID.Boss3;

        // ---- Loot ----
        private const int TrophyChance = 10; // 1 in N
        private const int MaskChance = 7;    // 1 in N (normal mode; the bag uses the same)
        private static readonly Color ChatColor = new Color(175, 130, 255);

        // ---- Movement ----
        private const float MoveSpeed = 9f;
        private const float MoveAccel = 0.08f;
        private const float HoverDistance = 300f;
        private const float HoverHeight = 140f;
        private const int RoomMarginTiles = 4;      // keeps him away from the throne room walls
        private const float DespawnDistance = 3500f;

        // ---- Timing (ticks, 60 = 1 second) ----
        private const int IntroTicks = 100;
        private const int TransitionTicks = 130;
        private const int IdleTicksPhase1 = 45;
        private const int IdleTicksPhase2 = 28;
        private const int DespawnTicks = 60;
        private const int FrameDelay = 8;

        // ---- Projectile damage. Vanilla multiplies hostile projectile damage by 2 (normal), 4 (expert), 6 (master). ----
        private const int ArrowDamage = 14;
        private const int RainDamage = 12;
        private const int ChargedDamage = 22;
        private const int MagicArrowDamage = 13;
        private const int RuneDamage = 18;
        private const int OrbDamage = 16;
        private const int BeamDamage = 20;
        private const int CloneArrowDamage = 12;

        // ---- Attack tuning ----
        private const float ArrowSpeed = 11f;
        private const float PredictFactor = 12f;
        private const int VolleyStart = 20;
        private const int VolleyGap = 18;
        private const float VolleySpread = 0.14f;
        private const int RainUpArrows = 6;
        private const int RainStart = 45;
        private const int RainDuration = 70;
        private const int RainIntervalPhase1 = 4;
        private const int RainIntervalPhase2 = 3;
        private const float RainSpread = 420f;
        private const float RainSpeed = 10f;
        private const int ChargedTrackTicks = 45;
        private const int ChargedLockTicks = 15;
        private const float ChargedSpeed = 24f;
        private const float ChargedSideAngle = 0.25f;
        private const int HomingCount = 3;
        private const float HomingSpeed = 7f;
        private const float RuneArrowSpread = 200f;
        private const int RuneFlightTicks = 40;
        private const float RuneGravity = 0.25f; // must match VolodarArrow
        private const int OrbCount = 3;
        private const int BeamRepositionTicks = 40;
        private const float CloneOffsetX = 420f;
        private const float CloneOffsetY = -120f;
        private const int GuardIntervalTicks = 900;
        private const int MaxGuards = 2;

        // ---- States (ai[0]) ----
        private const float StateIntro = 0f;
        private const float StateIdle = 1f;
        private const float StateVolley = 2f;
        private const float StateRain = 3f;
        private const float StateCharged = 4f;
        private const float StateHoming = 5f;
        private const float StateRunes = 6f;
        private const float StateSplit = 7f;
        private const float StateOrbs = 8f;
        private const float StateBeam = 9f;
        private const float StateClones = 10f;
        private const float StateTransition = 11f;
        private const float StateDespawn = 12f;

        private static readonly float[] Phase1Attacks = { StateVolley, StateRain, StateCharged, StateHoming, StateRunes };
        private static readonly float[] Phase2Attacks = { StateVolley, StateRain, StateCharged, StateSplit, StateRunes, StateOrbs, StateBeam, StateClones };

        private ref float State => ref NPC.ai[0];
        private ref float Timer => ref NPC.ai[1];
        private ref float Phase => ref NPC.ai[2];   // 0/1 = phase 1, 2 = phase 2
        private ref float Aux => ref NPC.ai[3];     // idle side (+1/-1) or locked aim angle

        private bool IsPhase2 => Phase >= 2f;
        private int Tick => (int)Timer;

        // Server-only bookkeeping.
        private float lastAttack = -1f;
        private int guardTimer;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 4;
            NPCID.Sets.TrailCacheLength[Type] = 6;
            NPCID.Sets.TrailingMode[Type] = 0;
            NPCID.Sets.MPAllowedEnemies[Type] = true; // needed for summoning in multiplayer
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 64;
            NPC.damage = ContactDamage;
            NPC.defense = BaseDefense;
            NPC.lifeMax = BaseLife;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = Item.buyPrice(gold: 5);
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            Music = Phase1Music;
        }

        // ================= AI =================

        public override void AI()
        {
            if (State != StateDespawn && !HasValidTarget())
            {
                NPC.TargetClosest();
                if (!HasValidTarget())
                {
                    SetState(StateDespawn);
                }
            }

            Player player = Main.player[NPC.target];
            NPC.dontTakeDamage = State == StateIntro || State == StateTransition || State == StateDespawn;
            Music = IsPhase2 ? Phase2Music : Phase1Music;

            if (State != StateCharged && State != StateBeam)
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = -NPC.direction; // art faces right

            if (!IsPhase2 && State != StateIntro && State != StateTransition && State != StateDespawn
                && NPC.life <= NPC.lifeMax * Phase2LifeRatio)
            {
                SetState(StateTransition);
            }

            switch (State)
            {
                case StateIntro: Intro(); break;
                case StateIdle: Idle(player); break;
                case StateVolley: Volley(player); break;
                case StateRain: Rain(player); break;
                case StateCharged: Charged(player); break;
                case StateHoming: Homing(player); break;
                case StateRunes: Runes(player); break;
                case StateSplit: SplitArrows(player); break;
                case StateOrbs: Orbs(player); break;
                case StateBeam: Beam(player); break;
                case StateClones: Clones(player); break;
                case StateTransition: Transition(); break;
                case StateDespawn: Despawn(); break;
            }

            if (IsPhase2 && Main.netMode != NetmodeID.MultiplayerClient && ++guardTimer >= GuardIntervalTicks)
            {
                guardTimer = 0;
                SummonGuards();
            }

            BarrierSparkles();
            Lighting.AddLight(NPC.Center, IsPhase2 ? new Vector3(0.5f, 0.3f, 0.9f) : new Vector3(0.3f, 0.35f, 0.6f));
        }

        private bool HasValidTarget()
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers)
            {
                return false;
            }
            Player p = Main.player[NPC.target];
            return p.active && !p.dead && Vector2.Distance(p.Center, NPC.Center) < DespawnDistance;
        }

        private void SetState(float state)
        {
            State = state;
            Timer = 0f;
            NPC.netUpdate = true;
        }

        // Back to hovering. The server picks which side of the player to hover on next.
        private void EndAttack()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Aux = Main.rand.NextBool() ? 1f : -1f;
                NPC.TargetClosest();
            }
            SetState(StateIdle);
        }

        // ================= States =================

        private void Intro()
        {
            if (Tick == 0)
            {
                // Rise from the throne.
                if (MysticalLibrarySystem.Exists)
                {
                    Point throne = MysticalLibraryBuilder.ThroneTile(MysticalLibrarySystem.Layout);
                    NPC.Center = new Vector2(throne.X * 16 + 8, (throne.Y - 3) * 16);
                }
                NPC.velocity = Vector2.Zero;
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                Say("SpawnLine");
            }

            NPC.velocity = Tick < 60 ? new Vector2(0f, -0.8f) : NPC.velocity * 0.9f;
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(30f, 40f), DustID.PurpleTorch, new Vector2(0f, -2f), 100, default, 1.4f);
                d.noGravity = true;
            }

            Timer++;
            if (Tick >= IntroTicks)
            {
                EndAttack();
            }
        }

        private void Idle(Player player)
        {
            MoveTo(HoverPoint(player), MoveSpeed);
            Timer++;

            int idleTicks = IsPhase2 ? IdleTicksPhase2 : IdleTicksPhase1;
            if (Tick >= idleTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float[] pool = IsPhase2 ? Phase2Attacks : Phase1Attacks;
                float next;
                do
                {
                    next = pool[Main.rand.Next(pool.Length)];
                }
                while (next == lastAttack);
                lastAttack = next;
                SetState(next);
            }
        }

        private void Volley(Player player)
        {
            MoveTo(HoverPoint(player), MoveSpeed * 0.6f);
            int bursts = IsPhase2 ? 4 : 3;
            int arrows = IsPhase2 ? 5 : 3;

            int sinceStart = Tick - VolleyStart;
            if (sinceStart >= 0 && sinceStart % VolleyGap == 0 && sinceStart / VolleyGap < bursts)
            {
                SoundEngine.PlaySound(SoundID.Item5, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aim = AimAt(player);
                    for (int i = 0; i < arrows; i++)
                    {
                        float offset = VolleySpread * (i - (arrows - 1) / 2f);
                        Shoot(aim.RotatedBy(offset) * ArrowSpeed, ArrowDamage, VolodarArrow.ModeNormal);
                    }
                }
            }

            Timer++;
            if (Tick >= VolleyStart + bursts * VolleyGap + 20)
            {
                EndAttack();
            }
        }

        private void Rain(Player player)
        {
            MoveTo(ClampToRoom(player.Center + new Vector2(0f, -260f)), MoveSpeed);

            if (Tick == 25)
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Pitch = -0.3f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < RainUpArrows; i++)
                    {
                        float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-0.35f, 0.35f);
                        Shoot(angle.ToRotationVector2() * 14f, 0, VolodarArrow.ModeUp);
                    }
                }
            }

            int interval = IsPhase2 ? RainIntervalPhase2 : RainIntervalPhase1;
            int sinceRain = Tick - RainStart;
            if (sinceRain >= 0 && sinceRain < RainDuration && sinceRain % interval == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                var spawn = new Vector2(player.Center.X + Main.rand.NextFloat(-RainSpread, RainSpread), RainSpawnY(player));
                var velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), RainSpeed);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), spawn, velocity, ModContent.ProjectileType<VolodarArrow>(),
                    RainDamage, 0f, Main.myPlayer, VolodarArrow.ModeRain);
            }

            Timer++;
            if (Tick >= RainStart + RainDuration + 20)
            {
                EndAttack();
            }
        }

        private void Charged(Player player)
        {
            NPC.velocity *= 0.9f;

            if (Tick < ChargedTrackTicks)
            {
                Aux = (player.Center - NPC.Center).ToRotation();
                NPC.direction = Math.Cos(Aux) >= 0 ? 1 : -1;
            }
            else if (Tick == ChargedTrackTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true; // lock in the server's aim
            }

            if (Tick == ChargedTrackTicks + ChargedLockTicks)
            {
                SoundEngine.PlaySound(SoundID.Item72, NPC.Center);
                Vector2 dir = Aux.ToRotationVector2();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Shoot(dir * ChargedSpeed, ChargedDamage, VolodarArrow.ModePiercing);
                    if (IsPhase2)
                    {
                        Shoot(dir.RotatedBy(ChargedSideAngle) * ChargedSpeed, ChargedDamage, VolodarArrow.ModePiercing);
                        Shoot(dir.RotatedBy(-ChargedSideAngle) * ChargedSpeed, ChargedDamage, VolodarArrow.ModePiercing);
                    }
                }
                NPC.velocity = -dir * 6f; // recoil
                Shake(4f, 12);
            }

            Timer++;
            if (Tick >= ChargedTrackTicks + ChargedLockTicks + 25)
            {
                EndAttack();
            }
        }

        private void Homing(Player player)
        {
            MoveTo(HoverPoint(player), MoveSpeed * 0.7f);

            for (int i = 0; i < HomingCount; i++)
            {
                if (Tick == 15 + i * 12)
                {
                    SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Launched outward in a fan, then they curve in.
                        Vector2 dir = AimAt(player).RotatedBy((i - (HomingCount - 1) / 2f) * 0.7f);
                        Shoot(dir * HomingSpeed, MagicArrowDamage, VolodarArrow.ModeHoming, player.whoAmI);
                    }
                }
            }

            Timer++;
            if (Tick >= 15 + HomingCount * 12 + 30)
            {
                EndAttack();
            }
        }

        private void Runes(Player player)
        {
            MoveTo(HoverPoint(player), MoveSpeed * 0.6f);
            int count = IsPhase2 ? 6 : 4;

            for (int i = 0; i < count; i++)
            {
                if (Tick == 20 + i * 8)
                {
                    SoundEngine.PlaySound(SoundID.Item5 with { Pitch = 0.3f }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Lob so the arrow comes down around the player after RuneFlightTicks.
                        float targetX = player.Center.X + MathHelper.Lerp(-RuneArrowSpread, RuneArrowSpread, i / (float)(count - 1));
                        var target = new Vector2(targetX, player.Bottom.Y);
                        Vector2 delta = target - NPC.Center;
                        float t = RuneFlightTicks;
                        var velocity = new Vector2(delta.X / t, (delta.Y - 0.5f * RuneGravity * t * t) / t);
                        Shoot(velocity, RuneDamage, VolodarArrow.ModeRune);
                    }
                }
            }

            Timer++;
            if (Tick >= 20 + count * 8 + 30)
            {
                EndAttack();
            }
        }

        private void SplitArrows(Player player)
        {
            MoveTo(HoverPoint(player), MoveSpeed * 0.6f);

            if (Tick == 20 || Tick == 40)
            {
                SoundEngine.PlaySound(SoundID.Item5, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Shoot(AimAt(player) * 9f, MagicArrowDamage, VolodarArrow.ModeSplit);
                }
            }

            Timer++;
            if (Tick >= 80)
            {
                EndAttack();
            }
        }

        private void Orbs(Player player)
        {
            MoveTo(ClampToRoom(player.Center + new Vector2(0f, -220f)), MoveSpeed);

            if (Tick == 30)
            {
                SoundEngine.PlaySound(SoundID.Item117, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < OrbCount; i++)
                    {
                        Vector2 dir = (MathHelper.TwoPi * i / OrbCount - MathHelper.PiOver2).ToRotationVector2();
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + dir * 60f, dir * 2f,
                            ModContent.ProjectileType<VolodarOrb>(), OrbDamage, 0f, Main.myPlayer, player.whoAmI);
                    }
                }
            }

            Timer++;
            if (Tick >= 100)
            {
                EndAttack();
            }
        }

        private void Beam(Player player)
        {
            if (Tick < BeamRepositionTicks)
            {
                MoveTo(BeamSpot(player), MoveSpeed * 1.4f);
            }
            else
            {
                NPC.velocity *= 0.85f;
            }

            if (Tick == BeamRepositionTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float toPlayer = (player.Center - NPC.Center).ToRotation();
                float dir = Main.rand.NextBool() ? 1f : -1f;
                Aux = dir;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<VolodarBeam>(),
                    BeamDamage, 0f, Main.myPlayer, NPC.whoAmI, toPlayer - dir * VolodarBeam.SweepArc / 2f, dir);
                NPC.netUpdate = true;
            }

            Timer++;
            if (Tick >= BeamRepositionTicks + VolodarBeam.TotalTicks + 10)
            {
                EndAttack();
            }
        }

        private void Clones(Player player)
        {
            MoveTo(HoverPoint(player), MoveSpeed * 0.6f);

            if (Tick == 15)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.4f }, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    foreach (int side in new[] { -1, 1 })
                    {
                        Vector2 pos = ClampToRoom(player.Center + new Vector2(side * CloneOffsetX, CloneOffsetY));
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<VolodarSpectralArcher>(),
                            CloneArrowDamage, 0f, Main.myPlayer, player.whoAmI);
                    }
                }
            }

            Timer++;
            if (Tick >= 110)
            {
                EndAttack();
            }
        }

        private void Transition()
        {
            MoveTo(RoomCenter(), MoveSpeed);

            if (Tick == 1)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                Shake(5f, 30);
                Say("Phase2Line");
            }
            if (Main.netMode != NetmodeID.Server && Tick % 5 == 0)
            {
                // Ring of light closing in on him.
                float radius = 160f * (1f - Tick / (float)TransitionTicks);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 12f + Tick * 0.05f).ToRotationVector2() * radius;
                    Dust d = Dust.NewDustPerfect(NPC.Center + offset, DustID.PurpleTorch, -offset * 0.02f, 100, default, 1.6f);
                    d.noGravity = true;
                }
            }
            if (Tick == TransitionTicks - 10)
            {
                SoundEngine.PlaySound(SoundID.Item74, NPC.Center);
                Shake(8f, 25);
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        Dust d = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(10f, 10f), 100, default, 2f);
                        d.noGravity = true;
                    }
                }
            }

            Timer++;
            if (Tick >= TransitionTicks)
            {
                Phase = 2f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SummonGuards();
                    guardTimer = 0;
                }
                EndAttack();
            }
        }

        private void Despawn()
        {
            NPC.velocity.Y -= 0.2f;
            NPC.alpha = Math.Min(255, NPC.alpha + 255 / DespawnTicks);
            Timer++;
            if (Tick >= DespawnTicks)
            {
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        // ================= Helpers =================

        private void MoveTo(Vector2 target, float speed)
        {
            Vector2 toTarget = target - NPC.Center;
            Vector2 desired = toTarget.Length() > speed ? toTarget.SafeNormalize(Vector2.Zero) * speed : toTarget;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desired, MoveAccel);
        }

        private Vector2 HoverPoint(Player player)
        {
            float side = Aux == 0f ? 1f : Math.Sign(Aux);
            return ClampToRoom(player.Center + new Vector2(side * HoverDistance, -HoverHeight));
        }

        private Vector2 AimAt(Player player)
        {
            return (player.Center + player.velocity * PredictFactor - NPC.Center).SafeNormalize(Vector2.UnitX * NPC.direction);
        }

        private void Shoot(Vector2 velocity, int damage, float mode, float ai1 = 0f)
        {
            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity, ModContent.ProjectileType<VolodarArrow>(),
                damage, 0f, Main.myPlayer, mode, ai1);
        }

        // Keeps a position inside the throne room (below the arches, away from the walls).
        private static Vector2 ClampToRoom(Vector2 position)
        {
            if (!MysticalLibrarySystem.Exists)
            {
                return position;
            }
            Rectangle room = MysticalLibrarySystem.Layout.ThroneRoom;
            float left = (room.X + RoomMarginTiles) * 16f;
            float right = (room.Right - RoomMarginTiles) * 16f;
            float top = (room.Y + MysticalLibraryBuilder.ThroneArchHeight + 3) * 16f;
            float bottom = (room.Bottom - RoomMarginTiles) * 16f;
            return new Vector2(MathHelper.Clamp(position.X, left, right), MathHelper.Clamp(position.Y, top, bottom));
        }

        private Vector2 RoomCenter()
        {
            if (!MysticalLibrarySystem.Exists)
            {
                return NPC.Center;
            }
            Rectangle room = MysticalLibrarySystem.Layout.ThroneRoom;
            return ClampToRoom(new Vector2(room.Center.X * 16f, (room.Y + room.Height / 2) * 16f));
        }

        private Vector2 BeamSpot(Player player)
        {
            if (!MysticalLibrarySystem.Exists)
            {
                return player.Center + new Vector2(0f, -350f);
            }
            Rectangle room = MysticalLibrarySystem.Layout.ThroneRoom;
            return ClampToRoom(new Vector2(room.Center.X * 16f, room.Y * 16f));
        }

        private float RainSpawnY(Player player)
        {
            if (!MysticalLibrarySystem.Exists)
            {
                return player.Center.Y - 650f;
            }
            return (MysticalLibrarySystem.Layout.ThroneRoom.Y + MysticalLibraryBuilder.ThroneArchHeight + 1) * 16f;
        }

        // Elf guards march in through the throne room entrance (server only). Spawned as "statue" NPCs so they drop nothing.
        private void SummonGuards()
        {
            if (!MysticalLibrarySystem.Exists)
            {
                return;
            }

            int squire = ModContent.NPCType<ElfSquire>();
            int highGuard = ModContent.NPCType<ElfHighGuard>();
            int alive = 0;
            foreach (NPC other in Main.ActiveNPCs)
            {
                if ((other.type == squire || other.type == highGuard) && other.SpawnedFromStatue)
                {
                    alive++;
                }
            }

            Point entrance = MysticalLibraryBuilder.ThroneRoomEntrance(MysticalLibrarySystem.Layout);
            int side = MysticalLibrarySystem.Layout.Side;
            for (int i = alive; i < MaxGuards; i++)
            {
                int type = i % 2 == 0 ? squire : highGuard;
                int x = (entrance.X - side * (2 + i * 3)) * 16 + 8;
                int y = (entrance.Y + 1) * 16;
                int index = NPC.NewNPC(NPC.GetSource_FromAI(), x, y, type);
                if (index < Main.maxNPCs)
                {
                    Main.npc[index].SpawnedFromStatue = true;
                    Main.npc[index].netUpdate = true;
                }
            }
        }

        // Sparkling barrier across the throne room doorway while the fight is on.
        private static void BarrierSparkles()
        {
            if (Main.netMode == NetmodeID.Server || !MysticalLibrarySystem.Exists || !Main.rand.NextBool(2))
            {
                return;
            }
            LibraryLayout layout = MysticalLibrarySystem.Layout;
            Rectangle room = layout.ThroneRoom;
            float x = (layout.Side > 0 ? room.Right : room.X) * 16f;
            float y = Main.rand.NextFloat((room.Bottom - MysticalLibraryBuilder.StairHeadroom) * 16f, room.Bottom * 16f);
            Dust d = Dust.NewDustPerfect(new Vector2(x, y), DustID.PurpleTorch, new Vector2(0f, -1f), 100, default, 1.3f);
            d.noGravity = true;
        }

        private void Shake(float strength, int frames)
        {
            if (Main.netMode != NetmodeID.Server && Vector2.Distance(Main.LocalPlayer.Center, NPC.Center) < 2000f)
            {
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), strength, 6f, frames, 2000f, FullName));
            }
        }

        // ================= Death =================

        public override void OnKill()
        {
            Say("DefeatLine");
            NPC.SetEventFlagCleared(ref DownedBossSystem.downedVolodar, -1);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Expert/Master: the treasure bag (bow or grimoire, Spectral Sigil, coins).
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<VolodarBag>()));

            // Normal mode: the bow or the grimoire, 50/50, and sometimes the mask (the bag handles Expert).
            var notExpert = new LeadingConditionRule(new Conditions.NotExpert());
            notExpert.OnSuccess(ItemDropRule.OneFromOptions(1, ModContent.ItemType<VolodarsBow>(), ModContent.ItemType<VolodarsGrimoire>()));
            notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<VolodarMask>(), MaskChance));
            npcLoot.Add(notExpert);

            // Every mode: trophy. Master Mode: relic.
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<VolodarTrophy>(), TrophyChance));
            npcLoot.Add(ItemDropRule.MasterModeCommonDrop(ModContent.ItemType<VolodarRelic>()));
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                ModContent.GetInstance<MysticalLibraryBiome>().ModBiomeBestiaryInfoElement,
                new FlavorTextBestiaryInfoElement("Mods.Reign_of_the_Ancients.Bestiary.Volodar"),
            });
        }

        // Chat line from the localization file (NPCs.Volodar.<key>). Leave a line empty to skip it.
        private static void Say(string key)
        {
            string fullKey = "Mods.Reign_of_the_Ancients.NPCs.Volodar." + key;
            if (string.IsNullOrWhiteSpace(Language.GetTextValue(fullKey)))
            {
                return;
            }
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(Language.GetTextValue(fullKey), ChatColor);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey(fullKey), ChatColor);
            }
        }

        // ================= Drawing =================

        public override void FindFrame(int frameHeight)
        {
            if (++NPC.frameCounter >= FrameDelay)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;
            Vector2 origin = NPC.frame.Size() / 2f;
            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color tint = IsPhase2 ? new Color(190, 140, 255) : new Color(140, 190, 255);
            float opacity = 1f - NPC.alpha / 255f;

            // Telegraph for the charged shot: thin while tracking, bright once locked.
            if (State == StateCharged && Tick < ChargedTrackTicks + ChargedLockTicks)
            {
                bool locked = Tick >= ChargedTrackTicks;
                float progress = Tick / (float)(ChargedTrackTicks + ChargedLockTicks);
                Color line = (locked ? new Color(220, 250, 255) : new Color(150, 200, 255)) * (0.25f + 0.6f * progress);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, NPC.Center - screenPos, new Rectangle(0, 0, 1, 1), line with { A = 0 },
                    Aux, new Vector2(0f, 0.5f), new Vector2(1400f, locked ? 4f : 2f), SpriteEffects.None, 0f);
            }

            // Afterimages while moving fast, always in phase 2.
            if (IsPhase2 || NPC.velocity.Length() > 4f)
            {
                for (int i = NPC.oldPos.Length - 1; i >= 0; i--)
                {
                    float fade = 1f - (i + 1f) / (NPC.oldPos.Length + 1f);
                    Vector2 pos = NPC.oldPos[i] + NPC.Size / 2f - screenPos;
                    spriteBatch.Draw(texture, pos, NPC.frame, (tint * 0.35f * fade * opacity) with { A = 0 }, 0f, origin, NPC.scale, effects, 0f);
                }
            }

            // Pulsing aura in phase 2.
            if (IsPhase2)
            {
                float pulse = 1.08f + 0.04f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f);
                spriteBatch.Draw(texture, NPC.Center - screenPos, NPC.frame, (tint * 0.5f * opacity) with { A = 0 }, 0f, origin, NPC.scale * pulse, effects, 0f);
            }

            spriteBatch.Draw(texture, NPC.Center - screenPos, NPC.frame, NPC.GetAlpha(drawColor), 0f, origin, NPC.scale, effects, 0f);
            return false;
        }
    }
}
