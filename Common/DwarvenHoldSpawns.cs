using System.Collections.Generic;
using System.Linq;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.NPCs.Dwarves;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Spawn rules inside the Dwarven Hold: only the dwarf grunts spawn there (no vanilla enemies),
    // each in its own zones (see each grunt's SpawnWeight), and nothing spawns in the Titan foundry.
    public class DwarvenHoldSpawns : GlobalNPC
    {
        public const float SpawnRateMultiplier = 0.5f; // lower = spawns more often
        public const int ExtraMaxSpawns = 3;

        public static int ZoneAtSpawn(NPCSpawnInfo spawnInfo)
        {
            int zone = DwarvenHold.ZoneAt(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY - 1);
            return zone != DwarvenHold.NoZone ? zone : DwarvenHold.ZoneAt(spawnInfo.SpawnTileX, spawnInfo.SpawnTileY);
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.InModBiome<DwarvenHoldBiome>())
            {
                spawnRate = (int)(spawnRate * SpawnRateMultiplier);
                maxSpawns += ExtraMaxSpawns;
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            int zone = ZoneAtSpawn(spawnInfo);
            if (zone == DwarvenHold.NoZone)
            {
                return;
            }
            if (zone == DwarvenHold.Foundry)
            {
                pool.Clear();
                return;
            }

            var holdNPCs = new HashSet<int>
            {
                ModContent.NPCType<DwarfSapper>(),
                ModContent.NPCType<DwarfGunner>(),
                ModContent.NPCType<DwarfMiner>(),
                ModContent.NPCType<DwarfMechPilot>(),
            };
            foreach (int type in pool.Keys.ToList())
            {
                if (!holdNPCs.Contains(type))
                {
                    pool.Remove(type);
                }
            }
        }
    }
}
