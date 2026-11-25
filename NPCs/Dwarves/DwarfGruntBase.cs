using System;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Common;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Dwarves
{
    // Shared behaviour for the corrupted dwarf grunts of the Dwarven Hold: targeting, walking with step-up and
    // jumping over obstacles, facing (art faces RIGHT), line of sight, a 4-frame walk cycle and zone-based spawning.
    public abstract class DwarfGruntBase : ModNPC
    {
        protected const float JumpSpeed = 6.5f;
        protected const float WalkAcceleration = 0.12f;
        protected const int FrameDelay = 8;

        protected Player Target => Main.player[NPC.target];
        protected int DirToTarget => Target.Center.X > NPC.Center.X ? 1 : -1;
        protected float DistToTarget => Vector2.Distance(Target.Center, NPC.Center);
        protected bool Grounded => NPC.velocity.Y == 0f;
        protected bool CanSeeTarget => Collision.CanHitLine(NPC.position, NPC.width, NPC.height, Target.position, Target.width, Target.height);
        protected bool IsServer => Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient;

        // Relative spawn weight in each hold zone (0 = doesn't spawn there).
        protected abstract float SpawnWeight(int zone);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 4;
        }

        protected void CommonDefaults(int width, int height)
        {
            NPC.width = width;
            NPC.height = height;
            NPC.aiStyle = -1;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            SpawnModBiomes = new int[] { ModContent.GetInstance<DwarvenHoldBiome>().Type };
        }

        // Re-targets when needed; returns false (and slows down) if there's nobody to fight.
        protected bool UpdateTarget()
        {
            if (NPC.target < 0 || NPC.target == 255 || !Target.active || Target.dead)
            {
                NPC.TargetClosest();
            }
            if (!Target.active || Target.dead)
            {
                NPC.velocity.X *= 0.9f;
                return false;
            }
            return true;
        }

        protected void Face(int dir)
        {
            NPC.direction = dir;
            NPC.spriteDirection = -dir; // sprite art faces right
        }

        protected void Walk(int dir, float speed)
        {
            NPC.velocity.X = MoveTowards(NPC.velocity.X, dir * speed, WalkAcceleration);
            JumpIfBlocked();
        }

        protected void Brake(float factor = 0.85f)
        {
            NPC.velocity.X *= factor;
        }

        protected void JumpIfBlocked()
        {
            if (NPC.collideX && Grounded)
            {
                NPC.velocity.Y = -JumpSpeed;
            }
        }

        protected void StepUp()
        {
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
        }

        protected static float MoveTowards(float current, float target, float step)
        {
            return Math.Abs(target - current) <= step ? target : current + Math.Sign(target - current) * step;
        }

        public override void FindFrame(int frameHeight)
        {
            if (Math.Abs(NPC.velocity.X) > 0.2f && Grounded)
            {
                NPC.frameCounter += Math.Abs(NPC.velocity.X) / 1.5f;
                if (NPC.frameCounter >= FrameDelay)
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y = (NPC.frame.Y + frameHeight) % (frameHeight * Main.npcFrameCount[Type]);
                }
            }
            else
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return SpawnWeight(DwarvenHoldSpawns.ZoneAtSpawn(spawnInfo));
        }
    }
}
