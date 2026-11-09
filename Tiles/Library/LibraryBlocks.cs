using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.Library
{
    // Mystical Library blocks. Most are placeholders that reuse a vanilla block texture
    // (VanillaTileID). Leave VanillaTileID at -1 to use a PNG next to the class instead.
    public abstract class LibraryBlockBase : ModTile
    {
        public const int RequiredPickPower = 130; // Palladium pickaxe

        protected virtual int VanillaTileID => -1;
        protected abstract Color MapColor { get; }
        protected virtual int BlockDust => DustID.Stone;

        public override string Texture => VanillaTileID >= 0 ? "Terraria/Images/Tiles_" + VanillaTileID : base.Texture;

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            MinPick = RequiredPickPower;
            DustType = BlockDust;
            HitSound = SoundID.Tink;
            AddMapEntry(MapColor);
        }

        public override bool CanExplode(int i, int j) => false;
    }

    // Entrance hall: white marble.
    public class LibraryBrick : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.MarbleBlock;
        protected override Color MapColor => new Color(220, 215, 200);
        protected override int BlockDust => DustID.Marble;
    }

    // Entrance hall trim: gold.
    public class LibraryTrim : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.GoldBrick;
        protected override Color MapColor => new Color(220, 180, 60);
        protected override int BlockDust => DustID.GoldCoin;
    }

    // Weathered patches on the cathedral's outside: rough natural marble.
    public class LibraryWeatheredStone : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.Marble;
        protected override Color MapColor => new Color(200, 195, 185);
        protected override int BlockDust => DustID.Marble;
    }

    // Forbidden archive: obsidian brick.
    public class ForbiddenBrick : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.ObsidianBrick;
        protected override Color MapColor => new Color(50, 35, 70);
        protected override int BlockDust => DustID.Obsidian;
    }

    // Forbidden archive trim: demonite brick.
    public class ForbiddenTrim : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.DemoniteBrick;
        protected override Color MapColor => new Color(90, 60, 120);
        protected override int BlockDust => DustID.Demonite;
    }

    // Volodar's throne room: granite (midnight blue).
    public class ThroneBrick : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.GraniteBlock;
        protected override Color MapColor => new Color(40, 45, 75);
        protected override int BlockDust => DustID.Granite;
    }

    // Throne room trim: silver.
    public class ThroneTrim : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.SilverBrick;
        protected override Color MapColor => new Color(190, 195, 210);
        protected override int BlockDust => DustID.Silver;
    }
}
