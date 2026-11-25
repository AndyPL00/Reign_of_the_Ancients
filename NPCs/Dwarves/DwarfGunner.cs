using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Projectiles.Dwarves;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Dwarves
{
    // Corrupted Blunderbuss Gunner (placeholder name). Closes to mid range, plants its feet and aims (smoke builds
    // at the muzzle as a warning), then blasts a shotgun spread of hot pellets and staggers back from the recoil.
    public class DwarfGunner : DwarfGruntBase
    {
        private const float WalkSpeed = 1.2f;
        private const float FireRange = 14 * 16f;
        private const float TooClose = 6 * 16f;
        private const int AimTicks = 32;
        private const int ReloadTicks = 100;
        private const int Pellets = 5;
        private const float SpreadRadians = 0.42f;   // total cone
        private const float PelletSpeed = 8.5f;
        private const int PelletDamage = 16;
        private const float Recoil = 3f;

        private const float StateMove = 0f;
        private const float StateAim = 1f;

        private ref float State => ref NPC.ai[0];
        private ref float Timer => ref NPC.ai[1];

        protected override float SpawnWeight(int zone) => zone switch
        {
            DwarvenHold.GreatHall => 1f,
            DwarvenHold.Forges => 1f,
            _ => 0f,
        };

        public override void SetDefaults()
        {
            CommonDefaults(28, 40);
            NPC.damage = 20;
            NPC.defense = 10;
            NPC.lifeMax = 110;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 180f;
            NPC.knockBackResist = 0.4f;
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

            if (State == StateAim)
            {
                Face(dir);
                Brake(0.7f);
                Vector2 muzzle = Muzzle();
                if (Main.rand.NextFloat() < Timer / AimTicks)
                {
                    Dust smoke = Dust.NewDustPerfect(muzzle, DustID.Smoke, new Vector2(dir * 0.5f, -0.6f), 120, default, 1.1f);
                    smoke.noGravity = true;
                }
                if (Timer >= AimTicks)
                {
                    Fire(dir, muzzle);
                    State = StateMove;
                    Timer = 0f;
                }
            }
            else
            {
                Face(dir);
                if (dist > FireRange || !CanSeeTarget)
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

                if (Timer >= ReloadTicks && dist < FireRange && CanSeeTarget && Grounded && IsServer)
                {
                    State = StateAim;
                    Timer = 0f;
                    NPC.netUpdate = true;
                }
            }
            StepUp();
        }

        private Vector2 Muzzle() => NPC.Center + new Vector2(NPC.direction * 16f, -2f);

        private void Fire(int dir, Vector2 muzzle)
        {
            SoundEngine.PlaySound(SoundID.Item36, NPC.Center);
            for (int i = 0; i < 10; i++)
            {
                Dust flash = Dust.NewDustPerfect(muzzle, DustID.Torch, new Vector2(dir * Main.rand.NextFloat(2f, 5f), Main.rand.NextFloat(-1.5f, 1.5f)), 100, default, 1.3f);
                flash.noGravity = true;
            }
            NPC.velocity.X = -dir * Recoil;

            if (!IsServer)
            {
                return;
            }
            Vector2 aim = (Target.Center - muzzle).SafeNormalize(Vector2.UnitX * dir);
            for (int i = 0; i < Pellets; i++)
            {
                float offset = MathHelper.Lerp(-SpreadRadians / 2f, SpreadRadians / 2f, i / (float)(Pellets - 1)) + Main.rand.NextFloat(-0.04f, 0.04f);
                Vector2 velocity = aim.RotatedBy(offset) * PelletSpeed * Main.rand.NextFloat(0.9f, 1.1f);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), muzzle, velocity, ModContent.ProjectileType<DwarvenPellet>(),
                    PelletDamage, 1f, Main.myPlayer, DwarvenPellet.PelletMode);
            }
        }
    }
}
