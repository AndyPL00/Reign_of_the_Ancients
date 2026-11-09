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

    // Gold backing behind the hall's marble pillars (base, capital and shaft).
    public class HallPillarWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.GoldBrick;
        protected override Color MapColor => new Color(190, 150, 60);
    }

    // Wooden backing inside the roof attic and tower belfries. Not part of any floor, and
    // MysticalLibrarySpawns blocks all spawns in front of it (those spaces are sealed off).
    public class LibraryAtticWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.Wood;
        protected override Color MapColor => new Color(110, 80, 55);
    }

    // ----- Floor 2: forbidden archive -----

    // Demonite backing behind the archive's pillars, also used for wall bands.
    public class ArchivePillarWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.DemoniteBrick;
        protected override Color MapColor => new Color(70, 50, 100);
    }

    // Dark wood inside the arched alcoves that hold the shelves.
    public class ArchiveNicheWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.Shadewood;
        protected override Color MapColor => new Color(45, 35, 55);
    }

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

    // Silver backing behind the throne room's pillars.
    public class ThronePillarWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.SilverBrick;
        protected override Color MapColor => new Color(160, 165, 180);
    }

    // Single glowing "stars" scattered across the vaulted ceiling.
    public class ThroneStarWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.DiamondGemspark;
        protected override Color MapColor => new Color(170, 200, 255);
        protected override Vector3 Glow => new Vector3(0.12f, 0.18f, 0.4f);
    }

    // Glowing midnight-blue arch behind the throne.
    public class ThroneBackdropWall : LibraryWallBase
    {
        protected override int VanillaWallID => WallID.SapphireGemspark;
        protected override Color MapColor => new Color(60, 90, 200);
        protected override Vector3 Glow => new Vector3(0.08f, 0.14f, 0.35f);
    }
}
