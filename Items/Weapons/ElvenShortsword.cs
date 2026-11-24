using Reign_of_the_Ancients.Items.Materials;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Weapons
{
    // Elven Shortsword (placeholder name). Fast stabs; every few hits it also fires a piercing light-dash.
    public class ElvenShortsword : ModItem
    {
        public const int BaseDamage = 24;
        public const int UseTimeTicks = 9;
        public const float BaseKnockback = 3f;
        public const int HitsPerDash = 4;
        public const float DashDamageRatio = 0.6f;
        public const float DashSpeed = 7f;   // per update; the dash has 2 extra updates
        public const int BarCost = 8;
        public const int EssenceCost = 2;

        // Counts stabs that hit something; lives on the item, so each copy keeps its own count.
        public int hitCounter;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(HitsPerDash);

        public override void SetDefaults()
        {
            Item.damage = BaseDamage;
            Item.knockBack = BaseKnockback;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.useAnimation = UseTimeTicks;
            Item.useTime = UseTimeTicks;
            Item.width = 32;
            Item.height = 32;
            Item.UseSound = SoundID.Item1;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<ElvenShortswordStab>();
            Item.shootSpeed = 2.1f;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1);
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
