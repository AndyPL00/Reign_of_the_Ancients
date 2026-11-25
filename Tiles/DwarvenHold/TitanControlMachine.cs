using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.NPCs.Bosses;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Reign_of_the_Ancients.Tiles.DwarvenHold
{
    // The great lever/machine in the Titan foundry that wakes the Dwarven Titan (placeholder name and art).
    // Placed only by world gen and can't be removed.
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

        // Pull the lever to wake the Dwarven Titan. Only after Skeletron, and only when it isn't already awake;
        // the machine can be used again after every fight.
        public override bool RightClick(int i, int j)
        {
            int titan = ModContent.NPCType<DwarvenTitan>();
            if (NPC.AnyNPCs(titan))
            {
                return true;
            }
            Player player = Main.LocalPlayer;
            if (!NPC.downedBoss3)
            {
                Main.NewText(Language.GetTextValue("Mods.Reign_of_the_Ancients.TitanMachineDormant"), new Color(230, 160, 80));
                return true;
            }
            WakeTitan(player);
            return true;
        }

        public static void WakeTitan(Player player)
        {
            int titan = ModContent.NPCType<DwarvenTitan>();
            SoundEngine.PlaySound(SoundID.Unlock, player.Center);
            SoundEngine.PlaySound(SoundID.Item149 with { Pitch = -0.5f }, player.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.SpawnOnPlayer(player.whoAmI, titan);
            }
            else
            {
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: titan);
            }
        }
    }
}
