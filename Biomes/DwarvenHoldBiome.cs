using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Walls.DwarvenHold;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Biomes
{
    // Active while the player stands in front of any Dwarven Hold wall.
    public class DwarvenHoldBiome : ModBiome
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override string BestiaryIcon => "Reign_of_the_Ancients/icon_small"; // placeholder

        // Placeholder vanilla track; swap for MusicLoader.GetMusicSlot(Mod, "Assets/Music/<FileName>") later.
        public override int Music => MusicID.UndergroundDesert;

        public override bool IsBiomeActive(Player player)
        {
            Point tile = player.Center.ToTileCoordinates();
            return DwarvenHold.ZoneAt(tile.X, tile.Y) != DwarvenHold.NoZone;
        }
    }

    public static class DwarvenHold
    {
        public const int NoZone = 0;
        public const int GreatHall = 1;  // gatehouse + great hall
        public const int Forges = 2;
        public const int Mines = 3;
        public const int Foundry = 4;    // Titan arena, no spawns

        public static int ZoneAt(int x, int y)
        {
            return WorldGen.InWorld(x, y) ? ZoneOfWall(Main.tile[x, y].WallType) : NoZone;
        }

        public static int ZoneOfWall(int wall)
        {
            if (wall == ModContent.WallType<HoldHallWall>() || wall == ModContent.WallType<HoldPillarWall>()
                || wall == ModContent.WallType<HoldGatehouseWall>() || wall == ModContent.WallType<HoldWindowWall>())
            {
                return GreatHall;
            }
            if (wall == ModContent.WallType<HoldForgeWall>() || wall == ModContent.WallType<HoldPipeWall>() || wall == ModContent.WallType<HoldCogWall>()
                || wall == ModContent.WallType<HoldMoltenStreamWall>())
            {
                return Forges;
            }
            if (wall == ModContent.WallType<HoldMineWall>())
            {
                return Mines;
            }
            if (wall == ModContent.WallType<FoundryWall>() || wall == ModContent.WallType<FoundryMoltenWall>())
            {
                return Foundry;
            }
            return NoZone;
        }
    }
}
