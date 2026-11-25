using System;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Common.Visuals;
using Reign_of_the_Ancients.Items.Placeable.Banners;
using Reign_of_the_Ancients.Projectiles.Dwarves;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Dwarves
{
    // Rusted Mech Pilot (placeholder name). A rarer mini-elite in an old, creaking mech.
    // At range it fires bursts of bullets; up close it vents steam, shudders, then charges across the floor
    // and is stunned for a moment afterwards (the window to punish it).
    public class DwarfMechPilot : DwarfGruntBase
    {
        private const float WalkSpeed = 0.9f;
        private const float KeepDistance = 8 * 16f;
        private const int BurstInterval = 120;
        private const int BurstShots = 3;
        private const int BurstGap = 8;
        private const float ShootRange = 40 * 16f;
        private const float BulletSpeed = 10f;
        private const int BulletDamage = 22;

        private const float ChargeRange = 12 * 16f;
        private const int ChargeWindupTicks = 40;
        private const float ChargeSpeed = 9f;
        private const int ChargeTicks = 50;
        private const int StunTicks = 45;
        private const int ChargeCooldownTicks = 200;
        private const int ContactDamage = 22;
        private const int ChargeDamage = 45;

        private const float StateWalk = 0f;
        private const float StateWindup = 1f;
        private const float StateCharge = 2f;
        private const float StateStunned = 3f;

        private ref float State => ref NPC.ai[0];
        private ref float Timer => ref NPC.ai[1];
        private ref float ChargeCooldown => ref NPC.ai[2];

        private static readonly Color ChargeLineColor = new Color(255, 80, 50);

        protected override int BannerItemType => ModContent.ItemType<DwarfMechPilotBanner>();
        protected override bool HasAttackFrame => false;
        protected override int HitDust => DustID.Torch;
        protected override int DeathDustAmount => 40;

        // Red line along the charge path while it winds up.
        protected override void DrawTelegraphs()
        {
            if (State != StateWindup)
            {
                return;
            }
            Vector2 start = NPC.Center + new Vector2(NPC.direction * NPC.width / 2f, 8f);
            float angle = NPC.direction > 0 ? 0f : MathHelper.Pi;
            BeamRenderer.DrawTelegraph(start, angle, ChargeSpeed * ChargeTicks * 0.6f, 1.6f, ChargeLineColor, Timer / ChargeWindupTicks);
        }

        // The old mech bursts apart: fire, smoke and a bang.
        protected override void OnDeathEffects()
        {
            SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
            for (int i = 0; i < 30; i++)
            {
                Dust smoke = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                smoke.velocity *= 2f;
                Dust fire = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, 0f, 100, default, 2.5f);
                fire.noGravity = true;
                fire.velocity *= 3f;
            }
            if (!Main.dedServ)
            {
                for (int i = 0; i < 4; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.Center - new Vector2(16f), Main.rand.NextVector2Circular(3f, 3f), GoreID.Smoke1 + Main.rand.Next(3));
                }
            }
        }

        protected override float SpawnWeight(int zone) => zone switch
        {
            DwarvenHold.Forges => 0.25f,
            DwarvenHold.Mines => 0.2f,
            _ => 0f,
        };

        public override void SetDefaults()
        {
            CommonDefaults(40, 52);
            NPC.damage = ContactDamage;
            NPC.defense = 16;
            NPC.lifeMax = 320;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 600f;
            NPC.knockBackResist = 0.1f;
        }

        public override void AI()
        {
            if (!UpdateTarget())
            {
                return;
            }
            int dir = DirToTarget;
            float dist = DistToTarget;
            Timer++;
            if (ChargeCooldown > 0f)
            {
                ChargeCooldown--;
            }
            NPC.damage = State == StateCharge ? ChargeDamage : ContactDamage;

            switch (State)
            {
                case StateWindup:
                    Brake(0.6f);
                    if (Main.rand.NextBool(2))
                    {
                        Dust steam = Dust.NewDustDirect(NPC.position, NPC.width, 10, DustID.Smoke, 0f, -2f, 80, default, 1.4f);
                        steam.noGravity = true;
                    }
                    if (Timer >= ChargeWindupTicks)
                    {
                        State = StateCharge;
                        Timer = 0f;
                        SoundEngine.PlaySound(SoundID.Item22 with { Pitch = -0.4f }, NPC.Center);
                    }
                    break;

                case StateCharge:
                    NPC.velocity.X = NPC.direction * ChargeSpeed;
                    JumpIfBlocked();
                    if (Main.rand.NextBool(2))
                    {
                        Dust spark = Dust.NewDustDirect(NPC.BottomLeft - new Vector2(0f, 6f), NPC.width, 6, DustID.Torch, -NPC.direction * 2f, -1f, 100, default, 1.2f);
                        spark.noGravity = true;
                    }
                    if (Timer >= ChargeTicks || (NPC.collideX && Timer > 5))
                    {
                        State = StateStunned;
                        Timer = 0f;
                        if (NPC.collideX)
                        {
                            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f }, NPC.Center); // slammed into a wall
                        }
                    }
                    break;

                case StateStunned:
                    Brake(0.9f);
                    if (Main.rand.NextBool(3))
                    {
                        Dust steam = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height / 2, DustID.Smoke, 0f, -1.5f, 100, default, 1.2f);
                        steam.noGravity = true;
                    }
                    if (Timer >= StunTicks)
                    {
                        State = StateWalk;
                        Timer = 0f;
                        ChargeCooldown = ChargeCooldownTicks;
                    }
                    break;

                default:
                    Face(dir);
                    if (dist > KeepDistance || !CanSeeTarget)
                    {
                        Walk(dir, WalkSpeed);
                    }
                    else
                    {
                        Brake();
                    }

                    // Burst fire: one bullet every BurstGap ticks once the interval is up.
                    if (Timer >= BurstInterval && CanSeeTarget && dist < ShootRange)
                    {
                        int sinceStart = (int)Timer - BurstInterval;
                        if (sinceStart % BurstGap == 0)
                        {
                            Shoot(dir);
                        }
                        if (sinceStart >= BurstGap * (BurstShots - 1))
                        {
                            Timer = 0f;
                        }
                    }

                    if (dist < ChargeRange && ChargeCooldown <= 0f && Grounded && CanSeeTarget && IsServer)
                    {
                        State = StateWindup;
                        Timer = 0f;
                        NPC.netUpdate = true;
                    }
                    break;
            }
            StepUp();
        }

        private void Shoot(int dir)
        {
            Vector2 muzzle = NPC.Center + new Vector2(dir * 20f, -10f);
            SoundEngine.PlaySound(SoundID.Item11, NPC.Center);
            Dust flash = Dust.NewDustPerfect(muzzle, DustID.Torch, new Vector2(dir * 2f, 0f), 100, default, 1.3f);
            flash.noGravity = true;
            if (IsServer)
            {
                Vector2 velocity = (Target.Center - muzzle).SafeNormalize(Vector2.UnitX * dir) * BulletSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), muzzle, velocity.RotatedByRandom(0.05f), ModContent.ProjectileType<DwarvenPellet>(),
                    BulletDamage, 1f, Main.myPlayer, DwarvenPellet.BulletMode);
            }
        }
    }
}
