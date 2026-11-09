using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Walls.Library
{
    // Mystical Library background walls. Placeholder art reuses vanilla wall textures.
    // The wall type is also how the mod tells which library floor a tile belongs to (spawns, biome).
    public abstract class LibraryWallBase : ModWall
    {
        protected abstract int VanillaWallID { get; }
        protected abstract Color MapColor { get; }

        public override string Texture => "Terraria/Images/Wall_" + VanillaWallID;

        public override void SetStaticDefaults()
        {
            Main.wallHouse[Type] = false; // not a "safe" house wall, so enemies can spawn in front of it
            DustType = DustID.Stone;
            AddMapEntry(MapColor);
        }

        // Matches the blocks: protected until Hardmode.
        public override void KillWall(int i, int j, ref bool fail)
        {
            if (!Main.hardMode)
            {
                fail = true;
            }
        }

        public override bool CanExplode(int i, int j) => false;
    }

    public class LibraryWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.MarbleBlock;
        protected override Color MapColor => new Color(150, 145, 135);
    }

    public class LibraryWindowWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.BlueStainedGlass;
        protected override Color MapColor => new Color(70, 110, 180);
    }

    public class ForbiddenLibraryWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.ObsidianBrick;
        protected override Color MapColor => new Color(35, 25, 50);
    }

    public class LibraryArenaWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.GraniteBlock;
        protected override Color MapColor => new Color(30, 32, 55);
    }
}
