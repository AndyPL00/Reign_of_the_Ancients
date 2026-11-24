using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Materials
{
    // Elven Bar (placeholder name). Smelted from Elven Ore at a Hellforge. Crafting material only (not placeable).
    public class ElvenBar : ModItem
    {
        public const int OrePerBar = 4;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.SortingPriorityMaterials[Type] = 59;
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 24;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(silver: 75);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenOre>(OrePerBar)
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }
}
