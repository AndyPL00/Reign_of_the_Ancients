using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Tools
{
    // Elven Pickaxe (placeholder name). Same pickaxe power as the Molten Pickaxe.
    public class ElvenPickaxe : ModItem
    {
        public const int PickPower = 100;
        public const int BaseDamage = 12;
        public const int UseTimeTicks = 14;
        public const int UseAnimationTicks = 20;
        public const float BaseKnockback = 2f;
        public const int BarCost = 15;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = BaseKnockback;
            Item.useTime = UseTimeTicks;
            Item.useAnimation = UseAnimationTicks;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.pick = PickPower;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1, silver: 20);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenBar>(BarCost)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
