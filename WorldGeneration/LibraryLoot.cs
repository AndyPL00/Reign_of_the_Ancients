using Reign_of_the_Ancients.Items.Accessories;
using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.WorldGeneration
{
    // What goes into the Mystical Library's chests. Floor 1 = cathedral, floor 2 = forbidden archive.
    // Runs during world gen (server / single player only).
    public static class LibraryLoot
    {
        // Signature item per floor: Archivist's Lantern (floor 1), Elven Quiver (floor 2).
        private const float SignatureChance = 0.33f;
        // One of the elf-drop accessories (Spellbook, Bracer, Crest).
        private const float ElfAccessoryChance = 0.15f;

        private const int EssenceMin = 3, EssenceMax = 6;
        private const int PotionTypesMin = 2, PotionTypesMax = 3;
        private const int PotionStackMin = 1, PotionStackMax = 2;
        private const int BombMin = 3, BombMax = 6;
        private const int ArrowMin = 25, ArrowMax = 50;
        private const int Floor1GoldMin = 1, Floor1GoldMax = 2;
        private const int Floor2GoldMin = 2, Floor2GoldMax = 4;

        private static readonly int[] Potions =
        {
            ItemID.ManaRegenerationPotion, ItemID.MagicPowerPotion, ItemID.ArcheryPotion,
            ItemID.RegenerationPotion, ItemID.IronskinPotion,
        };

        public static void Fill(Chest chest, int floor)
        {
            int slot = 0;
            void Add(int type, int stack = 1)
            {
                if (slot < Chest.maxItems)
                {
                    chest.item[slot].SetDefaults(type);
                    chest.item[slot].stack = stack;
                    slot++;
                }
            }
            int Roll(int min, int max) => WorldGen.genRand.Next(min, max + 1);

            if (WorldGen.genRand.NextFloat() < SignatureChance)
            {
                Add(floor == 1 ? ModContent.ItemType<ArchivistsLantern>() : ModContent.ItemType<ElvenQuiver>());
            }
            if (WorldGen.genRand.NextFloat() < ElfAccessoryChance)
            {
                int[] accessories =
                {
                    ModContent.ItemType<ApprenticesSpellbook>(), ModContent.ItemType<SquiresBracer>(), ModContent.ItemType<GuardiansCrest>(),
                };
                Add(WorldGen.genRand.Next(accessories));
            }

            Add(ModContent.ItemType<ElvenEssence>(), Roll(EssenceMin, EssenceMax));

            // A few different potions, no duplicates.
            int potionCount = Roll(PotionTypesMin, PotionTypesMax);
            int start = WorldGen.genRand.Next(Potions.Length);
            for (int i = 0; i < potionCount; i++)
            {
                Add(Potions[(start + i) % Potions.Length], Roll(PotionStackMin, PotionStackMax));
            }

            Add(ItemID.Bomb, Roll(BombMin, BombMax));
            int arrow = floor == 1 ? ItemID.FlamingArrow : (WorldGen.genRand.NextBool() ? ItemID.JestersArrow : ItemID.UnholyArrow);
            Add(arrow, Roll(ArrowMin, ArrowMax));
            Add(ItemID.GoldCoin, floor == 1 ? Roll(Floor1GoldMin, Floor1GoldMax) : Roll(Floor2GoldMin, Floor2GoldMax));
        }
    }
}
