using Reign_of_the_Ancients.Items.Materials;
using Reign_of_the_Ancients.NPCs.Elves;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Elven Essence drops for every library elf, kept in one place so the rates are easy to tune.
    // Elves summoned by Volodar are flagged as statue spawns, so they drop nothing.
    public class ElvenEssenceDrops : GlobalNPC
    {
        // 1 in N chance, min-max amount
        private const int LowerElfChance = 3;   // Apprentice, Squire
        private const int UpperElfChance = 2;   // High-Guard, Archmage
        private const int MinAmount = 1;
        private const int MaxAmount = 2;

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            int chance = 0;
            if (npc.type == ModContent.NPCType<ElvenApprentice>() || npc.type == ModContent.NPCType<ElfSquire>())
            {
                chance = LowerElfChance;
            }
            else if (npc.type == ModContent.NPCType<ElfHighGuard>() || npc.type == ModContent.NPCType<ElvenArchmage>())
            {
                chance = UpperElfChance;
            }

            if (chance > 0)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ElvenEssence>(), chance, MinAmount, MaxAmount));
            }
        }
    }
}
