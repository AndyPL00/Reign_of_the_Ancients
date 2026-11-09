using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Materials
{
    // Crafting material dropped by the Mystical Library's elves. Used for the Sealed Tome.
    public class ElvenEssence : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.ItemNoGravity[Type] = true; // floats when dropped
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 5);
        }

        public override void PostUpdate()
        {
            Lighting.AddLight(Item.Center, 0.2f, 0.45f, 0.4f);
        }
    }
}
