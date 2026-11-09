using Reign_of_the_Ancients.Items.Accessories;
using Reign_of_the_Ancients.Items.Weapons;
using Reign_of_the_Ancients.NPCs.Bosses;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Bosses
{
    // Volodar's Expert/Master treasure bag: the bow or the grimoire, the Spectral Sigil, and coins.
    public class VolodarBag : ModItem
    {
        private const int MaskChance = 7; // 1 in N

        public override void SetStaticDefaults()
        {
            ItemID.Sets.BossBag[Type] = true;
            ItemID.Sets.PreHardmodeLikeBossBag[Type] = true;
            Item.ResearchUnlockCount = 3;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ItemRarityID.Expert;
            Item.expert = true;
        }

        public override bool CanRightClick() => true;

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
            itemLoot.Add(ItemDropRule.OneFromOptions(1, ModContent.ItemType<VolodarsBow>(), ModContent.ItemType<VolodarsGrimoire>()));
            itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpectralSigil>()));
            itemLoot.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<Vanity.VolodarMask>(), MaskChance));
            itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Volodar>()));
        }
    }
}
