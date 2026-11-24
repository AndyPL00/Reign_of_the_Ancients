using Reign_of_the_Ancients.Common;
using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Armor
{
    // Elven Ranger set (placeholder name), chest piece.
    [AutoloadEquip(EquipType.Body)]
    public class ElvenRangerTunic : ModItem
    {
        public const int ArmorDefense = 7;
        public const int RangedCritBonusPercent = 8;
        public const int AmmoSaveChancePercent = 15;
        public const int BarCost = 20;
        public const int CrystalCost = 4;
        public const int EssenceCost = 5;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(RangedCritBonusPercent, AmmoSaveChancePercent);

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
            player.GetCritChance(DamageClass.Ranged) += RangedCritBonusPercent;
            player.GetModPlayer<ElvenRangerPlayer>().ammoSaveChance += AmmoSaveChancePercent / 100f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenBar>(BarCost)
                .AddIngredient<ArcaneCrystal>(CrystalCost)
                .AddIngredient<ElvenEssence>(EssenceCost)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
