using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Buffs;
using Reign_of_the_Ancients.Projectiles.Pets;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Pets
{
    // Volodar's Master Mode pet (placeholder name): a small sealed tome that floats beside you.
    public class TinySealedTome : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToVanitypet(ModContent.ProjectileType<TinySealedTomePet>(), ModContent.BuffType<TinySealedTomeBuff>());
            Item.width = 22;
            Item.height = 26;
            Item.rare = ItemRarityID.Master;
            Item.master = true;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            player.AddBuff(Item.buffType, 2); // the buff spawns the pet
            return false;
        }
    }
}
