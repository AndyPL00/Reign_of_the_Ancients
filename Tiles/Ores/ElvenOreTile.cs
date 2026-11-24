using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.Ores
{
    // Elven Ore (placeholder name). Spawns underground the first time Volodar is defeated (see ElvenOreSpawner).
    // The matching item (Items/Materials/ElvenOre) places this tile, so tModLoader drops it automatically when mined.
    public class ElvenOreTile : ModTile
    {
        public const int RequiredPickPower = 65; // same as Hellstone: needs the Nightmare/Deathbringer pickaxe
        private static readonly Color MapColor = new Color(90, 210, 180);

        // Placeholder: vanilla Mythril texture until Andy draws the ore.
        public override string Texture => "Terraria/Images/Tiles_" + TileID.Mythril;

        public override void SetStaticDefaults()
        {
            TileID.Sets.Ore[Type] = true;
            Main.tileSpelunker[Type] = true;
            Main.tileOreFinderPriority[Type] = 420; // just above Hellstone for the Metal Detector
            Main.tileShine2[Type] = true;
            Main.tileShine[Type] = 975;
            Main.tileMergeDirt[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;

            AddMapEntry(MapColor, CreateMapEntryName());

            MinPick = RequiredPickPower;
            DustType = DustID.GreenTorch;
            HitSound = SoundID.Tink;
        }
    }
}
