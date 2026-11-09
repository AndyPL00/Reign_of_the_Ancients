using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.Library
{
    // Mystical Library blocks. Placeholder art: each one reuses a vanilla block texture.
    // Swap in custom sprites by deleting the Texture override and adding a PNG next to this file.
    public abstract class LibraryBlockBase : ModTile
    {
        public const int RequiredPickPower = 130; // Palladium pickaxe

        protected abstract int VanillaTileID { get; }
        protected abstract Color MapColor { get; }
        protected virtual int BlockDust => DustID.Stone;

        public override string Texture => "Terraria/Images/Tiles_" + VanillaTileID;

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

    // Forbidden archive: obsidian brick.
    public class ForbiddenBrick : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.ObsidianBrick;
        protected override Color MapColor => new Color(50, 35, 70);
        protected override int BlockDust => DustID.Obsidian;
    }

    // Volodar's arena: granite.
    public class ArenaBrick : LibraryBlockBase
    {
        protected override int VanillaTileID => TileID.GraniteBlock;
        protected override Color MapColor => new Color(40, 45, 75);
        protected override int BlockDust => DustID.Granite;
    }
}
