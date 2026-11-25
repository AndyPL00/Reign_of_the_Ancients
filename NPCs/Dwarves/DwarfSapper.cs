using System;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Projectiles.Dwarves;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Dwarves
{
    // Corrupted Sapper (placeholder name). Keeps its distance and lobs bombs in an arc.
    // Badly hurt, it lights its own powder keg and runs at you to blow itself up.
    public class DwarfSapper : DwarfGruntBase
    {
        private const float WalkSpeed = 1.4f;
        private const float TooClose = 7 * 16f;        // backs off inside this range
        private const float TooFar = 16 * 16f;         // walks closer outside this range
        private const float ThrowRange = 30 * 16f;
        private const int ThrowInterval = 110;
        private const int BombFlightTicks = 45;        // aimed to land on you after about this long
        private const float MaxThrowSpeed = 13f;
        private const int BombDamage = 30;

        private const float KamikazeLifeFraction = 0.3f;
        private const float KamikazeSpeed = 3.4f;
        private const int KamikazeFuseTicks = 150;     // blows up after this long even if it can't reach you
        private const float KamikazeTriggerRange = 40f;
        private const int KamikazeDamage = 50;

        private const float StateNormal = 0f;
        private const float StateKamikaze = 1f;

        private ref float State => ref NPC.ai[0];
        private ref float Timer => ref NPC.ai[1];

        protected override float SpawnWeight(int zone) => zone switch
        {
            DwarvenHold.GreatHall => 1f,
            DwarvenHold.Forges => 0.6f,
            DwarvenHold.Mines => 0.5f,
            _ => 0f,
        };

        public override void SetDefaults()
        {
            CommonDefaults(26, 40);
            NPC.damage = 18;
            NPC.defense = 8;
            NPC.lifeMax = 90;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 150f;
            NPC.knockBackResist = 0.5f;
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

            if (State == StateNormal && NPC.life < NPC.lifeMax * KamikazeLifeFraction && IsServer)
            {
                State = StateKamikaze;
                Timer = 0f;
                NPC.netUpdate = true;
            }

            if (State == StateKamikaze)
            {
                Kamikaze(dir, dist);
            }
            else
            {
                Face(dir);
                if (dist > TooFar || !CanSeeTarget)
                {
                    Walk(dir, WalkSpeed);
                }
                else if (dist < TooClose)
                {
                    Walk(-dir, WalkSpeed);
                }
                else
                {
                    Brake();
                }

                if (Timer >= ThrowInterval && dist < ThrowRange && CanSeeTarget)
                {
                    Timer = 0f;
                    if (IsServer)
                    {
                        ThrowBomb();
                    }
                }
            }
            StepUp();
        }

        // Ballistic aim: pick a velocity that reaches the target in BombFlightTicks under the bomb's gravity.
        private void ThrowBomb()
        {
            Vector2 start = NPC.Center + new Vector2(NPC.direction * 8f, -12f);
            Vector2 delta = Target.Center - start;
            float t = BombFlightTicks;
            Vector2 velocity = new Vector2(delta.X / t, delta.Y / t - 0.5f * DwarvenBomb.Gravity * t);
            if (velocity.Length() > MaxThrowSpeed)
            {
                velocity = velocity.SafeNormalize(Vector2.UnitX) * MaxThrowSpeed;
            }
            Projectile.NewProjectile(NPC.GetSource_FromAI(), start, velocity, ModContent.ProjectileType<DwarvenBomb>(),
                BombDamage, 3f, Main.myPlayer, DwarvenBomb.ThrownMode);
        }

        private void Kamikaze(int dir, float dist)
        {
            Face(dir);
            Walk(dir, KamikazeSpeed);

            // Lit keg on its back: sparks and smoke, getting more frantic.
            if (Main.rand.NextBool(Math.Max(1, 4 - (int)(Timer / 50))))
            {
                Dust spark = Dust.NewDustPerfect(NPC.Top + new Vector2(-NPC.direction * 6f, 4f), DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-1f, 1f), -2f), 100, default, 1.4f);
                spark.noGravity = true;
            }
            if (Timer % 30 == 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item13 with { Pitch = 0.4f }, NPC.Center); // fizzing fuse
            }

            if (IsServer && (dist < KamikazeTriggerRange || Timer >= KamikazeFuseTicks))
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DwarvenBomb>(),
                    KamikazeDamage, 6f, Main.myPlayer, DwarvenBomb.KamikazeMode);
                NPC.StrikeInstantKill();
            }
        }
    }
}
