using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.Banners
{
    // Enemy banners for the library elves. ModBannerTile handles placement and the nearby-banner buff;
    // each NPC links to its banner via Banner/BannerItem in SetDefaults.
    // Texture: one 18x54 column per style, in StyleID order.
    public class ElvenBanner : ModBannerTile
    {
        public enum StyleID
        {
            ElvenApprentice,
            ElfSquire,
            ElfHighGuard,
            ElvenArchmage,
        }
    }
}
