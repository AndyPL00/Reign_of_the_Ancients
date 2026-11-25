using System.Collections.Generic;
using Reign_of_the_Ancients.Items.Bosses;
using Reign_of_the_Ancients.Items.Pets;
using Reign_of_the_Ancients.Items.Placeable;
using Reign_of_the_Ancients.Items.Vanity;
using Reign_of_the_Ancients.NPCs.Bosses;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Optional support for other mods. Everything here is skipped if the other mod isn't installed.
    public class ModIntegrations : ModSystem
    {
        // Boss Checklist progression: Eye of Cthulhu 2, Evil bosses 3, Queen Bee 4, Skeletron 5.
        private const float VolodarProgression = 3.5f;
        private const float TitanProgression = 5.5f;  // after Skeletron, before the Wall of Flesh (6)

        public override void PostSetupContent()
        {
            if (!ModLoader.TryGetMod("BossChecklist", out Mod bossChecklist))
            {
                return;
            }

            bossChecklist.Call(
                "LogBoss",
                Mod,
                nameof(Volodar),
                VolodarProgression,
                () => DownedBossSystem.downedVolodar,
                ModContent.NPCType<Volodar>(),
                new Dictionary<string, object>
                {
                    ["spawnItems"] = ModContent.ItemType<SealedTome>(),
                    ["collectibles"] = new List<int>
                    {
                        ModContent.ItemType<VolodarRelic>(),
                        ModContent.ItemType<VolodarTrophy>(),
                        ModContent.ItemType<VolodarMask>(),
                        ModContent.ItemType<TinySealedTome>(),
                    },
                    ["spawnInfo"] = Language.GetText("Mods.Reign_of_the_Ancients.BossChecklist.Volodar.SpawnInfo"),
                });

            // Collectibles get added here once the Titan's loot exists.
            bossChecklist.Call(
                "LogBoss",
                Mod,
                nameof(DwarvenTitan),
                TitanProgression,
                () => DownedBossSystem.downedTitan,
                new List<int> { ModContent.NPCType<DwarvenTitan>(), ModContent.NPCType<DwarvenTitanCannon>() },
                new Dictionary<string, object>
                {
                    ["spawnInfo"] = Language.GetText("Mods.Reign_of_the_Ancients.BossChecklist.DwarvenTitan.SpawnInfo"),
                });
        }
    }
}
