using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Weapons
{
    // Volodar's signature bow (dropped by Volodar). Fires the arrows you have; every few shots it looses
    // an arcane arrow instead, which splits into homing shards.
    public class VolodarsBow : ModItem
    {
        public const int BaseDamage = 30;
        public const int UseTimeTicks = 20;
        public const float ShotSpeed = 10f;
        public const int ArcaneEvery = 3;              // every Nth shot is an arcane arrow
        public const float ArcaneDamageMultiplier = 1.25f;
        public const float ArcaneSpeedMultiplier = 1.2f;

        private int shotCount;

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 40;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 2.5f;
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
            Item.value = Item.sellPrice(gold: 3);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            shotCount++;
            if (shotCount % ArcaneEvery != 0)
            {
                return true; // normal arrow
            }

            SoundEngine.PlaySound(SoundID.Item9, position);
            Projectile.NewProjectile(source, position, velocity * ArcaneSpeedMultiplier, ModContent.ProjectileType<ArcaneArrow>(),
                (int)(damage * ArcaneDamageMultiplier), knockback, player.whoAmI);
            return false;
        }

        // Hold the bow a little in front of the hand.
        public override Vector2? HoldoutOffset() => new Vector2(-2f, 0f);
    }
}
