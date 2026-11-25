using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Common.Visuals;
using Reign_of_the_Ancients.Projectiles.Bosses.Titan;
using Reign_of_the_Ancients.WorldGeneration;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Bosses
{
    // The Dwarven Titan (placeholder name): an ancient, self-running dwarven war machine on treads, woken at the
    // control machine in the hold's foundry. Slow, ground-bound (never jumps), heavy-hitting. No dialogue.
    //
    // Two arm cannons (DwarvenTitanCannon) are separate, optional targets: destroying one removes its cannon
    // attacks and makes the Titan a little faster and angrier.
    // Phase 1 (plated, takes reduced damage): cannon barrage, ground slam shockwaves, tread ram, homing steam rockets.
    //
    // Multiplayer: the server picks attacks and spawns projectiles; state lives in ai[] and syncs via netUpdate.
    [AutoloadBossHead]
    public class DwarvenTitan : ModNPC
    {
        // ---- Stats ----
        private const int BaseLife = 6500;
        private const int BaseDefense = 18;
        private const int ContactDamage = 40;
        private const int RamDamage = 60;
        private const float PlatedDamageTaken = 0.65f;   // phase 1: plates soak 35% of every hit
        private const int Phase1Music = MusicID.Boss2;
        private const int Phase2Music = MusicID.Boss3;

        // ---- Movement ----
        private const float DriveSpeed = 1.6f;
        private const float DriveAccel = 0.06f;
        private const float StopDistance = 48f;          // stops driving when this close (horizontally) to the target
        private const float RageSpeedPerLostArm = 0.12f; // speed/attack-rate bonus per destroyed arm cannon
        private const float DespawnDistance = 3500f;
        private const int EntranceMarginTiles = 17;      // keeps it off the entrance dais and the control machine
        private const int FarMarginTiles = 9;            // keeps it out of the far lava channel

        // ---- Timing (ticks, 60 = 1 second) ----
        private const int IntroTicks = 150;
        private const int IntroDriveStart = 70;
        private const int IdleTicksPhase1 = 70;
        private const int DespawnTicks = 90;

        // ---- Projectile damage. Vanilla multiplies hostile projectile damage by 2 (normal), 4 (expert), 6 (master). ----
        public const int ShellDamage = 20;
        private const int ShockwaveDamage = 18;
        private const int RocketDamage = 16;

        // ---- Attack tuning ----
        public const int CannonWindupTicks = 40;          // arm cannons aim (with a telegraph line) before firing
        public const int CannonShots = 3;
        public const int CannonGapTicks = 14;
        public const int ShellFlightTicks = 55;
        private const int SlamWindupTicks = 45;
        private const int SlamRecoverTicks = 40;
        private const float SlamTilt = 0.12f;             // radians the front lifts while rearing up
        private const float ShockwaveSpeed = 7f;
        private const int RamWindupTicks = 55;
        private const float RamSpeed = 11f;
        private const int RamMaxTicks = 90;
        private const int StunTicks = 75;
        private const int RocketCountPhase1 = 2;
        private const int RocketGapTicks = 18;
        private const float RocketLaunchSpeed = 6f;

        // ---- States (ai[0]) ----
        public const float StateIntro = 0f;
        public const float StateDrive = 1f;
        public const float StateCannons = 2f;
        public const float StateSlam = 3f;
        public const float StateRam = 4f;
        public const float StateStunned = 5f;
        public const float StateRockets = 6f;
        public const float StateDespawn = 10f;

        private static readonly float[] Phase1Attacks = { StateCannons, StateSlam, StateRam, StateRockets };

        private static readonly Color RamLineColor = new Color(255, 70, 40);

        private ref float State => ref NPC.ai[0];
        private ref float Timer => ref NPC.ai[1];
        private ref float Phase => ref NPC.ai[2];   // 0/1 = phase 1 (plated), 2 = phase 2 (core exposed)
        private ref float Aux => ref NPC.ai[3];     // ram direction (+1/-1)

        private bool IsPhase2 => Phase >= 2f;
        private int Tick => (int)Timer;

        // Server-only bookkeeping.
        private float lastAttack = -1f;

        // Tread animation, purely visual.
        private float treadFrame;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 8; // 4 tread frames plated, 4 with the core exposed
            NPCID.Sets.MPAllowedEnemies[Type] = true; // needed for summoning in multiplayer
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.TrailCacheLength[Type] = 4;
            NPCID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            NPC.width = 160;
            NPC.height = 120;
            NPC.damage = ContactDamage;
            NPC.defense = BaseDefense;
            NPC.lifeMax = BaseLife;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = Item.buyPrice(gold: 7);
            NPC.aiStyle = -1;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.npcSlots = 12f;
            Music = Phase1Music;
        }

        // ================= Shared with the arm cannons =================

        // Where an arm cannon's shoulder joint sits (side -1 = left pylon, +1 = right pylon).
        public static Vector2 ArmAnchor(NPC titan, int side) => titan.Center + new Vector2(side * 100f, -50f);

        public static int AliveArms(NPC titan)
        {
            int count = 0;
            int armType = ModContent.NPCType<DwarvenTitanCannon>();
            foreach (NPC other in Main.ActiveNPCs)
            {
                if (other.type == armType && (int)other.ai[0] == titan.whoAmI)
                {
                    count++;
                }
            }
            return count;
        }

        // 1 with both arms, a little more for each one destroyed (after the intro, when the arms exist).
        private float Rage => State == StateIntro ? 1f : 1f + RageSpeedPerLostArm * (2 - AliveArms(NPC));

        // ================= AI =================

        public override void AI()
        {
            if (State != StateDespawn && State != StateIntro && !HasValidTarget())
            {
                NPC.TargetClosest();
                if (!HasValidTarget())
                {
                    SetState(StateDespawn);
                }
            }
            if (State == StateIntro && Tick == 0)
            {
                NPC.TargetClosest();
            }

            Player player = Main.player[NPC.target];
            NPC.dontTakeDamage = State == StateIntro || State == StateDespawn;
            NPC.damage = State == StateRam && Tick >= RamWindupTicks ? RamDamage : ContactDamage;
            Music = IsPhase2 ? Phase2Music : Phase1Music;

            if (State != StateRam && State != StateStunned && State != StateDespawn)
            {
                NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            }
            NPC.spriteDirection = -NPC.direction; // art faces right

            switch (State)
            {
                case StateIntro: Intro(); break;
                case StateDrive: Drive(player); break;
                case StateCannons: Cannons(); break;
                case StateSlam: Slam(); break;
                case StateRam: Ram(); break;
                case StateStunned: Stunned(); break;
                case StateRockets: Rockets(player); break;
                case StateDespawn: Despawn(); break;
            }

            KeepInArena();
            EngineEffects();
            Lighting.AddLight(NPC.Center, IsPhase2 ? new Vector3(0.8f, 0.4f, 0.1f) : new Vector3(0.45f, 0.25f, 0.08f));
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

        private void EndAttack()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.TargetClosest();
            }
            SetState(StateDrive);
        }

        // ================= States =================

        // Rolls out of a burst of steam at the far end of the foundry, klaxon blaring, then deploys its arm cannons.
        private void Intro()
        {
            if (Tick == 0)
            {
                if (DwarvenHoldSystem.Exists && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    GetArena(out float left, out float right, out float floor);
                    float x = DwarvenHoldSystem.Layout.Side > 0 ? right : left;
                    NPC.Bottom = new Vector2(x, floor);
                    NPC.netUpdate = true;
                }
                NPC.velocity = Vector2.Zero;
                NPC.alpha = 255;
                SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.6f }, NPC.Center);
            }

            NPC.alpha = Math.Max(0, NPC.alpha - 6);
            if (Main.netMode != NetmodeID.Server && Tick < 90)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust steam = Dust.NewDustDirect(NPC.position - new Vector2(40f, 20f), NPC.width + 80, NPC.height + 20, DustID.Smoke, 0f, -1.5f, 120, default, 2.2f);
                    steam.noGravity = true;
                }
            }
            if (Tick == 40 || Tick == 80)
            {
                SoundEngine.PlaySound(SoundID.Item113 with { Pitch = -0.8f }, NPC.Center); // klaxon placeholder
            }

            if (Tick >= IntroDriveStart)
            {
                float inward = DwarvenHoldSystem.Exists ? -DwarvenHoldSystem.Layout.Side : NPC.direction;
                NPC.velocity.X = MoveTowards(NPC.velocity.X, inward * DriveSpeed, DriveAccel);
            }

            if (Tick == IntroTicks - 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnArms();
            }

            Timer++;
            if (Tick >= IntroTicks)
            {
                EndAttack();
            }
        }

        private void SpawnArms()
        {
            int armType = ModContent.NPCType<DwarvenTitanCannon>();
            foreach (int side in new[] { -1, 1 })
            {
                Vector2 anchor = ArmAnchor(NPC, side);
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)anchor.X, (int)anchor.Y, armType, NPC.whoAmI + 1, NPC.whoAmI, side);
            }
            SoundEngine.PlaySound(SoundID.Item149, NPC.Center);
        }

        private void Drive(Player player)
        {
            float dx = player.Center.X - NPC.Center.X;
            float target = Math.Abs(dx) > StopDistance ? Math.Sign(dx) * DriveSpeed * Rage : 0f;
            NPC.velocity.X = MoveTowards(NPC.velocity.X, target, DriveAccel);

            Timer++;
            int idleTicks = (int)(IdleTicksPhase1 / Rage);
            if (Tick >= idleTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float next;
                int guard = 0;
                do
                {
                    next = Phase1Attacks[Main.rand.Next(Phase1Attacks.Length)];
                }
                while ((next == lastAttack || (next == StateCannons && AliveArms(NPC) == 0)) && ++guard < 20);
                lastAttack = next;
                SetState(next);
            }
        }

        // The arm cannons do the aiming and firing themselves (they read this state and timer); the body just brakes.
        private void Cannons()
        {
            Brake(0.9f);
            Timer++;
            if (Tick >= CannonWindupTicks + CannonShots * CannonGapTicks + 30)
            {
                EndAttack();
            }
        }

        // Rears up on its treads, then slams down: shockwaves run along the floor both ways (jump over them).
        private void Slam()
        {
            Brake(0.85f);
            if (Tick == 1)
            {
                SoundEngine.PlaySound(SoundID.Item23 with { Pitch = -0.7f }, NPC.Center);
            }
            if (Tick < SlamWindupTicks && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust steam = Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 12f), NPC.width, 12, DustID.Smoke, 0f, -2f, 100, default, 1.6f);
                steam.noGravity = true;
            }
            if (Tick == SlamWindupTicks)
            {
                SlamImpact();
            }

            Timer++;
            if (Tick >= SlamWindupTicks + SlamRecoverTicks)
            {
                EndAttack();
            }
        }

        private void SlamImpact()
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.5f, Volume = 1.2f }, NPC.Center);
            Shake(9f, 30);
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 40; i++)
                {
                    Dust d = Dust.NewDustDirect(NPC.BottomLeft - new Vector2(30f, 8f), NPC.width + 60, 8, DustID.Smoke, Main.rand.NextFloat(-6f, 6f), -2f, 100, default, 2f);
                    d.noGravity = true;
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                foreach (int dir in new[] { -1, 1 })
                {
                    Vector2 start = new Vector2(NPC.Center.X + dir * NPC.width / 2f, NPC.Bottom.Y);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), start, new Vector2(dir * ShockwaveSpeed, 0f),
                        ModContent.ProjectileType<TitanShockwave>(), ShockwaveDamage, 4f, Main.myPlayer, NPC.Bottom.Y);
                }
            }
        }

        // Revs up (smoke from the treads and a red line along the floor), then rams across the arena.
        // Slamming into the end of the arena stuns it: the window to punish it.
        private void Ram()
        {
            if (Tick == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Aux = Main.player[NPC.target].Center.X > NPC.Center.X ? 1f : -1f;
                NPC.netUpdate = true;
            }
            int dir = Aux >= 0f ? 1 : -1;
            NPC.direction = dir;

            if (Tick < RamWindupTicks)
            {
                Brake(0.8f);
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    Dust smoke = Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 16f), NPC.width, 16, DustID.Smoke, -dir * 3f, -1f, 100, default, 1.8f);
                    smoke.noGravity = true;
                }
                if (Tick % 15 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item22 with { Pitch = -0.6f + Tick / (float)RamWindupTicks * 0.5f }, NPC.Center);
                }
            }
            else
            {
                if (Tick == RamWindupTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.5f }, NPC.Center);
                    Shake(4f, 15);
                }
                NPC.velocity.X = dir * RamSpeed * Rage;
                if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
                {
                    Dust spark = Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 8f), NPC.width, 8, DustID.Torch, -dir * 4f, -2f, 100, default, 1.4f);
                    spark.noGravity = true;
                }

                bool hitEnd = AtArenaEnd(dir) || (NPC.collideX && Tick > RamWindupTicks + 5);
                if (hitEnd)
                {
                    SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
                    Shake(10f, 30);
                    NPC.velocity.X = -dir * 3f;
                    SetState(StateStunned);
                    return;
                }
                if (Tick >= RamWindupTicks + RamMaxTicks)
                {
                    EndAttack();
                    return;
                }
            }
            Timer++;
        }

        private void Stunned()
        {
            Brake(0.92f);
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust steam = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height / 2, DustID.Smoke, 0f, -2f, 100, default, 1.8f);
                steam.noGravity = true;
                if (Main.rand.NextBool(3))
                {
                    Dust spark = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Electric, 0f, 0f, 100, default, 0.8f);
                    spark.noGravity = true;
                }
            }
            Timer++;
            if (Tick >= StunTicks)
            {
                EndAttack();
            }
        }

        // Slow homing steam rockets launched from the pod on its back.
        private void Rockets(Player player)
        {
            Brake(0.9f);
            int count = RocketCountPhase1;
            for (int i = 0; i < count; i++)
            {
                if (Tick == 20 + i * RocketGapTicks)
                {
                    Vector2 pod = NPC.Top + new Vector2(-NPC.direction * 21f, -8f);
                    SoundEngine.PlaySound(SoundID.Item61, pod);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), -RocketLaunchSpeed);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), pod, velocity, ModContent.ProjectileType<TitanRocket>(),
                            RocketDamage, 2f, Main.myPlayer, player.whoAmI);
                    }
                }
            }
            Timer++;
            if (Tick >= 20 + count * RocketGapTicks + 40)
            {
                EndAttack();
            }
        }

        // Nobody left to fight: drives back to the far end and shuts down in a cloud of steam.
        private void Despawn()
        {
            float home = DwarvenHoldSystem.Exists ? DwarvenHoldSystem.Layout.Side : NPC.direction;
            NPC.velocity.X = MoveTowards(NPC.velocity.X, home * DriveSpeed, DriveAccel);
            NPC.alpha = Math.Min(255, NPC.alpha + 255 / DespawnTicks + 1);
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust steam = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, -1.5f, 120, default, 2f);
                steam.noGravity = true;
            }
            Timer++;
            if (Tick >= DespawnTicks)
            {
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        // ================= Helpers =================

        private static float MoveTowards(float current, float target, float step)
        {
            return current < target ? Math.Min(current + step, target) : Math.Max(current - step, target);
        }

        private void Brake(float factor)
        {
            NPC.velocity.X *= factor;
            if (Math.Abs(NPC.velocity.X) < 0.05f)
            {
                NPC.velocity.X = 0f;
            }
        }

        // Limits for the Titan's centre X inside the foundry (off the dais, out of the far lava channel), and the floor.
        private void GetArena(out float left, out float right, out float floor)
        {
            Rectangle f = DwarvenHoldSystem.Layout.Foundry;
            bool entranceLeft = DwarvenHoldSystem.Layout.Side > 0;
            int leftMargin = entranceLeft ? EntranceMarginTiles : FarMarginTiles;
            int rightMargin = entranceLeft ? FarMarginTiles : EntranceMarginTiles;
            left = (f.X + leftMargin) * 16f + NPC.width / 2f;
            right = (f.Right - rightMargin) * 16f - NPC.width / 2f;
            floor = f.Bottom * 16f;
        }

        private void KeepInArena()
        {
            if (!DwarvenHoldSystem.Exists || State == StateIntro || State == StateDespawn)
            {
                return;
            }
            GetArena(out float left, out float right, out _);
            if (NPC.Center.X < left)
            {
                NPC.Center = new Vector2(left, NPC.Center.Y);
                NPC.velocity.X = Math.Max(0f, NPC.velocity.X);
            }
            else if (NPC.Center.X > right)
            {
                NPC.Center = new Vector2(right, NPC.Center.Y);
                NPC.velocity.X = Math.Min(0f, NPC.velocity.X);
            }
        }

        private bool AtArenaEnd(int dir)
        {
            if (!DwarvenHoldSystem.Exists)
            {
                return false;
            }
            GetArena(out float left, out float right, out _);
            return dir > 0 ? NPC.Center.X >= right - 1f : NPC.Center.X <= left + 1f;
        }

        // Smoke from the stacks, a low engine rumble and tread dust while it moves.
        private void EngineEffects()
        {
            if (Main.netMode == NetmodeID.Server || NPC.alpha > 200)
            {
                return;
            }
            if (Main.rand.NextBool(IsPhase2 ? 3 : 5))
            {
                Vector2 stack = NPC.Top + new Vector2(-NPC.direction * 68f, -40f);
                Dust smoke = Dust.NewDustPerfect(stack + Main.rand.NextVector2Circular(6f, 2f), DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -2f), 120, default, 1.8f);
                smoke.noGravity = true;
            }
            if (Math.Abs(NPC.velocity.X) > 0.5f && Main.rand.NextBool(4))
            {
                Dust.NewDust(NPC.BottomLeft - new Vector2(0f, 6f), NPC.width, 6, DustID.Smoke, -NPC.velocity.X * 0.3f, -0.5f, 120, default, 1.2f);
            }
            if (Main.GameUpdateCount % 50 == 0 && Math.Abs(NPC.velocity.X) > 0.3f)
            {
                SoundEngine.PlaySound(SoundID.Item22 with { Pitch = -0.9f, Volume = 0.5f }, NPC.Center);
            }
        }

        private void Shake(float strength, int frames)
        {
            if (Main.netMode != NetmodeID.Server && Vector2.Distance(Main.LocalPlayer.Center, NPC.Center) < 2400f)
            {
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), strength, 6f, frames, 2400f, FullName));
            }
        }

        // ================= Damage =================

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (!IsPhase2)
            {
                modifiers.FinalDamage *= PlatedDamageTaken;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                Dust spark = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Torch, hit.HitDirection * 2f, -2f, 100, default, 1.2f);
                spark.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                SoundEngine.PlaySound(SoundID.Item62, NPC.Center);
                for (int i = 0; i < 80; i++)
                {
                    Dust smoke = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 2.5f);
                    smoke.velocity *= 3f;
                    Dust fire = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, 0f, 100, default, 3f);
                    fire.noGravity = true;
                    fire.velocity *= 5f;
                }
                for (int i = 0; i < 10; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.Center + Main.rand.NextVector2Circular(60f, 40f), Main.rand.NextVector2Circular(4f, 4f), GoreID.Smoke1 + Main.rand.Next(3), 1.5f);
                }
            }
        }

        public override bool CheckActive() => false;

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                ModContent.GetInstance<DwarvenHoldBiome>().ModBiomeBestiaryInfoElement,
                new FlavorTextBestiaryInfoElement("Mods.Reign_of_the_Ancients.Bestiary.DwarvenTitan"),
            });
        }

        // ================= Drawing =================

        public override void FindFrame(int frameHeight)
        {
            treadFrame = (treadFrame + Math.Abs(NPC.velocity.X) * 0.15f) % 4f;
            int frame = (int)treadFrame + (IsPhase2 ? 4 : 0);
            NPC.frame.Y = frame * frameHeight;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;
            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = new Vector2(NPC.frame.Width / 2f, NPC.frame.Height);

            // Red line along the floor while revving up for the ram.
            if (State == StateRam && Tick < RamWindupTicks)
            {
                int dir = Aux >= 0f ? 1 : -1;
                Vector2 start = new Vector2(NPC.Center.X + dir * NPC.width / 2f, NPC.Bottom.Y - 12f);
                float length = 1200f;
                if (DwarvenHoldSystem.Exists)
                {
                    GetArena(out float left, out float right, out _);
                    length = Math.Max(16f, dir > 0 ? right + NPC.width / 2f - start.X : start.X - (left - NPC.width / 2f));
                }
                BeamRenderer.DrawTelegraph(start, dir > 0 ? 0f : MathHelper.Pi, length, 2.5f, RamLineColor, Tick / (float)RamWindupTicks);
            }

            // Rearing up before a slam: the front lifts.
            float rotation = 0f;
            if (State == StateSlam && Tick < SlamWindupTicks)
            {
                rotation = -NPC.direction * SlamTilt * MathHelper.SmoothStep(0f, 1f, Tick / (float)SlamWindupTicks);
            }

            // Afterimages while ramming.
            if (State == StateRam && Tick >= RamWindupTicks)
            {
                for (int i = NPC.oldPos.Length - 1; i >= 0; i--)
                {
                    float fade = 1f - (i + 1f) / (NPC.oldPos.Length + 1f);
                    Vector2 pos = NPC.oldPos[i] + new Vector2(NPC.width / 2f, NPC.height) - screenPos;
                    spriteBatch.Draw(texture, pos, NPC.frame, (new Color(255, 120, 60) * 0.3f * fade) with { A = 0 }, 0f, origin, NPC.scale, effects, 0f);
                }
            }

            spriteBatch.Draw(texture, NPC.Bottom - screenPos + new Vector2(0f, 2f), NPC.frame, NPC.GetAlpha(drawColor), rotation, origin, NPC.scale, effects, 0f);
            return false;
        }
    }
}
