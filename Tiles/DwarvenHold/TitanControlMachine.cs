using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Reign_of_the_Ancients.Tiles.DwarvenHold
{
    // The great lever/machine in the Titan foundry that wakes the Dwarven Titan (placeholder name and art).
    // Placed only by world gen and can't be removed. The Titan itself comes later: for now it just shows a message.
    public class TitanControlMachine : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;
            TileID.Sets.HasOutlines[Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(200, 140, 60), CreateMapEntryName());
            DustType = DustID.Copper;
            MinPick = int.MaxValue;
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => false;

        public override bool CanExplode(int i, int j) => false;

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemID.Lever;
        }

        public override bool RightClick(int i, int j)
        {
            // Placeholder until the Dwarven Titan exists.
            Main.NewText(Language.GetTextValue("Mods.Reign_of_the_Ancients.TitanMachineDormant"), new Color(230, 160, 80));
            return true;
        }
    }
}
