using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.NPCs.Bosses;
using Reign_of_the_Ancients.Tiles.DwarvenHold;
using Reign_of_the_Ancients.WorldGeneration;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Commands
{
    // Debug: /spawntitan wakes the Dwarven Titan in the foundry, even before Skeletron. Stand in the foundry first.
    public class SpawnTitanCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;
        public override string Command => "spawntitan";
        public override string Usage => "/spawntitan";
        public override string Description => "Debug: wakes the Dwarven Titan in the foundry, ignoring Skeletron (singleplayer only).";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                caller.Reply("Only works in singleplayer.", Color.OrangeRed);
                return;
            }
            if (NPC.AnyNPCs(ModContent.NPCType<DwarvenTitan>()))
            {
                caller.Reply("The Titan is already awake.", Color.OrangeRed);
                return;
            }
            if (!DwarvenHoldSystem.Exists)
            {
                caller.Reply("There is no Dwarven Hold in this world (use /buildhold first).", Color.OrangeRed);
                return;
            }
            Point tile = caller.Player.Center.ToTileCoordinates();
            if (!DwarvenHoldSystem.Layout.Foundry.Contains(tile))
            {
                caller.Reply("Stand inside the Titan foundry first (the big room at the far end of the mines).", Color.OrangeRed);
                return;
            }
            TitanControlMachine.WakeTitan(caller.Player);
        }
    }
}
