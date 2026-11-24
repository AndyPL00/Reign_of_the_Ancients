using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Tools
{
    // Elven Hammer (placeholder name). Same hammer power as the Molten Hamaxe.
    public class ElvenHammer : ModItem
    {
        public const int HammerPower = 70;
        public const int BaseDamage = 26;
        public const int UseTimeTicks = 20;
        public const int UseAnimationTicks = 30;
        public const float BaseKnockback = 6.5f;
        public const float HammerScale = 1.2f;
        public const int BarCost = 12;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = BaseKnockback;
            Item.scale = HammerScale;
            Item.useTime = UseTimeTicks;
            Item.useAnimation = UseAnimationTicks;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.hammer = HammerPower;
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
