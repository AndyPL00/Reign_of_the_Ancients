using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.Banners
{
    // Enemy banners for the Dwarven Hold grunts (same setup as ElvenBanner).
    // Texture: one 18x54 column per style, in StyleID order.
    public class DwarvenBanner : ModBannerTile
    {
        public enum StyleID
        {
            DwarfSapper,
            DwarfGunner,
            DwarfMiner,
            DwarfMechPilot,
        }
    }
}
