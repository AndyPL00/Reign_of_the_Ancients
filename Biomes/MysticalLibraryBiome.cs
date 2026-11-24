using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Walls.Library;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Biomes
{
    // Active while the player stands in front of any Mystical Library wall.
    public class MysticalLibraryBiome : ModBiome
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override string BestiaryIcon => "Reign_of_the_Ancients/icon_small"; // placeholder

        // Placeholder vanilla track. To use a custom one, drop an .ogg in Assets/Music/ and return
        // MusicLoader.GetMusicSlot(Mod, "Assets/Music/<FileName>") instead.
        public override int Music => MusicID.Space;

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
        public const int ThroneRoom = 3;

        // Which library floor a tile belongs to, based on its background wall.
        public static int FloorAt(int x, int y)
        {
            return WorldGen.InWorld(x, y) ? FloorOfWall(Main.tile[x, y].WallType) : NoFloor;
        }

        public static int FloorOfWall(int wall)
        {
            if (wall == ModContent.WallType<LibraryWall>() ||
                wall == ModContent.WallType<LibraryWindowWall>() ||
                wall == ModContent.WallType<LibraryRoseWindowWall>() ||
                wall == ModContent.WallType<HallPillarWall>())
            {
                return EntranceHall;
            }
            if (wall == ModContent.WallType<ForbiddenLibraryWall>() ||
                wall == ModContent.WallType<VaultWall>() ||
                wall == ModContent.WallType<VaultFenceWall>() ||
                wall == ModContent.WallType<ArchivePillarWall>() ||
                wall == ModContent.WallType<ArchiveNicheWall>())
            {
                return ForbiddenArchive;
            }
            if (wall == ModContent.WallType<ThroneRoomWall>() ||
                wall == ModContent.WallType<ThroneBackdropWall>() ||
                wall == ModContent.WallType<ThronePillarWall>() ||
                wall == ModContent.WallType<ThroneStarWall>())
            {
                return ThroneRoom;
            }
            return NoFloor;
        }
    }
}
