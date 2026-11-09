using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.DamageClasses;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Weapons
{
    // Heavy greatsword dropped by the Elf High-Guard.
    // Left click: weighty held swing (windup -> fast arc -> follow-through), alternating direction.
    // Right click: same swing, costs mana and releases a magic slash that scales with melee + magic damage.
    public class ElvenGreatsword : ModItem
    {
        public const int BaseDamage = 42;
        public const float SwordScale = 1.25f;     // visual size and hitbox reach of the swing
        public const int UseTimeTicks = 40;          // also the right-click cooldown (same item use time)
        public const float BaseKnockback = 7.5f;
        public const int ManaCost = 15;
        public const float SlashDamageMultiplier = 0.8f; // slash base damage = sword damage * this, then melee+magic bonuses
        public const float SlashKnockbackMultiplier = 0.5f;
        public const float SlashSpeed = 13f;

        private int swingDirection = -1;

        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 64;
            Item.damage = BaseDamage;
            Item.scale = SwordScale;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = BaseKnockback;
            Item.useTime = UseTimeTicks;
            Item.useAnimation = UseTimeTicks;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;        // damage comes from the swing projectile
            Item.noUseGraphic = true;   // the swing projectile draws the sword
            Item.autoReuse = true;
            Item.UseSound = null;       // the swing projectile plays its own sounds on the beat
            Item.shoot = ModContent.ProjectileType<ElvenGreatswordSwing>();
            Item.shootSpeed = 1f;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 2);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // Mana is only charged for the right-click magic swing.
            Item.mana = player.altFunctionUse == 2 ? ManaCost : 0;
            return true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool magicSwing = player.altFunctionUse == 2;
            swingDirection = -swingDirection;

            Vector2 aim = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            int index = Projectile.NewProjectile(source, player.MountedCenter, aim, type, damage, knockback,
                player.whoAmI, swingDirection, magicSwing ? 1f : 0f, player.itemAnimationMax);

            if (magicSwing && Main.projectile[index].ModProjectile is ElvenGreatswordSwing swing)
            {
                // Only the owner spawns the slash, so these values only need to exist on this client.
                DamageClass hybrid = ModContent.GetInstance<MeleeMagicDamageClass>();
                swing.SlashDamage = (int)player.GetTotalDamage(hybrid).ApplyTo(Item.damage * SlashDamageMultiplier);
                swing.SlashCrit = (int)player.GetTotalCritChance(hybrid) + Item.crit;
                swing.SlashKnockback = knockback * SlashKnockbackMultiplier;
            }

            return false;
        }
    }
}
