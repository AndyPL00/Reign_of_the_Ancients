using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Tools
{
    // Elven Waraxe (placeholder name).
    public class ElvenWaraxe : ModItem
    {
        public const int AxePower = 28; // shown in-game as 140% (x5)
        public const int BaseDamage = 22;
        public const int UseTimeTicks = 20;
        public const int UseAnimationTicks = 27;
        public const float BaseKnockback = 5f;
        public const int BarCost = 12;

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
            Item.axe = AxePower;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1);
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
