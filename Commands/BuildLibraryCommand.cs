using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.WorldGeneration;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Commands
{
    // Debug: /buildlibrary builds the Mystical Library with its entrance floor under your feet.
    // Destroys whatever is in the way, so use it in a test world.
    public class BuildLibraryCommand : ModCommand
    {
        public override CommandType Type => CommandType.World;
        public override string Command => "buildlibrary";
        public override string Usage => "/buildlibrary";
        public override string Description => "Debug: builds the Mystical Library at your position (singleplayer only).";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.netMode != NetmodeID.SinglePlayer)
            {
                caller.Reply("Only works in singleplayer.", Color.OrangeRed);
                return;
            }

            Point feet = caller.Player.Bottom.ToTileCoordinates();
            if (!MysticalLibrarySystem.BuildAt(feet.X, feet.Y))
            {
                caller.Reply("Not enough room here (too close to the world edge or too deep).", Color.OrangeRed);
                return;
            }

            caller.Reply("Mystical Library built.", Color.MediumPurple);
        }
    }
}
