using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Accessories
{
    // Apprentice's Spellbook (placeholder name). Dropped by the Elven Apprentice; also rare Library chest loot.
    public class ApprenticesSpellbook : ModItem
    {
        public const int MaxManaBonus = 20;
        public const int MagicDamageBonusPercent = 4;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxManaBonus, MagicDamageBonusPercent);

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 80);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.statManaMax2 += MaxManaBonus;
            player.GetDamage(DamageClass.Magic) += MagicDamageBonusPercent / 100f;
        }
    }
}
