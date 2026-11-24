using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Armor
{
    // Spellblade set (placeholder name), chest piece.
    [AutoloadEquip(EquipType.Body)]
    public class SpellbladeBreastplate : ModItem
    {
        public const int ArmorDefense = 9;
        public const int CritBonusPercent = 7;  // melee and magic
        public const int BarCost = 20;
        public const int EssenceCost = 5;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritBonusPercent);

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 20;
            Item.defense = ArmorDefense;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 2);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritBonusPercent;
            player.GetCritChance(DamageClass.Magic) += CritBonusPercent;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenBar>(BarCost)
                .AddIngredient<ElvenEssence>(EssenceCost)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
