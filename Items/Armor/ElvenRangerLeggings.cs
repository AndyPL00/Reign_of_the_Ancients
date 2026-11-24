using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Armor
{
    // Elven Ranger set (placeholder name), leg piece.
    [AutoloadEquip(EquipType.Legs)]
    public class ElvenRangerLeggings : ModItem
    {
        public const int ArmorDefense = 6;
        public const int RangedDamageBonusPercent = 6;
        public const int MoveSpeedBonusPercent = 10;
        public const int BarCost = 15;
        public const int CrystalCost = 4;
        public const int EssenceCost = 4;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(RangedDamageBonusPercent, MoveSpeedBonusPercent);

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = ArmorDefense;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1, silver: 50);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += RangedDamageBonusPercent / 100f;
            player.moveSpeed += MoveSpeedBonusPercent / 100f;
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
