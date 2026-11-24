using Reign_of_the_Ancients.Common;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Accessories
{
    // Squire's Bracer (placeholder name). Dropped by the Elf Squire; also rare Library chest loot.
    // The extra knockback against enemies targeting you lives in ElvenAccessoryPlayer.
    public class SquiresBracer : ModItem
    {
        public const int MeleeSpeedBonusPercent = 6;
        public const int DefenseBonus = 1;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            MeleeSpeedBonusPercent, DefenseBonus, (int)(ElvenAccessoryPlayer.BracerKnockbackBonus * 100));

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 24;
            Item.accessory = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 80);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeedBonusPercent / 100f;
            player.statDefense += DefenseBonus;
            player.GetModPlayer<ElvenAccessoryPlayer>().squiresBracer = true;
        }
    }
}
