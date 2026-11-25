using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.DwarvenHold.Decor
{
    // Wooden cargo crane: lattice tower, winch cab, jib and pulley (14x16 tiles of art). Like the gears, the tile is
    // only a 2x2 anchor at the foot of the tower and the whole crane is drawn from it, so the tiles under the jib stay
    // free for the rope (vanilla rope tiles), the hanging load and its platform. Lit tile by tile.
    // Style 0: jib reaches right. Style 1: jib reaches left.
    public class HoldCrane : HoldDecorBase
    {
        public const int ArtWidth = 14, ArtHeight = 16;
        public const int JibRight = 0, JibLeft = 1;

        // Where the anchor's top-left tile sits inside the art, per style (the tower's centre is between its two columns).
        private static readonly Point[] AnchorInArt = { new Point(3, ArtHeight - 2), new Point(ArtWidth - 5, ArtHeight - 2) };

        // Rope column relative to the anchor's top-left tile, per style (under the pulley at the jib tip).
        public static int RopeOffsetX(int style) => style == JibRight ? ArtWidth - 1 - AnchorInArt[0].X : -AnchorInArt[1].X;

        // Row of the first rope tile below the art's pulley, relative to the anchor's top row.
        public static int RopeStartOffsetY => 3 - AnchorInArt[0].Y;

        public override int Width => 2;
        public override int Height => 2;
        protected override Color MapColor => new Color(94, 64, 40);
        protected override int DecorDust => Terraria.ID.DustID.WoodFurniture;

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (CellOf(i, j) != Point.Zero)
            {
                return false;
            }
            int style = Utils.Clamp(StyleOf(i, j), JibRight, JibLeft);
            Texture2D texture = Terraria.GameContent.TextureAssets.Tile[Type].Value;
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            int artLeft = i - AnchorInArt[style].X, artTop = j - AnchorInArt[style].Y;
            for (int cx = 0; cx < ArtWidth; cx++)
            {
                for (int cy = 0; cy < ArtHeight; cy++)
                {
                    int x = artLeft + cx, y = artTop + cy;
                    if (!WorldGen.InWorld(x, y))
                    {
                        continue;
                    }
                    Rectangle source = new Rectangle(style * ArtWidth * 16 + cx * 16, cy * 16, 16, 16);
                    spriteBatch.Draw(texture, new Vector2(x * 16, y * 16) - Main.screenPosition + zero, source, Lighting.GetColor(x, y));
                }
            }
            return false;
        }
    }

    // What hangs from the crane's rope: crates, a barrel and a sack in a rope bridle (5x3). It stands on a row of
    // ordinary platforms placed under it, so players can stand on it.
    public class HoldCraneLoad : HoldDecorBase
    {
        public override int Width => 5;
        public override int Height => 3;
        protected override Color MapColor => new Color(138, 94, 56);
        protected override int DecorDust => Terraria.ID.DustID.WoodFurniture;
    }
}
