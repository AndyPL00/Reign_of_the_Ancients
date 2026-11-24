using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Items.Materials;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Weapons
{
    // Elven Broadsword (placeholder name). Each swing also releases a short-range pale crescent.
    public class ElvenBroadsword : ModItem
    {
        public const int BaseDamage = 32;
        public const int UseTimeTicks = 22;
        public const float BaseKnockback = 5.5f;
        public const float CrescentDamageRatio = 0.5f;
        public const float CrescentSpeed = 9f;
        public const int BarCost = 12;
        public const int EssenceCost = 3;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)(CrescentDamageRatio * 100));

        public override void SetDefaults()
        {
            Item.width = 44;
            Item.height = 44;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = BaseKnockback;
            Item.useTime = UseTimeTicks;
            Item.useAnimation = UseTimeTicks;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<ElvenCrescent>();
            Item.shootSpeed = CrescentSpeed;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1, silver: 50);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            damage = (int)(damage * CrescentDamageRatio);
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustDirect(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.IceTorch);
                d.noGravity = true;
            }
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
