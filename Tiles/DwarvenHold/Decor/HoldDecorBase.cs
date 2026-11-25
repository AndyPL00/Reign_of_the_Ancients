using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Reign_of_the_Ancients.Tiles.DwarvenHold.Decor
{
    // Steampunk set dressing for the Dwarven Hold (placeholder art). Non-solid multi-tile objects that you walk past,
    // placed only by world gen. They need no support (they hang in the air or stand on platforms), can't be broken
    // before Skeletron like the rest of the hold, and drop nothing.
    public abstract class HoldDecorBase : ModTile
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        protected virtual Color MapColor => new Color(150, 105, 55);
        protected virtual int DecorDust => DustID.Copper;

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLavaDeath[Type] = false;
            Main.tileNoAttach[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DecorDust;
            HitSound = SoundID.Tink;
            MinPick = HoldBlockBase.RequiredPickPower;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.Width = Width;
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.Origin = Point16.Zero;
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, Height).ToArray();
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.AnchorBottom = AnchorData.Empty;
            TileObjectData.addTile(Type);

            AddMapEntry(MapColor);
            SetDecorDefaults();
        }

        protected virtual void SetDecorDefaults()
        {
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => NPC.downedBoss3;

        public override bool CanExplode(int i, int j) => false;

        // World gen: writes the object's tiles directly (top-left corner at left, top), replacing whatever is there.
        public static void Place(int left, int top, int type, int width, int height, int style = 0)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    Tile tile = Main.tile[left + dx, top + dy];
                    tile.HasTile = true;
                    tile.TileType = (ushort)type;
                    tile.TileFrameX = (short)((style * width + dx) * 18);
                    tile.TileFrameY = (short)(dy * 18);
                    tile.Slope = SlopeType.Solid;
                    tile.IsHalfBlock = false;
                    tile.TileColor = 0;
                    tile.LiquidAmount = 0;
                }
            }
        }

        // Which cell of the object (i, j) is, counted from its top-left corner.
        protected Point CellOf(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            return new Point(tile.TileFrameX / 18 % Width, tile.TileFrameY / 18 % Height);
        }

        protected int StyleOf(int i, int j) => Main.tile[i, j].TileFrameX / 18 / Width;
    }
}
