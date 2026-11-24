using Reign_of_the_Ancients.Buffs;
using Reign_of_the_Ancients.Items.Materials;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Weapons
{
    // Elven Whip (placeholder name). Tags enemies with an arcane mark (see ElvenWhipTag).
    public class ElvenWhip : ModItem
    {
        public const int BaseDamage = 26;
        public const float BaseKnockback = 2f;
        public const float ShootSpeed = 4f;
        public const int UseTimeTicks = 30;
        public const int BarCost = 10;
        public const int EssenceCost = 3;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ElvenWhipTag.TagDamage, ElvenWhipTag.ShardChance);

        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<ElvenWhipProjectile>(), BaseDamage, BaseKnockback, ShootSpeed, UseTimeTicks);
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1, silver: 50);
        }

        public override bool MeleePrefix() => true;

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
