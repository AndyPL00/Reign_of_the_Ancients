using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Walls.Library
{
    // Mystical Library background walls. Placeholder art reuses vanilla wall textures.
    // The wall type is also how the mod tells which library floor a tile belongs to
    // (see MysticalLibrary.FloorOfWall), so every wall here maps to a floor.
    public abstract class LibraryWallBase : ModWall
    {
        protected abstract int VanillaWallID { get; }
        protected abstract Color MapColor { get; }
        protected virtual Vector3 Glow => Vector3.Zero; // light emitted, for gemspark-style walls

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

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Glow != Vector3.Zero)
            {
                r = Glow.X;
                g = Glow.Y;
                b = Glow.Z;
            }
        }
    }

    // ----- Floor 1: entrance hall -----

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

    public class LibraryRoseWindowWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.PurpleStainedGlass;
        protected override Color MapColor => new Color(130, 70, 170);
    }

    // ----- Floor 2: forbidden archive -----

    public class ForbiddenLibraryWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.ObsidianBrick;
        protected override Color MapColor => new Color(35, 25, 50);
    }

    // Glowing purple backdrop of the sealed vault.
    public class VaultWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.AmethystGemspark;
        protected override Color MapColor => new Color(150, 70, 200);
        protected override Vector3 Glow => new Vector3(0.25f, 0.08f, 0.35f);
    }

    // Iron bars framing the sealed vault.
    public class VaultFenceWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.IronFence;
        protected override Color MapColor => new Color(80, 80, 90);
    }

    // ----- Floor 3: Volodar's throne room -----

    public class ThroneRoomWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.GraniteBlock;
        protected override Color MapColor => new Color(30, 32, 55);
    }

    // Glowing midnight-blue arch behind the throne.
    public class ThroneBackdropWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.SapphireGemspark;
        protected override Color MapColor => new Color(60, 90, 200);
        protected override Vector3 Glow => new Vector3(0.08f, 0.14f, 0.35f);
    }
}
