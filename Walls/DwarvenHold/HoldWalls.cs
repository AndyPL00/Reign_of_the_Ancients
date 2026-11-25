using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Walls.DwarvenHold
{
    // Dwarven Hold background walls (placeholder art reuses vanilla walls).
    // The wall type is also how the mod tells which hold zone a tile belongs to (DwarvenHold.ZoneOfWall).
    public abstract class HoldWallBase : ModWall
    {
        protected abstract int VanillaWallID { get; }
        protected abstract Color MapColor { get; }
        protected virtual Vector3 Glow => Vector3.Zero;

        public override string Texture => "Terraria/Images/Wall_" + VanillaWallID;

        public override void SetStaticDefaults()
        {
            Main.wallHouse[Type] = false; // enemies can spawn in front of it
            DustType = DustID.Stone;
            AddMapEntry(MapColor);
        }

        // Sealed until Skeletron is dead, like the hold's blocks.
        public override void KillWall(int i, int j, ref bool fail)
        {
            if (!NPC.downedBoss3)
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

    // ----- Zone 1: gatehouse and great hall -----
    public class HoldGatehouseWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.GrayBrick;
        protected override Color MapColor => new Color(60, 58, 55);
    }

    public class HoldHallWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.RedBrick;
        protected override Color MapColor => new Color(70, 35, 28);
    }

    // Dark stone of the hall's pillars (walk-through, drawn as background).
    public class HoldPillarWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.ObsidianBrick;
        protected override Color MapColor => new Color(40, 28, 30);
    }

    // Tall framed windows between the pillars.
    public class HoldWindowWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.YellowStainedGlass;
        protected override Color MapColor => new Color(150, 130, 60);
    }

    // ----- Zone 2: forges -----
    public class HoldForgeWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.HellstoneBrick;
        protected override Color MapColor => new Color(55, 52, 50);
    }

    // Cog panels behind the forge machinery.
    public class HoldCogWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.CogWall;
        protected override Color MapColor => new Color(120, 100, 70);
    }

    // The molten stream pouring from the forge crucible.
    public class HoldMoltenStreamWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.Lavafall;
        protected override Color MapColor => new Color(240, 170, 40);
        protected override Vector3 Glow => new Vector3(0.7f, 0.45f, 0.1f);
    }

    // Vertical steam pipes running up the forge walls.
    public class HoldPipeWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.TinPlating;
        protected override Color MapColor => new Color(110, 105, 95);
    }

    // ----- Zone 3: mines -----
    public class HoldMineWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.Planked;
        protected override Color MapColor => new Color(70, 52, 38);
    }

    // ----- Zone 4: Titan foundry -----
    public class FoundryWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.ObsidianBrick;
        protected override Color MapColor => new Color(70, 30, 22);
    }

    // Glowing molten seams behind the foundry's lava channels.
    public class FoundryMoltenWall : HoldWallBase
    {
        protected override int VanillaWallID => WallID.Lavafall;
        protected override Color MapColor => new Color(230, 110, 30);
        protected override Vector3 Glow => new Vector3(0.6f, 0.25f, 0.05f);
    }
}
