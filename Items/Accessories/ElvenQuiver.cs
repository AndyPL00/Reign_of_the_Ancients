using Reign_of_the_Ancients.Common;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Accessories
{
    // Elven Quiver (placeholder name). Library chest loot (archive floor).
    // Spectral arrow logic lives in ElvenRangerPlayer, next to the Ranger set's Arcane Volley; the two stack.
    public class ElvenQuiver : ModItem
    {
        public const int RangedDamageBonusPercent = 10;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            RangedDamageBonusPercent, ElvenRangerPlayer.QuiverShotsPerArrow, (int)(ElvenRangerPlayer.QuiverDamageRatio * 100));

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 1, silver: 50);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetDamage(DamageClass.Ranged) += RangedDamageBonusPercent / 100f;
            player.GetModPlayer<ElvenRangerPlayer>().elvenQuiver = true;
        }
    }
}
