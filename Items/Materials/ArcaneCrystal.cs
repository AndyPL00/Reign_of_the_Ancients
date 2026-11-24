using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Materials
{
    // Arcane Crystal (placeholder name). Dropped by the Elven Archmage; used for the Elven Ranger armor.
    // (Class isn't called ArcaneShard because that name is taken by a projectile.)
    public class ArcaneCrystal : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 20);
        }

        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.4f, 0.25f, 0.6f);
        }
    }
}
