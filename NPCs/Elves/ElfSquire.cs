using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Common;

namespace Reign_of_the_Ancients.NPCs.Elves
{
    public class ElfSquire : ModNPC
    {
        private const float LibrarySpawnChance = 0.8f; // relative weight vs. other elves on the same floor
        private const float WalkSpeed = 1.5f;
        private const int FrameDelay = 6;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 46;
            NPC.damage = 12;
            NPC.defense = 4;
            NPC.lifeMax = 35;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 70f;
            NPC.knockBackResist = 0.6f;
            NPC.aiStyle = -1;
            SpawnModBiomes = new int[] { ModContent.GetInstance<MysticalLibraryBiome>().Type }; // bestiary biome
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

            NPC.direction = player.Center.X > NPC.Center.X ? 1 : -1;
            NPC.spriteDirection = -NPC.direction; // art faces right by default, so flip

            NPC.velocity.X = NPC.direction * WalkSpeed;

            Collision.StepUp(
                ref NPC.position,
                ref NPC.velocity,
                NPC.width,
                NPC.height,
                ref NPC.stepSpeed,
                ref NPC.gfxOffY
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
            return MysticalLibrarySpawns.Chance(spawnInfo, MysticalLibrary.EntranceHall, LibrarySpawnChance);
        }
    }
}