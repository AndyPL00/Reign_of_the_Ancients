using Microsoft.Xna.Framework;
using Terraria.ID;

namespace Reign_of_the_Ancients.Tiles.Library
{
    // Red carpet with gold edges for the entrance hall floor.
    // Every frame on the sheet is the same pattern, so it tiles seamlessly in a row.
    public class LibraryCarpet : LibraryBlockBase
    {
        protected override Color MapColor => new Color(155, 27, 48);
        protected override int BlockDust => DustID.Clentaminator_Red;
    }
}
