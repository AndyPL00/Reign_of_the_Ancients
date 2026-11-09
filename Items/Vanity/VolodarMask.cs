using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Vanity
{
    // Vanity head piece. VolodarMask_Head.png is the worn sprite (40x1120, 20 frames).
    [AutoloadEquip(EquipType.Head)]
    public class VolodarMask : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.vanity = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 75);
        }
    }
}
