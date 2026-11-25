using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.WorldGeneration;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Commands
{
    // Debug: /buildhold builds the Dwarven Hold with its gate right in front of you (it extends toward the world's
    // middle and goes ~150 tiles down, so stand deep in the caverns). /buildhold open opens the gate.
    // Destroys whatever is in the way, so use it in a test world.
    public class BuildHoldCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;
        public override string Command => "buildhold";
        public override string Usage => "/buildhold [open]";
        public override string Description => "Debug: builds the Dwarven Hold at your position, or opens its gate (singleplayer only).";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                caller.Reply("Only works in singleplayer.", Color.OrangeRed);
                return;
            }

            if (args.Length > 0 && args[0] == "open")
            {
                if (!DwarvenHoldSystem.Exists)
                {
                    caller.Reply("There is no Dwarven Hold in this world.", Color.OrangeRed);
                    return;
                }
                DwarvenHoldSystem.OpenGate();
                caller.Reply("Gate opened.", Color.Orange);
                return;
            }

            // The gate goes a few tiles in front of you, its floor level with your feet.
            Point feet = caller.Player.Bottom.ToTileCoordinates();
            int side = feet.X < Main.maxTilesX / 2 ? 1 : -1;
            int gateX = feet.X + side * (DwarvenHoldBuilder.Lining + 6);
            if (!DwarvenHoldSystem.BuildAt(gateX, feet.Y))
            {
                caller.Reply("Not enough room here (too close to the world edge, or too close to the Underworld).", Color.OrangeRed);
                return;
            }
            caller.Reply("Dwarven Hold built. The gate opens once Skeletron is dead (or use /buildhold open).", Color.Orange);
        }
    }
}
