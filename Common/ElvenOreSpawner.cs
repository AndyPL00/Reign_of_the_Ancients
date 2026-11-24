using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Tiles.Ores;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Scatters Elven Ore veins through the underground and cavern layers. Called once, the first time Volodar dies.
    // Runs only on the server / in single player; changed tiles are sent to clients.
    public static class ElvenOreSpawner
    {
        private const double VeinsPerTile = 2.5E-05;  // ~125 veins in a small world, ~550 in a large one
        private const int EdgeMargin = 100;           // tiles kept clear of the world's left/right edges
        private const int UnderworldMargin = 30;      // stop this many tiles above the Underworld
        private const int MinStrength = 4;            // vein thickness
        private const int MaxStrength = 7;
        private const int MinSteps = 3;               // vein length
        private const int MaxSteps = 7;
        private const int SyncRadius = 14;            // tiles around each vein sent to clients
        private const int PlacementTries = 10;        // retries to find a solid tile for each vein

        private const string MessageKey = "Mods.Reign_of_the_Ancients.ElvenOreSpawned";
        private static readonly Color MessageColor = new Color(50, 255, 130); // same green as vanilla ore messages

        public static void Spawn()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            ushort oreType = (ushort)ModContent.TileType<ElvenOreTile>();
            int veins = (int)(Main.maxTilesX * Main.maxTilesY * VeinsPerTile);
            int top = (int)Main.worldSurface;
            int bottom = Main.UnderworldLayer - UnderworldMargin;

            for (int v = 0; v < veins; v++)
            {
                for (int attempt = 0; attempt < PlacementTries; attempt++)
                {
                    int x = WorldGen.genRand.Next(EdgeMargin, Main.maxTilesX - EdgeMargin);
                    int y = WorldGen.genRand.Next(top, bottom);
                    if (!Main.tile[x, y].HasTile)
                    {
                        continue;
                    }

                    // OreRunner only replaces ordinary stone/dirt-type tiles, so structures (incl. the Library) are safe.
                    WorldGen.OreRunner(x, y, WorldGen.genRand.Next(MinStrength, MaxStrength + 1),
                        WorldGen.genRand.Next(MinSteps, MaxSteps + 1), oreType);
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendTileSquare(-1, x - SyncRadius, y - SyncRadius, SyncRadius * 2 + 1, SyncRadius * 2 + 1);
                    }
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(Language.GetTextValue(MessageKey)))
            {
                return;
            }
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(Language.GetTextValue(MessageKey), MessageColor);
            }
            else
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey(MessageKey), MessageColor);
            }
        }
    }
}
