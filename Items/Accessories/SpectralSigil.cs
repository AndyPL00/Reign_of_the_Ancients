using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Accessories
{
    // Expert-mode accessory from Volodar's treasure bag. When you fire a ranged weapon, a spectral
    // archer appears behind you and shoots an arcane arrow at the nearest enemy (with a cooldown).
    public class SpectralSigil : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.expert = true;
            Item.rare = ItemRarityID.Expert;
            Item.value = Item.sellPrice(gold: 2);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<SpectralSigilPlayer>().equipped = true;
        }
    }

    public class SpectralSigilPlayer : ModPlayer
    {
        public const int CooldownTicks = 75;
        public const float DamageRatio = 0.6f;       // of the shot that triggered it
        private static readonly Vector2 SpawnOffset = new Vector2(-40f, -30f); // behind and above (flipped by direction)

        public bool equipped;
        private int cooldown;

        public override void ResetEffects()
        {
            equipped = false;
        }

        public override void PostUpdate()
        {
            if (cooldown > 0)
            {
                cooldown--;
            }
        }

        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (equipped && cooldown <= 0 && item.CountsAsClass(DamageClass.Ranged))
            {
                cooldown = CooldownTicks;
                Vector2 spawn = Player.Center + new Vector2(SpawnOffset.X * Player.direction, SpawnOffset.Y);
                Projectile.NewProjectile(source, spawn, Vector2.Zero, ModContent.ProjectileType<SpectralArcherAlly>(),
                    (int)(damage * DamageRatio), knockback, Player.whoAmI);
            }
            return true;
        }
    }
}
