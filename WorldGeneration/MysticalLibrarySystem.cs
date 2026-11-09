using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace Reign_of_the_Ancients.WorldGeneration
{
    // Places the Mystical Library during world generation and remembers where it is
    // (saved with the world, synced to clients) so later content like Volodar can find the arena.
    public class MysticalLibrarySystem : ModSystem
    {
        // Site search
        private const int StrictAttempts = 400;
        private const int RelaxedAttempts = 200;
        private const double MinEdgeFraction = 0.12;  // how far from the world edge the entrance may be...
        private const double MaxEdgeFraction = 0.38;  // ...as a fraction of world width
        private const int MinSpawnDistance = 220;     // tiles from world spawn
        private const int MaxGroundVariation = 8;     // max height difference across the hall footprint
        private const float MinGrassFraction = 0.6f;  // "forest" check
        private const int WorldEdgePadding = 60;
        private const int UnderworldPadding = 30;

        public static bool Exists;
        public static LibraryLayout Layout;

        public override void ClearWorld()
        {
            Exists = false;
            Layout = default;
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            int index = tasks.FindIndex(pass => pass.Name.Equals("Final Cleanup"));
            var pass = new PassLegacy("Mystical Library", GenerateLibrary);
            if (index != -1)
            {
                tasks.Insert(index, pass);
            }
            else
            {
                tasks.Add(pass);
            }
        }

        private static void GenerateLibrary(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Building the Mystical Library";

            // Forest on the side of the world away from the Dungeon.
            int siteSide = Main.dungeonX < Main.maxTilesX / 2 ? 1 : -1;
            // Underground floors extend toward the middle of the world.
            int buildSide = siteSide > 0 ? -1 : 1;

            for (int attempt = 0; attempt < StrictAttempts + RelaxedAttempts; attempt++)
            {
                bool strict = attempt < StrictAttempts;
                double fraction = MinEdgeFraction + WorldGen.genRand.NextDouble() * (MaxEdgeFraction - MinEdgeFraction);
                int centerX = siteSide < 0 ? (int)(Main.maxTilesX * fraction) : (int)(Main.maxTilesX * (1 - fraction));

                if (Math.Abs(centerX - Main.spawnTileX) < MinSpawnDistance)
                {
                    continue;
                }

                int groundY = FindSurface(centerX);
                if (groundY < 0)
                {
                    continue;
                }

                LibraryLayout layout = MysticalLibraryBuilder.Plan(centerX, groundY, buildSide);
                if (!FitsInWorld(layout) || (strict && !IsGoodSite(layout)))
                {
                    continue;
                }

                Build(layout);
                return;
            }

            ModContent.GetInstance<Reign_of_the_Ancients>().Logger.Warn("Mystical Library: no valid site found, skipped.");
        }

        // Used by the /buildlibrary debug command.
        public static bool BuildAt(int centerX, int groundY)
        {
            int side = centerX < Main.maxTilesX / 2 ? 1 : -1;
            LibraryLayout layout = MysticalLibraryBuilder.Plan(centerX, groundY, side);
            if (!FitsInWorld(layout))
            {
                return false;
            }

            Build(layout);
            return true;
        }

        private static void Build(LibraryLayout layout)
        {
            MysticalLibraryBuilder.Build(layout);
            Layout = layout;
            Exists = true;
        }

        private static bool FitsInWorld(LibraryLayout layout)
        {
            return layout.Bounds.Left > WorldEdgePadding
                && layout.Bounds.Right < Main.maxTilesX - WorldEdgePadding
                && layout.Bounds.Top > WorldEdgePadding
                && layout.Bounds.Bottom < Main.UnderworldLayer - UnderworldPadding;
        }

        private static bool IsGoodSite(LibraryLayout layout)
        {
            int total = 0, grass = 0, bad = 0;
            int left = layout.Hall.X - MysticalLibraryBuilder.Wall;
            int right = layout.Hall.Right + MysticalLibraryBuilder.Wall;

            for (int x = left; x < right; x += 2)
            {
                total++;
                int y = FindSurface(x);
                if (y < 0 || Math.Abs(y - layout.GroundY) > MaxGroundVariation || Main.tile[x, y - 1].LiquidAmount > 0)
                {
                    bad++;
                    continue;
                }
                if (Main.tile[x, y].TileType == TileID.Grass)
                {
                    grass++;
                }
            }

            return bad <= total / 10 && grass >= total * MinGrassFraction;
        }

        // Highest solid ground tile in a column, skipping floating islands.
        private static int FindSurface(int x)
        {
            int maxY = Math.Min((int)Main.worldSurface + 50, Main.maxTilesY - 50);
            for (int y = 40; y < maxY; y++)
            {
                Tile tile = Main.tile[x, y];
                if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
                {
                    continue;
                }

                // Real ground is mostly solid underneath; sky islands have air below them.
                int air = 0;
                for (int k = 1; k <= 35; k++)
                {
                    Tile below = Main.tile[x, y + k];
                    if (!below.HasTile || !Main.tileSolid[below.TileType])
                    {
                        air++;
                    }
                }
                if (air <= 10)
                {
                    return y;
                }
            }

            return -1;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (Exists)
            {
                tag["libraryX"] = Layout.CenterX;
                tag["libraryGroundY"] = Layout.GroundY;
                tag["librarySide"] = Layout.Side;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Exists = tag.ContainsKey("libraryX");
            if (Exists)
            {
                Layout = MysticalLibraryBuilder.Plan(tag.GetInt("libraryX"), tag.GetInt("libraryGroundY"), tag.GetInt("librarySide"));
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(Exists);
            writer.Write(Layout.CenterX);
            writer.Write(Layout.GroundY);
            writer.Write(Layout.Side);
        }

        public override void NetReceive(BinaryReader reader)
        {
            Exists = reader.ReadBoolean();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int side = reader.ReadInt32();
            Layout = Exists ? MysticalLibraryBuilder.Plan(x, y, side) : default;
        }
    }
}
