using Reign_of_the_Ancients.Common;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Accessories
{
    // Guardian's Crest (placeholder name). Dropped by the Elf High-Guard; also rare Library chest loot.
    // After a while without being hit, the next hit is reduced (logic in ElvenAccessoryPlayer).
    public class GuardiansCrest : ModItem
    {
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            ElvenAccessoryPlayer.CrestChargeTicks / 60, (int)(ElvenAccessoryPlayer.CrestDamageReduction * 100));

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<ElvenAccessoryPlayer>().guardiansCrest = true;
        }
    }
}
