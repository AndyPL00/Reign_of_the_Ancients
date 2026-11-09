using System.Collections.Generic;
using System.Linq;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.NPCs.Elves;
using Reign_of_the_Ancients.Walls.Library;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Spawn rules inside the Mystical Library: only elves spawn there (no vanilla enemies),
    // each elf on its own floor, and nothing spawns in the arena.
    public class MysticalLibrarySpawns : GlobalNPC
    {
        public const float SpawnRateMultiplier = 0.5f; // lower = spawns more often
        public const int ExtraMaxSpawns = 3;

        // Used by each elf's SpawnChance.
        public static float Chance(NPCSpawnInfo spawnInfo, int floor, float chance)
        {
            return FloorAtSpawn(spawnInfo) == floor ? chance : 0f;
        }

        public static int FloorAtSpawn(NPCSpawnInfo spawnInfo)
        {
            int floor = MysticalLibrary.FloorAt(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1);
            return floor != MysticalLibrary.NoFloor ? floor : MysticalLibrary.FloorAt(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY);
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.InModBiome<MysticalLibraryBiome>())
            {
                spawnRate = (int)(spawnRate * SpawnRateMultiplier);
                maxSpawns += ExtraMaxSpawns;
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            // Sealed spaces (roof attic, belfries): nothing spawns.
            int atticWall = ModContent.WallType<LibraryAtticWall>();
            if (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1].WallType == atticWall ||
                Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == atticWall)
            {
                pool.Clear();
                return;
            }

            if (FloorAtSpawn(spawnInfo) == MysticalLibrary.NoFloor)
            {
                return;
            }

            var libraryNPCs = new HashSet<int>
            {
                ModContent.NPCType<ElvenApprentice>(),
                ModContent.NPCType<ElfSquire>(),
                ModContent.NPCType<ElfHighGuard>(),
                ModContent.NPCType<ElvenArchmage>(),
            };

            foreach (int type in pool.Keys.ToList())
            {
                if (!libraryNPCs.Contains(type))
                {
                    pool.Remove(type);
                }
            }
        }
    }
}
