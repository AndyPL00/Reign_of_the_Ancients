using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.DwarvenHold
{
    // Dwarven Hold blocks (placeholder art reuses vanilla textures).
    // The whole hold is sealed until Skeletron is dead: its blocks can't be mined or blown up before that,
    // so the only way in is the great gate (which opens by itself, see DwarvenHoldSystem).
    public abstract class HoldBlockBase : ModTile
    {
        public const int RequiredPickPower = 100; // after Skeletron: Molten / Elven pickaxe

        protected abstract int VanillaTileID { get; }
        protected abstract Color MapColor { get; }
        protected virtual int BlockDust => DustID.Stone;

        public override string Texture => "Terraria/Images/Tiles_" + VanillaTileID;

        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileMergeDirt[Type] = false;
            MinPick = RequiredPickPower;
            DustType = BlockDust;
            HitSound = SoundID.Tink;
            AddMapEntry(MapColor);
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => NPC.downedBoss3;

        public override bool CanExplode(int i, int j) => false;
    }

    // Gatehouse and floor tops: grey dwarven stone.
    public class HoldBrick : HoldBlockBase
    {
        protected override int VanillaTileID => TileID.GrayBrick;
        protected override Color MapColor => new Color(105, 100, 95);
    }

    // Great hall and forges: dark red-brown brick.
    public class HoldRedBrick : HoldBlockBase
    {
        protected override int VanillaTileID => TileID.RedBrick;
        protected override Color MapColor => new Color(110, 55, 40);
    }

    // Glowing molten seams traced through the hull, under the floors and around the ceilings.
    public class HoldMoltenTrim : HoldBlockBase
    {
        private static readonly Vector3 Glow = new Vector3(0.55f, 0.22f, 0.04f);

        protected override int VanillaTileID => TileID.Lavafall;
        protected override Color MapColor => new Color(235, 110, 30);
        protected override int BlockDust => DustID.Torch;

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = Glow.X;
            g = Glow.Y;
            b = Glow.Z;
        }
    }

    // Brass trim: hall floors, bands, pillar caps.
    public class HoldBrass : HoldBlockBase
    {
        protected override int VanillaTileID => TileID.CopperPlating;
        protected override Color MapColor => new Color(185, 120, 60);
        protected override int BlockDust => DustID.Copper;
    }

    // Mines: rough carved stone lining every tunnel.
    public class HoldMineRock : HoldBlockBase
    {
        protected override int VanillaTileID => TileID.StoneSlab;
        protected override Color MapColor => new Color(80, 76, 72);
    }

    // Forges and the Titan foundry: heat-blackened brick.
    public class FoundryBrick : HoldBlockBase
    {
        protected override int VanillaTileID => TileID.HellstoneBrick;
        protected override Color MapColor => new Color(120, 50, 35);
        protected override int BlockDust => DustID.Torch;
    }

    // The great gate. Never breakable by hand; DwarvenHoldSystem removes it the first time Skeletron dies.
    public class HoldGate : HoldBlockBase
    {
        protected override int VanillaTileID => TileID.TinPlating;
        protected override Color MapColor => new Color(150, 140, 120);

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;
    }
}
