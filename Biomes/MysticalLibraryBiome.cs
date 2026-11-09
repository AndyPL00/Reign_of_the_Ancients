using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Walls.Library;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Biomes
{
    // Active while the player stands in front of any Mystical Library wall.
    public class MysticalLibraryBiome : ModBiome
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override string BestiaryIcon => "Reign_of_the_Ancients/icon_small"; // placeholder

        public override bool IsBiomeActive(Player player)
        {
            Point tile = player.Center.ToTileCoordinates();
            return MysticalLibrary.FloorAt(tile.X, tile.Y) != MysticalLibrary.NoFloor;
        }
    }

    public static class MysticalLibrary
    {
        public const int NoFloor = 0;
        public const int EntranceHall = 1;
        public const int ForbiddenArchive = 2;
        public const int Arena = 3;

        // Which library floor a tile belongs to, based on its background wall.
        public static int FloorAt(int x, int y)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return NoFloor;
            }

            int wall = Main.tile[x, y].WallType;
            if (wall == ModContent.WallType<LibraryWall>() || wall == ModContent.WallType<LibraryWindowWall>())
            {
                return EntranceHall;
            }
            if (wall == ModContent.WallType<ForbiddenLibraryWall>())
            {
                return ForbiddenArchive;
            }
            if (wall == ModContent.WallType<LibraryArenaWall>())
            {
                return Arena;
            }

            return NoFloor;
        }
    }
}
