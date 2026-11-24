using Reign_of_the_Ancients.Tiles.Banners;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Placeable.Banners
{
    public class ElfHighGuardBanner : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<ElvenBanner>(), (int)ElvenBanner.StyleID.ElfHighGuard);
            Item.width = 10;
            Item.height = 24;
            Item.SetShopValues(ItemRarityColor.Blue1, Item.buyPrice(silver: 10));
        }
    }
}
