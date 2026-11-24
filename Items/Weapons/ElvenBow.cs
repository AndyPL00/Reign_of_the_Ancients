using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Items.Materials;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Weapons
{
    // Elven Bow (placeholder name). Wooden arrows become fast, piercing streaks of light; other arrows fire normally.
    public class ElvenBow : ModItem
    {
        public const int BaseDamage = 26;
        public const int UseTimeTicks = 20;
        public const float ShotSpeed = 9f;
        public const float StreakSpeedRatio = 0.6f; // the streak has 2 extra updates, so it still ends up ~1.8x faster
        public const int BarCost = 10;
        public const int EssenceCost = 3;
        public const int CrystalCost = 2;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 40;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 2f;
            Item.useTime = UseTimeTicks;
            Item.useAnimation = UseTimeTicks;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item5;
            Item.shoot = ProjectileID.WoodenArrowFriendly;
            Item.useAmmo = AmmoID.Arrow;
            Item.shootSpeed = ShotSpeed;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1, silver: 50);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.WoodenArrowFriendly)
            {
                type = ModContent.ProjectileType<ElvenLightStreak>();
                velocity *= StreakSpeedRatio;
            }
        }

        public override Vector2? HoldoutOffset() => new Vector2(-2f, 0f);

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenBar>(BarCost)
                .AddIngredient<ElvenEssence>(EssenceCost)
                .AddIngredient<ArcaneCrystal>(CrystalCost)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
