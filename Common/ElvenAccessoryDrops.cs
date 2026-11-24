using Reign_of_the_Ancients.Items.Accessories;
using Reign_of_the_Ancients.NPCs.Elves;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Accessory drops for the library elves, kept in one place so the rates are easy to tune.
    // Elves summoned by Volodar are flagged as statue spawns, so they drop nothing.
    public class ElvenAccessoryDrops : GlobalNPC
    {
        private const int SpellbookChance = 25; // 1 in N
        private const int BracerChance = 25;
        private const int CrestChance = 20;

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == ModContent.NPCType<ElvenApprentice>())
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ApprenticesSpellbook>(), SpellbookChance));
            }
            else if (npc.type == ModContent.NPCType<ElfSquire>())
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SquiresBracer>(), BracerChance));
            }
            else if (npc.type == ModContent.NPCType<ElfHighGuard>())
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<GuardiansCrest>(), CrestChance));
            }
        }
    }
}
