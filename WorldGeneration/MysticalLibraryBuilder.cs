using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Tiles.Library;
using Reign_of_the_Ancients.Walls.Library;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.WorldGeneration
{
    // Room interiors in tile coordinates. Each room's floor surface is the row at rect.Bottom.
    public struct LibraryLayout
    {
        public int CenterX;   // middle of the entrance hall
        public int GroundY;   // entrance hall floor row
        public int Side;      // +1: underground floors extend right of the hall, -1: left
        public Rectangle Hall;
        public Rectangle Archive;
        public Rectangle Arena;
        public int Stair1X;   // top step of the hall -> archive staircase
        public int Stair2X;   // top step of the archive -> arena staircase
        public Rectangle Bounds; // everything the builder touches
    }

    // Layout:
    //   Floor 1 (surface): cathedral entrance hall with a gable roof and spire.
    //   A grand staircase descends toward Side and enters the Forbidden Archive through its wall.
    //   Floor 2 (underground): darker archive with a platform gallery.
    //   A second staircase turns back and enters the arena through its side wall.
    //   Floor 3: open arena for the Volodar fight (sits beneath the cathedral).
    public static class MysticalLibraryBuilder
    {
        // Rooms
        public const int Wall = 3;                  // shell thickness
        public const int HallWidth = 140;
        public const int HallHeight = 30;
        public const int ArchiveWidth = 180;
        public const int ArchiveHeight = 24;
        public const int ArenaWidth = 150;
        public const int ArenaHeight = 44;
        public const int GroundBetweenFloors = 10;  // natural ground between one room's floor and the next room's ceiling

        // Roof
        public const int RoofOverhang = 2;
        public const int RoofRun = 3;               // roof rises 1 tile every 3 tiles
        public const int SpireWidth = 4;
        public const int SpireHeight = 14;
        public const int SpireTipHeight = 4;

        // Staircases (45 degrees, 1 tile down per tile across)
        public const int StairHeadroom = 8;
        public const int StairThickness = 3;
        public const int StairLining = 2;
        public const int Stair1Offset = 50;         // from hall center toward Side
        public const int Stair2Inset = 20;          // from the archive's entry wall
        public const int StairTorchSpacing = 6;

        // Entrance hall details
        public const int DoorHeight = 6;
        public const int PlazaMargin = 10;          // flattened ground outside each door
        public const int FoundationDepth = 15;
        public const int GalleryHeight = 12;        // balcony height above the floor
        public const int GalleryLength = 40;
        public const int BaySpacing = 20;           // window / column / chandelier rhythm
        public const int WindowWidth = 4;
        public const int HallBookcaseSpacing = 7;
        public const int GalleryBookcaseSpacing = 5;
        public const int TorchHeight = 6;

        // Forbidden archive details
        public const int ArchiveGallerySegment = 40;
        public const int ArchiveGalleryGap = 8;     // gaps with ladder platforms between gallery segments
        public const int ArchiveBookcaseSpacing = 7;
        public const float ArchiveBookcaseChance = 0.75f; // some shelves missing: abandoned feel
        public const int ArchiveTorchSpacing = 24;
        public const int ChainSpacing = 15;
        public const float CobwebChance = 0.12f;

        // Arena details
        public const int ArenaPlatformRows = 4;
        public const int ArenaPlatformSpacing = 8;
        public const int ArenaPlatformInset = 12;
        public const int ArenaTorchSpacing = 8;

        // Vanilla furniture styles
        private const int HallTorchStyle = 5;       // white
        private const int ForbiddenTorchStyle = 4;  // purple
        private const int ArenaTorchStyle = 9;      // ice
        private const int ChandelierStyle = 2;      // gold
        private const int BannerStyles = 4;         // red / green / blue / yellow

        private static ushort hallBrick, hallTrim, forbiddenBrick, arenaBrick;
        private static ushort hallWall, windowWall, forbiddenWall, arenaWall;

        public static LibraryLayout Plan(int centerX, int groundY, int side)
        {
            var l = new LibraryLayout { CenterX = centerX, GroundY = groundY, Side = side };

            l.Hall = new Rectangle(centerX - HallWidth / 2, groundY - HallHeight, HallWidth, HallHeight);

            int archiveFloor = groundY + Wall + GroundBetweenFloors + Wall + ArchiveHeight;
            l.Stair1X = centerX + side * Stair1Offset;
            int stair1End = l.Stair1X + side * (archiveFloor - groundY);
            int archiveX = side > 0 ? stair1End + 1 : stair1End - ArchiveWidth;
            l.Archive = new Rectangle(archiveX, archiveFloor - ArchiveHeight, ArchiveWidth, ArchiveHeight);

            int arenaFloor = archiveFloor + Wall + GroundBetweenFloors + Wall + ArenaHeight;
            l.Stair2X = side > 0 ? l.Archive.X + Stair2Inset : l.Archive.Right - 1 - Stair2Inset;
            int stair2End = l.Stair2X - side * (arenaFloor - archiveFloor);
            int arenaX = side > 0 ? stair2End - ArenaWidth : stair2End + 1;
            l.Arena = new Rectangle(arenaX, arenaFloor - ArenaHeight, ArenaWidth, ArenaHeight);

            int left = Math.Min(l.Hall.X - Wall - PlazaMargin, Math.Min(l.Archive.X, l.Arena.X) - Wall);
            int right = Math.Max(l.Hall.Right + Wall + PlazaMargin, Math.Max(l.Archive.Right, l.Arena.Right) + Wall);
            int top = SpireTop(l) - 10;
            int bottom = l.Arena.Bottom + Wall;
            l.Bounds = new Rectangle(left, top, right - left, bottom - top);
            return l;
        }

        private static int RoofHalfWidth => HallWidth / 2 + Wall + RoofOverhang;
        private static int RoofBase(LibraryLayout l) => l.Hall.Y - Wall - 1;
        private static int RoofPeak(LibraryLayout l) => RoofBase(l) - RoofHalfWidth / RoofRun;
        private static int SpireTop(LibraryLayout l) => RoofPeak(l) - SpireHeight - SpireTipHeight;

        public static void Build(LibraryLayout l)
        {
            LoadTypes();
            var interiors = new[] { l.Hall, l.Archive, l.Arena };
            var torches = new List<(int x, int y, int style)>();

            PrepareSite(l);

            Box(l.Hall, hallBrick, hallWall);
            Box(l.Archive, forbiddenBrick, forbiddenWall);
            Box(l.Arena, arenaBrick, arenaWall);
            HallTrim(l);
            Roof(l);

            Staircase(l.Stair1X, l.GroundY, l.Archive.Bottom - l.GroundY, l.Side,
                hallBrick, hallWall, forbiddenBrick, forbiddenWall, HallTorchStyle, ForbiddenTorchStyle, interiors, torches);
            Staircase(l.Stair2X, l.Archive.Bottom, l.Arena.Bottom - l.Archive.Bottom, -l.Side,
                forbiddenBrick, forbiddenWall, arenaBrick, arenaWall, ForbiddenTorchStyle, ArenaTorchStyle, interiors, torches);

            HallStructure(l);
            ArchiveStructure(l);
            ArenaStructure(l);

            WorldGen.RangeFrame(l.Bounds.Left, l.Bounds.Top, l.Bounds.Right, l.Bounds.Bottom);

            HallFurniture(l, torches);
            ArchiveFurniture(l, torches);
            ArenaFurniture(l, torches);

            foreach (var (x, y, style) in torches)
            {
                WorldGen.PlaceTile(x, y, TileID.Torches, true, false, -1, style);
            }
        }

        private static void LoadTypes()
        {
            hallBrick = (ushort)ModContent.TileType<LibraryBrick>();
            hallTrim = (ushort)ModContent.TileType<LibraryTrim>();
            forbiddenBrick = (ushort)ModContent.TileType<ForbiddenBrick>();
            arenaBrick = (ushort)ModContent.TileType<ArenaBrick>();
            hallWall = (ushort)ModContent.WallType<LibraryWall>();
            windowWall = (ushort)ModContent.WallType<LibraryWindowWall>();
            forbiddenWall = (ushort)ModContent.WallType<ForbiddenLibraryWall>();
            arenaWall = (ushort)ModContent.WallType<LibraryArenaWall>();
        }

        // ---------- Terrain ----------

        // Clears the sky above the hall and plaza, then fills holes so the hall and plaza sit on solid, flat ground.
        private static void PrepareSite(LibraryLayout l)
        {
            int left = l.Hall.X - Wall - PlazaMargin;
            int right = l.Hall.Right + Wall + PlazaMargin;

            for (int x = left; x < right; x++)
            {
                for (int y = l.Bounds.Top; y < l.GroundY; y++)
                {
                    Main.tile[x, y].ClearEverything();
                }
                for (int y = l.GroundY; y < l.GroundY + Wall + FoundationDepth; y++)
                {
                    Tile tile = Main.tile[x, y];
                    tile.LiquidAmount = 0;
                    if (!tile.HasTile || !Main.tileSolid[tile.TileType])
                    {
                        SetTile(x, y, TileID.Dirt);
                    }
                }
            }
        }

        // ---------- Rooms ----------

        // Solid shell of `brick` around an empty interior, wall everywhere.
        private static void Box(Rectangle interior, ushort brick, ushort wall)
        {
            for (int x = interior.X - Wall; x < interior.Right + Wall; x++)
            {
                for (int y = interior.Y - Wall; y < interior.Bottom + Wall; y++)
                {
                    Main.tile[x, y].ClearEverything();
                    SetWall(x, y, wall);
                    if (!interior.Contains(x, y))
                    {
                        SetTile(x, y, brick);
                    }
                }
            }
        }

        private static void HallTrim(LibraryLayout l)
        {
            for (int x = l.Hall.X - Wall; x < l.Hall.Right + Wall; x++)
            {
                SetTile(x, l.Hall.Y - 1, hallTrim);  // ceiling edge
                SetTile(x, l.GroundY, hallTrim);     // floor
            }
        }

        private static void Roof(LibraryLayout l)
        {
            int baseY = RoofBase(l);
            int halfWidth = RoofHalfWidth;
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                int x = l.CenterX + dx;
                int rise = (halfWidth - Math.Abs(dx)) / RoofRun;
                for (int y = baseY - rise; y <= baseY; y++)
                {
                    SetTile(x, y, y == baseY - rise ? hallTrim : hallBrick);
                }
            }

            int peakY = RoofPeak(l);
            for (int x = l.CenterX - SpireWidth / 2; x < l.CenterX + SpireWidth / 2; x++)
            {
                for (int y = peakY - SpireHeight; y <= peakY; y++)
                {
                    SetTile(x, y, hallBrick);
                }
            }
            for (int x = l.CenterX - 1; x <= l.CenterX; x++)
            {
                for (int y = SpireTop(l); y < peakY - SpireHeight; y++)
                {
                    SetTile(x, y, hallTrim);
                }
            }
        }

        // 45-degree staircase carved through the ground, lined with brick. The top half uses the upper
        // floor's materials and the bottom half the lower floor's, so the theme shifts as you descend.
        private static void Staircase(int startX, int startY, int depth, int dir,
            ushort upperBrick, ushort upperWall, ushort lowerBrick, ushort lowerWall,
            int upperTorch, int lowerTorch, Rectangle[] interiors, List<(int, int, int)> torches)
        {
            for (int i = 0; i <= depth; i++)
            {
                int x = startX + dir * i;
                int step = startY + i;
                bool lowerHalf = i > depth / 2;
                ushort brick = lowerHalf ? lowerBrick : upperBrick;
                ushort wall = lowerHalf ? lowerWall : upperWall;

                for (int y = step - StairHeadroom - StairLining; y < step - StairHeadroom; y++)
                {
                    if (!InsideAny(interiors, x, y))
                    {
                        SetTile(x, y, brick);
                        SetWall(x, y, wall);
                    }
                }
                for (int y = step - StairHeadroom; y < step; y++)
                {
                    Tile tile = Main.tile[x, y];
                    tile.HasTile = false;
                    tile.LiquidAmount = 0;
                    if (!IsLibraryWall(tile.WallType))
                    {
                        SetWall(x, y, wall);
                    }
                }
                for (int y = step; y < step + StairThickness; y++)
                {
                    if (!InsideAny(interiors, x, y))
                    {
                        SetTile(x, y, brick);
                        SetWall(x, y, wall);
                    }
                }

                if (i % StairTorchSpacing == StairTorchSpacing / 2)
                {
                    torches.Add((x, step - 4, lowerHalf ? lowerTorch : upperTorch));
                }
            }
        }

        // ---------- Floor 1: entrance hall ----------

        private static void HallStructure(LibraryLayout l)
        {
            Rectangle hall = l.Hall;
            int floor = l.GroundY;

            // Doors on both sides, with a gold lintel.
            foreach (int doorX in new[] { hall.X - Wall, hall.Right })
            {
                for (int x = doorX; x < doorX + Wall; x++)
                {
                    for (int y = floor - DoorHeight; y < floor; y++)
                    {
                        Tile doorTile = Main.tile[x, y];
                        doorTile.HasTile = false;
                    }
                    SetTile(x, floor - DoorHeight - 1, hallTrim);
                }
            }

            // Tall stained-glass windows with pointed tops.
            int windowTop = hall.Y + 3;
            int windowBottom = floor - GalleryHeight - 3;
            for (int bx = hall.X + 8; bx + WindowWidth <= hall.Right - 4; bx += BaySpacing)
            {
                for (int y = windowTop; y <= windowBottom; y++)
                {
                    for (int k = 0; k < WindowWidth; k++)
                    {
                        if (y == windowTop && (k == 0 || k == WindowWidth - 1))
                        {
                            continue;
                        }
                        SetWall(bx + k, y, windowWall);
                    }
                }

                // Marble column between windows, floor to ceiling.
                int columnX = bx + BaySpacing / 2 + WindowWidth / 2;
                if (columnX < hall.Right - 2 && !NearStairMouth(l, columnX))
                {
                    for (int y = hall.Y; y < floor; y++)
                    {
                        SetTile(columnX, y, TileID.MarbleColumn);
                    }
                }
            }

            // Balconies along both ends, each reached by two ladder platforms.
            int galleryY = floor - GalleryHeight;
            for (int x = hall.X; x < hall.X + GalleryLength; x++)
            {
                SetPlatform(x, galleryY);
            }
            for (int x = hall.Right - GalleryLength; x < hall.Right; x++)
            {
                SetPlatform(x, galleryY);
            }
            for (int k = 0; k < 4; k++)
            {
                for (int h = 4; h < GalleryHeight; h += 4)
                {
                    SetPlatform(hall.X + GalleryLength + k, floor - h);
                    SetPlatform(hall.Right - GalleryLength - 1 - k, floor - h);
                }
            }
        }

        private static void HallFurniture(LibraryLayout l, List<(int, int, int)> torches)
        {
            Rectangle hall = l.Hall;
            int floor = l.GroundY;
            int galleryY = floor - GalleryHeight;

            for (int x = hall.X + 3; x < hall.Right - 3; x += HallBookcaseSpacing)
            {
                if (!NearStairMouth(l, x))
                {
                    WorldGen.PlaceObject(x, floor - 1, TileID.Bookcases, true);
                }
            }
            for (int x = hall.X + 2; x < hall.X + GalleryLength - 2; x += GalleryBookcaseSpacing)
            {
                WorldGen.PlaceObject(x, galleryY - 1, TileID.Bookcases, true);
            }
            for (int x = hall.Right - 3; x > hall.Right - GalleryLength + 2; x -= GalleryBookcaseSpacing)
            {
                WorldGen.PlaceObject(x, galleryY - 1, TileID.Bookcases, true);
            }

            for (int bx = hall.X + 8; bx + WindowWidth <= hall.Right - 4; bx += BaySpacing)
            {
                WorldGen.PlaceObject(bx + WindowWidth / 2, hall.Y, TileID.Chandeliers, true, ChandelierStyle);
                WorldGen.PlaceObject(bx + WindowWidth + 3, hall.Y, TileID.Banners, true, WorldGen.genRand.Next(BannerStyles));

                int columnX = bx + BaySpacing / 2 + WindowWidth / 2;
                torches.Add((columnX - 2, floor - TorchHeight, HallTorchStyle));
                torches.Add((columnX + 2, floor - TorchHeight, HallTorchStyle));
            }
        }

        // ---------- Floor 2: forbidden archive ----------

        private static void ArchiveStructure(LibraryLayout l)
        {
            Rectangle archive = l.Archive;
            int floor = archive.Bottom;
            int galleryY = floor - GalleryHeight;

            // Full-length gallery with gaps; each gap has ladder platforms up from the floor.
            for (int x = archive.X; x < archive.Right; x++)
            {
                int offset = DistanceFromEntry(l, x);
                if (offset % ArchiveGallerySegment < ArchiveGalleryGap)
                {
                    for (int h = 4; h < GalleryHeight; h += 4)
                    {
                        SetPlatform(x, floor - h);
                    }
                }
                else
                {
                    SetPlatform(x, galleryY);
                }
            }

            // Hanging chains and cobwebs along the ceiling.
            for (int x = archive.X + 7; x < archive.Right; x += ChainSpacing)
            {
                int length = WorldGen.genRand.Next(3, 8);
                for (int y = archive.Y; y < archive.Y + length; y++)
                {
                    if (!Main.tile[x, y].HasTile)
                    {
                        SetTile(x, y, TileID.Chain);
                    }
                }
            }
            for (int x = archive.X; x < archive.Right; x++)
            {
                for (int y = archive.Y; y < archive.Y + 4; y++)
                {
                    if (!Main.tile[x, y].HasTile && WorldGen.genRand.NextFloat() < CobwebChance)
                    {
                        SetTile(x, y, TileID.Cobweb);
                    }
                }
            }
        }

        private static void ArchiveFurniture(LibraryLayout l, List<(int, int, int)> torches)
        {
            Rectangle archive = l.Archive;
            int floor = archive.Bottom;
            int galleryY = floor - GalleryHeight;

            for (int x = archive.X + 3; x < archive.Right - 3; x += ArchiveBookcaseSpacing)
            {
                if (WorldGen.genRand.NextFloat() < ArchiveBookcaseChance)
                {
                    WorldGen.PlaceObject(x, floor - 1, TileID.Bookcases, true);
                }
                if (WorldGen.genRand.NextFloat() < ArchiveBookcaseChance)
                {
                    WorldGen.PlaceObject(x, galleryY - 1, TileID.Bookcases, true);
                }
            }

            for (int x = archive.X + ArchiveTorchSpacing / 2; x < archive.Right; x += ArchiveTorchSpacing)
            {
                torches.Add((x, floor - TorchHeight, ForbiddenTorchStyle));
                torches.Add((x, galleryY - TorchHeight, ForbiddenTorchStyle));
            }
        }

        // ---------- Floor 3: arena ----------

        private static void ArenaStructure(LibraryLayout l)
        {
            Rectangle arena = l.Arena;
            for (int row = 1; row <= ArenaPlatformRows; row++)
            {
                int y = arena.Bottom - row * ArenaPlatformSpacing;
                for (int x = arena.X + ArenaPlatformInset; x < arena.Right - ArenaPlatformInset; x++)
                {
                    SetPlatform(x, y);
                }
            }
        }

        private static void ArenaFurniture(LibraryLayout l, List<(int, int, int)> torches)
        {
            Rectangle arena = l.Arena;
            for (int y = arena.Bottom - 4; y > arena.Y + 2; y -= ArenaTorchSpacing)
            {
                torches.Add((arena.X, y, ArenaTorchStyle));
                torches.Add((arena.Right - 1, y, ArenaTorchStyle));
            }
        }

        // ---------- Helpers ----------

        // Distance from the archive wall that the first staircase comes through.
        private static int DistanceFromEntry(LibraryLayout l, int x)
        {
            return l.Side > 0 ? x - l.Archive.X : l.Archive.Right - 1 - x;
        }

        // Keeps columns and bookcases out of the hall floor opening above the first staircase.
        private static bool NearStairMouth(LibraryLayout l, int x)
        {
            int a = l.Stair1X - 2 * l.Side;
            int b = l.Stair1X + l.Side * (StairHeadroom + Wall + 2);
            return x >= Math.Min(a, b) && x <= Math.Max(a, b);
        }

        private static bool InsideAny(Rectangle[] rects, int x, int y)
        {
            foreach (Rectangle r in rects)
            {
                if (r.Contains(x, y))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsLibraryWall(int wall)
        {
            return wall == hallWall || wall == windowWall || wall == forbiddenWall || wall == arenaWall;
        }

        private static void SetTile(int x, int y, int type)
        {
            Tile tile = Main.tile[x, y];
            tile.HasTile = true;
            tile.TileType = (ushort)type;
            tile.Slope = SlopeType.Solid;
            tile.IsHalfBlock = false;
            tile.TileFrameX = 0;
            tile.TileFrameY = 0;
            tile.LiquidAmount = 0;
        }

        private static void SetWall(int x, int y, ushort wall)
        {
            Tile tile = Main.tile[x, y];
            tile.WallType = wall;
        }

        // Wooden platform; may replace a marble column but nothing else.
        private static void SetPlatform(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.TileType == TileID.MarbleColumn)
            {
                SetTile(x, y, TileID.Platforms);
            }
        }
    }
}
