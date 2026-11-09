using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace Reign_of_the_Ancients.NPCs.Elves
{
    public class ElfHighGuard : ModNPC
    {
        private const float WalkSpeed = 1.2f;
        private const float ThrustRange = 90f;
        private const int WindupTicks = 40;
        private const float LungeSpeed = 8f;
        private const int ThrustDurationTicks = 12;
        private const int ThrustCooldownTicks = 60;
        private const int ThrustReach = 40;
        private const int ThrustDamage = 26;
        private const int FrameDelay = 7;

        private const float StateApproach = 0f;
        private const float StateWindup = 1f;
        private const float StateThrust = 2f;
        private const float StateCooldown = 3f;

        private bool hasHitThisThrust;
        private int moveDir = 1;
        private int lockedDir = 1;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 50;
            NPC.damage = 0; // contact damage disabled — all damage comes from the thrust hitbox
            NPC.defense = 18;
            NPC.lifeMax = 120;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 140f;
            NPC.knockBackResist = 0.25f;
            NPC.aiStyle = -1;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.stepSpeed = 1f;
        }

        public override void AI()
        {
            if (NPC.target < 0 ||
                NPC.target == 255 ||
                !Main.player[NPC.target].active ||
                Main.player[NPC.target].dead)
            {
                NPC.TargetClosest();
            }

            Player player = Main.player[NPC.target];

            if (!player.active || player.dead)
            {
                NPC.velocity.X *= 0.9f;
                return;
            }

            moveDir = player.Center.X > NPC.Center.X ? 1 : -1;
            if (NPC.ai[0] == StateApproach || NPC.ai[0] == StateCooldown)
            {
                lockedDir = moveDir;
            }

            NPC.direction = -lockedDir;
            NPC.spriteDirection = NPC.direction;

            switch (NPC.ai[0])
            {
                case StateApproach:
                    HandleApproach(player);
                    break;

                case StateWindup:
                    HandleWindup();
                    break;

                case StateThrust:
                    HandleThrust(player);
                    break;

                case StateCooldown:
                    HandleCooldown(player);
                    break;
            }

            Collision.StepUp(
                ref NPC.position,
                ref NPC.velocity,
                NPC.width,
                NPC.height,
                ref NPC.stepSpeed,
                ref NPC.gfxOffY
            );
        }

        private void HandleApproach(Player player)
        {
            NPC.velocity.X = moveDir * WalkSpeed;

            float horizontalDistance = System.Math.Abs(
                player.Center.X - NPC.Center.X
            );

            if (horizontalDistance <= ThrustRange)
            {
                NPC.ai[0] = StateWindup;
                NPC.ai[1] = 0f;
                NPC.velocity.X = 0f;
            }
        }

        private void HandleWindup()
        {
            NPC.velocity.X *= 0.8f;
            NPC.ai[1]++;

            if (NPC.ai[1] >= WindupTicks)
            {
                NPC.ai[0] = StateThrust;
                NPC.ai[1] = 0f;
                hasHitThisThrust = false;

                NPC.velocity.X = lockedDir * LungeSpeed;
            }
        }

        private void HandleThrust(Player player)
        {
            NPC.ai[1]++;

            // Hold lunge speed for the whole thrust — no decay mid-lunge.
            NPC.velocity.X = lockedDir * LungeSpeed;

            if (!hasHitThisThrust)
            {
                Rectangle thrustHitbox = GetThrustHitbox();

                if (thrustHitbox.Intersects(player.Hitbox))
                {
                    player.Hurt(
                        PlayerDeathReason.ByNPC(NPC.whoAmI),
                        ThrustDamage,
                        lockedDir,
                        false,
                        false
                    );

                    hasHitThisThrust = true;
                }
            }

            if (NPC.ai[1] >= ThrustDurationTicks)
            {
                NPC.ai[0] = StateCooldown;
                NPC.ai[1] = 0f;
                // no hard stop here — HandleCooldown decays the lunge into a slide
            }
        }

        private void HandleCooldown(Player player)
        {
            // Decay lunge momentum into a slide, then transition to
            // walking toward the player once it's mostly bled off.
            NPC.velocity.X *= 0.9f;

            if (System.Math.Abs(NPC.velocity.X) < 0.3f)
            {
                NPC.velocity.X = moveDir * WalkSpeed;
            }

            NPC.ai[1]++;

            if (NPC.ai[1] >= ThrustCooldownTicks)
            {
                NPC.ai[0] = StateApproach;
                NPC.ai[1] = 0f;
            }
        }

        private Rectangle GetThrustHitbox()
        {
            int y = (int)NPC.position.Y;

            if (lockedDir > 0)
            {
                int x = (int)(NPC.position.X + NPC.width);
                return new Rectangle(x, y, ThrustReach, NPC.height);
            }
            else
            {
                int x = (int)(NPC.position.X - ThrustReach);
                return new Rectangle(x, y, ThrustReach, NPC.height);
            }
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

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ElvenGreatsword>(), 1));
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return 0f;
        }
    }
}