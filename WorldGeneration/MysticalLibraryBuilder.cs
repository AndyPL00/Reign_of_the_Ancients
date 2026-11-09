using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
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
        public Rectangle ThroneRoom;
        public int Stair1X;   // top step of the hall -> archive staircase
        public int Stair2X;   // top step of the archive -> throne room staircase
        public Rectangle Bounds; // everything the builder touches
    }

    // Layout:
    //   Floor 1 (surface): white-and-gold cathedral hall. Red carpet, vaulted arches, stained glass,
    //     rose window, bell towers, balconies with bookcases, reading nooks.
    //   Grand staircase (red carpet, turning to obsidian) down to:
    //   Floor 2: Forbidden Archive. Obsidian and demonite, dim purple light, half-empty shelves,
    //     chains, cobwebs, and a glowing sealed vault behind iron bars.
    //   Second staircase back under the hall to:
    //   Floor 3: Volodar's throne room. Midnight granite and silver, tall arches, pillars with small
    //     balconies, blue banners, and a stepped dais with the throne in front of a glowing arch.
    public static class MysticalLibraryBuilder
    {
        // ---- Rooms ----
        public const int Wall = 3;                  // shell thickness
        public const int HallWidth = 140;
        public const int HallHeight = 30;
        public const int ArchiveWidth = 180;
        public const int ArchiveHeight = 24;
        public const int ThroneRoomWidth = 150;
        public const int ThroneRoomHeight = 44;
        public const int GroundBetweenFloors = 10;  // natural ground between one room's floor and the next room's ceiling

        // ---- Roof and towers ----
        public const int RoofOverhang = 2;
        public const int RoofRun = 3;               // roof rises 1 tile every 3 tiles
        public const int SpireWidth = 4;
        public const int SpireHeight = 14;
        public const int SpireTipHeight = 4;
        public const int TowerWidth = 9;            // bell towers above each door
        public const int TowerHeight = 20;
        public const int TowerCapHeight = 8;
        public const int TowerBandSpacing = 6;      // gold bands up the towers

        // ---- Staircases (45 degrees, 1 tile down per tile across) ----
        public const int StairHeadroom = 8;
        public const int StairThickness = 3;
        public const int StairLining = 2;
        public const int Stair1Offset = 50;         // from hall center toward Side
        public const int Stair2Inset = 20;          // from the archive's entry wall
        public const int StairTorchSpacing = 6;

        // ---- Entrance hall ----
        public const int DoorHeight = 6;
        public const int PlazaMargin = 16;          // shaped ground outside each porch (landing + steps + blend)
        public const int FoundationDepth = 15;
        public const int HallBaySpacing = 20;       // column rhythm
        public const int HallArchHeight = 4;
        public const int GalleryHeight = 12;        // balcony height above the floor
        public const int GalleryBays = 2;           // balconies cover this many bays at each end
        public const int WindowWidth = 4;
        public const int WindowTopGap = 2;          // below the arches
        public const int TallWindowFloorGap = 8;    // tall windows stop this far above the floor
        public const int RoseRadius = 6;

        // ---- Forbidden archive ----
        public const int ArchiveBaySpacing = 20;
        public const int ArchiveArchHeight = 4;
        public const int ArchiveLadderEvery = 3;    // every Nth bay has a gap with ladder platforms
        public const float ArchiveBookcaseChance = 0.75f; // missing shelves: abandoned feel
        public const float CobwebChance = 0.35f;
        public const int ChainMinLength = 2;
        public const int ChainMaxLength = 7;

        // ---- Throne room ----
        public const int ThronePillarSpacing = 18;
        public const int ThroneArchHeight = 6;
        public const int BalconyHalfWidth = 3;      // pillar balconies are 7 wide
        public const int BalconyLow = 10;
        public const int BalconyHigh = 20;
        public const int DaisTiers = 3;
        public const int DaisTierHeight = 2;
        public const int DaisWidth = 30;            // bottom tier, measured from the far wall
        public const int DaisTierStep = 8;          // each tier is this much narrower
        public const int BackdropHalfWidth = 6;
        public const int BackdropHeight = 20;

        private static ushort hallBrick, hallTrim, carpet, forbiddenBrick, forbiddenTrim, throneBrick, throneTrim;
        // ---- Pillars (walk-through: decorative column tiles in front of a stepped wall silhouette) ----
        public const int PillarShaftHalfWidth = 1;  // 3-wide shaft
        public const int PillarFootHalfWidth = 3;   // 7-wide outer step of the base and capital
        public const int PillarStepHalfWidth = 2;   // 5-wide inner step

        // ---- Archive architecture ----
        public const int LowerNicheHalfWidth = 4;   // arched alcoves under the gallery
        public const int UpperNicheHalfWidth = 3;   // arched alcoves above the gallery

        // ---- Throne room extras ----
        public const int BalconyWideHalfWidth = 4;  // pillar balconies are 9 wide
        public const int WallLedgeWidth = 6;        // ledges on the entrance-side wall
        public const int StarBandDepth = 14;        // how far below the arches the night sky reaches
        public const float StarChance = 0.05f;

        // ---- Cathedral exterior ----
        public const int WingWidth = 10;            // entrance porch on each side of the hall (interior)
        public const int WingHeight = 10;
        public const int WingRoofRun = 2;           // lean-to roof rises 1 tile every 2 tiles toward the hall
        public const int RoofShell = 2;             // roof thickness; the rest is a beamed attic
        public const int AtticBeamSpacing = 5;
        public const int TowerUpperWidth = 7;       // narrower upper stage of each tower
        public const int TowerUpperHeight = 8;
        public const int BelfryWidth = 3;
        public const int BelfryHeight = 4;
        public const int StringCourseSpacing = 6;   // gold bands on the hall's outer walls
        public const float WeatherChance = 0.12f;   // exposed outer bricks swapped for rough stone

        // ---- Grounding ----
        public const int LandingWidth = 4;          // paved landing outside each porch door
        public const int PlazaSteps = 3;            // steps down from the landing to the ground
        public const int PlazaStepRun = 2;
        public const float PlantChance = 0.45f;

        // ---- Domes (raised ceilings over key bays) ----
        public const int DomeHeight = 6;
        public const int DomeShell = 2;
        public const int DomeInset = 3;             // dome starts this far in from the bay's pillars

        // ---- Underground hulls ----
        public const int HullBulge = 3;             // extra, uneven thickness on top of Wall
        public const float HullAccentChance = 0.08f;

        private static readonly int[] BalconyHeightsEven = { 9, 23 };
        private static readonly int[] BalconyHeightsOdd = { 16, 30 };
        private static readonly int[] WallLedgeHeights = { 12, 26 };

        private static ushort hallWall, windowWall, roseWall, forbiddenWall, vaultWall, vaultFence, throneWall, backdropWall;
        private static ushort hallPillarWall, archivePillarWall, nicheWall, thronePillarWall, starWall, atticWall;
        private static ushort weatheredStone;
        private static HashSet<int> smoothTypes;

        // ================= Planning =================

        public static LibraryLayout Plan(int centerX, int groundY, int side)
        {
            var l = new LibraryLayout { CenterX = centerX, GroundY = groundY, Side = side };

            l.Hall = new Rectangle(centerX - HallWidth / 2, groundY - HallHeight, HallWidth, HallHeight);

            int archiveFloor = groundY + Wall + GroundBetweenFloors + Wall + ArchiveHeight;
            l.Stair1X = centerX + side * Stair1Offset;
            int stair1End = l.Stair1X + side * (archiveFloor - groundY);
            int archiveX = side > 0 ? stair1End + 1 : stair1End - ArchiveWidth;
            l.Archive = new Rectangle(archiveX, archiveFloor - ArchiveHeight, ArchiveWidth, ArchiveHeight);

            int throneFloor = archiveFloor + Wall + GroundBetweenFloors + Wall + ThroneRoomHeight;
            l.Stair2X = side > 0 ? l.Archive.X + Stair2Inset : l.Archive.Right - 1 - Stair2Inset;
            int stair2End = l.Stair2X - side * (throneFloor - archiveFloor);
            int throneX = side > 0 ? stair2End - ThroneRoomWidth : stair2End + 1;
            l.ThroneRoom = new Rectangle(throneX, throneFloor - ThroneRoomHeight, ThroneRoomWidth, ThroneRoomHeight);

            int left = Math.Min(HallOuterLeft(l) - PlazaMargin, Math.Min(l.Archive.X, l.ThroneRoom.X) - Wall - HullBulge);
            int right = Math.Max(HallOuterRight(l) + PlazaMargin, Math.Max(l.Archive.Right, l.ThroneRoom.Right) + Wall + HullBulge);
            int top = Math.Min(SpireTop(l), TowerTop(l)) - 10;
            int bottom = l.ThroneRoom.Bottom + Wall + HullBulge;
            l.Bounds = new Rectangle(left, top, right - left, bottom - top);
            return l;
        }

        private static int RoofHalfWidth => HallWidth / 2 + Wall + RoofOverhang;
        private static int RoofBase(LibraryLayout l) => l.Hall.Y - Wall - 1;
        private static int RoofPeak(LibraryLayout l) => RoofBase(l) - RoofHalfWidth / RoofRun;
        private static int SpireTop(LibraryLayout l) => RoofPeak(l) - SpireHeight - SpireTipHeight;
        private static int TowerTop(LibraryLayout l) => RoofBase(l) - TowerHeight - TowerUpperHeight - TowerCapHeight;

        // Outer edges of the cathedral including both porches (right edge is exclusive).
        private static int HallOuterLeft(LibraryLayout l) => l.Hall.X - Wall - WingWidth - Wall;
        private static int HallOuterRight(LibraryLayout l) => l.Hall.Right + Wall + WingWidth + Wall;

        private static Rectangle Wing(LibraryLayout l, bool left)
        {
            int x = left ? l.Hall.X - Wall - WingWidth : l.Hall.Right + Wall;
            return new Rectangle(x, l.GroundY - WingHeight, WingWidth, WingHeight);
        }

        // ================= Building =================

        public static void Build(LibraryLayout l)
        {
            LoadTypes();
            var interiors = new[] { l.Hall, l.Archive, l.ThroneRoom };
            var torches = new List<(int x, int y, int item)>();

            PrepareSite(l);

            Box(l.Hall, hallBrick, hallWall);
            Box(Wing(l, true), hallBrick, hallWall);
            Box(Wing(l, false), hallBrick, hallWall);
            Hull(l.Archive, forbiddenBrick, forbiddenTrim, forbiddenWall);
            Hull(l.ThroneRoom, throneBrick, throneTrim, throneWall);
            TrimRows(Wing(l, true), hallTrim, carpet);
            TrimRows(Wing(l, false), hallTrim, carpet);
            TrimRows(l.Hall, hallTrim, carpet);
            TrimRows(l.Archive, forbiddenTrim, forbiddenTrim);
            TrimRows(l.ThroneRoom, throneTrim, throneTrim);
            Roof(l);
            WingRoofs(l);
            Towers(l);
            StringCourses(l);

            List<int> hallPillars = EvenPillars(l.Hall, HallBaySpacing);
            List<int> archivePillars = EvenPillars(l.Archive, ArchiveBaySpacing);
            List<int> thronePillars = ThronePillars(l);
            int[] hallArch = ArchedCeiling(l.Hall, hallPillars, HallArchHeight, hallBrick, hallTrim);
            int[] archiveArch = ArchedCeiling(l.Archive, archivePillars, ArchiveArchHeight, forbiddenBrick, forbiddenTrim);
            int[] throneArch = ArchedCeiling(l.ThroneRoom, thronePillars, ThroneArchHeight, throneBrick, throneTrim);

            // Domes over the key bays: the rose window, the sealed vault, and the throne.
            var hallBays = Bays(l.Hall, hallPillars);
            var archiveBays = Bays(l.Archive, archivePillars);
            var throneBay = Bays(l.ThroneRoom, thronePillars).Find(bay => bay.a < ThroneX(l) && ThroneX(l) < bay.c);
            Dome(l.Hall, hallBays[hallBays.Count / 2], hallArch, hallBrick, hallTrim, hallWall);
            Dome(l.Archive, archiveBays[archiveBays.Count / 2], archiveArch, forbiddenBrick, forbiddenTrim, forbiddenWall);
            Dome(l.ThroneRoom, throneBay, throneArch, throneBrick, throneTrim, throneWall);

            Staircase(l.Stair1X, l.GroundY, l.Archive.Bottom - l.GroundY, l.Side,
                carpet, hallBrick, hallWall, ItemID.WhiteTorch,
                forbiddenTrim, forbiddenBrick, forbiddenWall, ItemID.PurpleTorch, interiors, torches);
            Staircase(l.Stair2X, l.Archive.Bottom, l.ThroneRoom.Bottom - l.Archive.Bottom, -l.Side,
                forbiddenTrim, forbiddenBrick, forbiddenWall, ItemID.PurpleTorch,
                throneTrim, throneBrick, throneWall, ItemID.BlueTorch, interiors, torches);

            HallStructure(l, hallPillars, hallArch);
            ArchiveStructure(l, archivePillars, archiveArch);
            ThroneStructure(l, thronePillars, throneArch);
            WingDoors(l);

            // Ceiling tiles that banners will hang from must stay flat, so smoothing skips them.
            var keepFlat = new HashSet<Point>();
            int bannerSide = PillarFootHalfWidth + 1;
            void KeepHangPoint(Rectangle room, int[] depth, int x)
            {
                if (x >= room.X && x < room.Right)
                {
                    keepFlat.Add(new Point(x, HangY(room, depth, x) - 1));
                }
            }
            foreach (int p in hallPillars) { KeepHangPoint(l.Hall, hallArch, p - bannerSide); KeepHangPoint(l.Hall, hallArch, p + bannerSide); }
            foreach (int p in thronePillars) { KeepHangPoint(l.ThroneRoom, throneArch, p - bannerSide); KeepHangPoint(l.ThroneRoom, throneArch, p + bannerSide); }
            KeepHangPoint(l.ThroneRoom, throneArch, ThroneX(l) - (BackdropHalfWidth + 1));
            KeepHangPoint(l.ThroneRoom, throneArch, ThroneX(l) + (BackdropHalfWidth + 1));

            Weather(l);
            SmoothEdges(l.Bounds, keepFlat);
            WorldGen.RangeFrame(l.Bounds.Left, l.Bounds.Top, l.Bounds.Right, l.Bounds.Bottom);

            PlazaDecor(l);
            WingFurniture(l);
            HallFurniture(l, hallPillars, hallArch, torches);
            ArchiveFurniture(l, archivePillars, archiveArch, torches);
            ThroneFurniture(l, thronePillars, throneArch, torches);

            foreach (var (x, y, item) in torches)
            {
                PlaceSingle(x, y, item);
            }
        }

        private static void LoadTypes()
        {
            hallBrick = (ushort)ModContent.TileType<LibraryBrick>();
            hallTrim = (ushort)ModContent.TileType<LibraryTrim>();
            carpet = (ushort)ModContent.TileType<LibraryCarpet>();
            forbiddenBrick = (ushort)ModContent.TileType<ForbiddenBrick>();
            forbiddenTrim = (ushort)ModContent.TileType<ForbiddenTrim>();
            throneBrick = (ushort)ModContent.TileType<ThroneBrick>();
            throneTrim = (ushort)ModContent.TileType<ThroneTrim>();
            hallWall = (ushort)ModContent.WallType<LibraryWall>();
            windowWall = (ushort)ModContent.WallType<LibraryWindowWall>();
            roseWall = (ushort)ModContent.WallType<LibraryRoseWindowWall>();
            forbiddenWall = (ushort)ModContent.WallType<ForbiddenLibraryWall>();
            vaultWall = (ushort)ModContent.WallType<VaultWall>();
            vaultFence = (ushort)ModContent.WallType<VaultFenceWall>();
            throneWall = (ushort)ModContent.WallType<ThroneRoomWall>();
            backdropWall = (ushort)ModContent.WallType<ThroneBackdropWall>();
            hallPillarWall = (ushort)ModContent.WallType<HallPillarWall>();
            archivePillarWall = (ushort)ModContent.WallType<ArchivePillarWall>();
            nicheWall = (ushort)ModContent.WallType<ArchiveNicheWall>();
            thronePillarWall = (ushort)ModContent.WallType<ThronePillarWall>();
            starWall = (ushort)ModContent.WallType<ThroneStarWall>();
            atticWall = (ushort)ModContent.WallType<LibraryAtticWall>();
            weatheredStone = (ushort)ModContent.TileType<LibraryWeatheredStone>();
            smoothTypes = new HashSet<int>
            {
                hallBrick, hallTrim, carpet, weatheredStone, forbiddenBrick, forbiddenTrim, throneBrick, throneTrim, TileID.MarbleBlock,
            };
        }

        // ================= Terrain and shells =================

        // Clears the sky above the cathedral and shapes the ground around it: a paved marble landing
        // outside each porch, steps down to the surrounding ground, then grass blending outward.
        private static void PrepareSite(LibraryLayout l)
        {
            int outerLeft = HallOuterLeft(l);
            int outerRight = HallOuterRight(l);
            int podiumEnd = LandingWidth + PlazaSteps * PlazaStepRun;
            int bottom = l.GroundY + Wall + FoundationDepth;

            for (int x = outerLeft - PlazaMargin; x < outerRight + PlazaMargin; x++)
            {
                int distance = x < outerLeft ? outerLeft - x : x >= outerRight ? x - outerRight + 1 : 0;
                int surface = l.GroundY;
                if (distance > LandingWidth)
                {
                    surface += Math.Min(PlazaSteps, (distance - LandingWidth - 1) / PlazaStepRun + 1);
                }

                for (int y = l.Bounds.Top; y < surface; y++)
                {
                    Main.tile[x, y].ClearEverything();
                }

                for (int y = surface; y < bottom; y++)
                {
                    Tile tile = Main.tile[x, y];
                    tile.LiquidAmount = 0;
                    bool podium = distance > 0 && distance <= podiumEnd && y <= l.GroundY + PlazaSteps;
                    if (podium)
                    {
                        SetTile(x, y, TileID.MarbleBlock);
                    }
                    else if (!tile.HasTile || !Main.tileSolid[tile.TileType])
                    {
                        SetTile(x, y, y == surface && distance > podiumEnd ? TileID.Grass : TileID.Dirt);
                    }
                    else if (y == surface && distance > podiumEnd && tile.TileType == TileID.Dirt)
                    {
                        tile.TileType = TileID.Grass;
                    }
                }
            }
        }

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

        // Underground shell: at least Wall thick, with rounded corners and an uneven outer surface
        // (thickness drifts by up to HullBulge), speckled with trim for texture.
        private static void Hull(Rectangle interior, ushort brick, ushort accent, ushort wall)
        {
            float phaseA = WorldGen.genRand.NextFloat(MathHelper.TwoPi);
            float phaseB = WorldGen.genRand.NextFloat(MathHelper.TwoPi);
            int reach = Wall + HullBulge;

            for (int x = interior.X - reach; x < interior.Right + reach; x++)
            {
                for (int y = interior.Y - reach; y < interior.Bottom + reach; y++)
                {
                    int dx = x < interior.X ? interior.X - x : x >= interior.Right ? x - interior.Right + 1 : 0;
                    int dy = y < interior.Y ? interior.Y - y : y >= interior.Bottom ? y - interior.Bottom + 1 : 0;

                    if (dx == 0 && dy == 0)
                    {
                        Main.tile[x, y].ClearEverything();
                        SetWall(x, y, wall);
                        continue;
                    }

                    float noise = 0.5f + 0.3f * (float)Math.Sin(x * 0.11f + phaseA) + 0.2f * (float)Math.Sin(y * 0.23f + phaseB);
                    float thickness = Wall + HullBulge * MathHelper.Clamp(noise, 0f, 1f);
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (distance <= thickness)
                    {
                        Main.tile[x, y].ClearEverything();
                        SetWall(x, y, wall);
                        bool outerLayer = distance > Wall;
                        SetTile(x, y, outerLayer && WorldGen.genRand.NextFloat() < HullAccentChance ? accent : brick);
                    }
                }
            }
        }

        // Trim along the ceiling and floor; the floor surface inside the room uses `floorTop`.
        private static void TrimRows(Rectangle room, ushort trim, ushort floorTop)
        {
            for (int x = room.X - Wall; x < room.Right + Wall; x++)
            {
                SetTile(x, room.Y - 1, trim);
                bool inside = x >= room.X && x < room.Right;
                SetTile(x, room.Bottom, inside ? floorTop : trim);
                SetTile(x, room.Bottom + 1, trim);
            }
        }

        // Gable roof: a gold-ridged shell with a hollow, beamed attic inside, topped by the spire.
        private static void Roof(LibraryLayout l)
        {
            int baseY = RoofBase(l);
            int halfWidth = RoofHalfWidth;
            for (int dx = -halfWidth; dx <= halfWidth; dx++)
            {
                int x = l.CenterX + dx;
                int top = baseY - (halfWidth - Math.Abs(dx)) / RoofRun;
                for (int y = top; y <= baseY; y++)
                {
                    if (y == top || y == baseY)
                    {
                        SetTile(x, y, hallTrim); // gold ridge and eaves
                    }
                    else if (y < top + RoofShell)
                    {
                        SetTile(x, y, hallBrick);
                    }
                    else
                    {
                        SetWall(x, y, atticWall);
                        if (dx % AtticBeamSpacing == 0)
                        {
                            SetTile(x, y, TileID.WoodenBeam);
                        }
                    }
                }
            }

            int peakY = RoofPeak(l);
            for (int x = l.CenterX - SpireWidth / 2; x < l.CenterX + SpireWidth / 2; x++)
            {
                for (int y = peakY - SpireHeight; y <= peakY; y++)
                {
                    bool band = (peakY - y) % TowerBandSpacing == 0;
                    SetTile(x, y, band ? hallTrim : hallBrick);
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

        // Lean-to roofs over the porches, rising toward the main hall.
        private static void WingRoofs(LibraryLayout l)
        {
            foreach (bool left in new[] { true, false })
            {
                Rectangle wing = Wing(l, left);
                int baseY = wing.Y - Wall - 1;
                int outerEdge = left ? wing.X - Wall - 1 : wing.Right + Wall; // one tile of overhang
                int innerEdge = left ? l.Hall.X - Wall - 1 : l.Hall.Right + Wall;
                int step = left ? 1 : -1;
                for (int x = outerEdge, dist = 0; x != innerEdge + step; x += step, dist++)
                {
                    int top = baseY - dist / WingRoofRun;
                    for (int y = top; y <= baseY; y++)
                    {
                        SetTile(x, y, y == top || y == baseY ? hallTrim : hallBrick);
                    }
                }
            }
        }

        // Bell towers over each door: a tall lower stage with an open belfry, a narrower upper stage,
        // and a pointed gold-tipped cap.
        private static void Towers(LibraryLayout l)
        {
            int baseY = RoofBase(l);
            foreach (int wallCenter in new[] { l.Hall.X - Wall / 2 - 1, l.Hall.Right + Wall / 2 })
            {
                TowerStage(wallCenter, baseY, TowerWidth, TowerHeight);

                // Belfry opening near the top of the lower stage.
                int belfryTop = baseY - TowerHeight + 2;
                for (int x = wallCenter - BelfryWidth / 2; x <= wallCenter + BelfryWidth / 2; x++)
                {
                    for (int y = belfryTop; y < belfryTop + BelfryHeight; y++)
                    {
                        Tile tile = Main.tile[x, y];
                        tile.HasTile = false;
                        SetWall(x, y, atticWall);
                    }
                }

                int upperBase = baseY - TowerHeight - 1;
                TowerStage(wallCenter, upperBase, TowerUpperWidth, TowerUpperHeight);

                int capBase = upperBase - TowerUpperHeight - 1;
                int left = wallCenter - TowerUpperWidth / 2;
                for (int row = 0; row < TowerCapHeight; row++)
                {
                    int inset = row * (TowerUpperWidth / 2 + 1) / TowerCapHeight;
                    for (int x = left + inset; x < left + TowerUpperWidth - inset; x++)
                    {
                        SetTile(x, capBase - row, row >= TowerCapHeight - 2 ? hallTrim : hallBrick);
                    }
                }
            }
        }

        private static void TowerStage(int center, int baseY, int width, int height)
        {
            int left = center - width / 2;
            for (int x = left; x < left + width; x++)
            {
                for (int y = baseY - height; y <= baseY; y++)
                {
                    bool band = (baseY - y) % TowerBandSpacing == 0;
                    SetTile(x, y, band ? hallTrim : hallBrick);
                }
            }
        }

        // Gold bands every few rows on the hall's outer walls, between the porch roofs and the eaves.
        private static void StringCourses(LibraryLayout l)
        {
            int roofBase = RoofBase(l);
            foreach (int x in new[] { l.Hall.X - Wall, l.Hall.Right + Wall - 1 })
            {
                for (int y = roofBase + 1; y < l.GroundY; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && tile.TileType == hallBrick && (l.GroundY - y) % StringCourseSpacing == 0)
                    {
                        tile.TileType = hallTrim;
                    }
                }
            }
        }

        // Porch outer doors with gold lintels.
        private static void WingDoors(LibraryLayout l)
        {
            foreach (bool left in new[] { true, false })
            {
                Rectangle wing = Wing(l, left);
                int doorX = left ? wing.X - Wall : wing.Right;
                for (int x = doorX; x < doorX + Wall; x++)
                {
                    for (int y = l.GroundY - DoorHeight; y < l.GroundY; y++)
                    {
                        Tile tile = Main.tile[x, y];
                        tile.HasTile = false;
                    }
                    SetTile(x, l.GroundY - DoorHeight - 1, hallTrim);
                    SetTile(x, l.GroundY, hallTrim);
                }
            }
        }

        // Swaps some exposed outer bricks of the cathedral for rough stone so it looks weathered.
        private static void Weather(LibraryLayout l)
        {
            for (int x = HallOuterLeft(l) - 2; x < HallOuterRight(l) + 2; x++)
            {
                for (int y = l.Bounds.Top; y < l.GroundY; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && tile.TileType == hallBrick && TouchesSky(x, y) && WorldGen.genRand.NextFloat() < WeatherChance)
                    {
                        tile.TileType = weatheredStone;
                    }
                }
            }
        }

        private static bool TouchesSky(int x, int y)
        {
            return IsSky(x - 1, y) || IsSky(x + 1, y) || IsSky(x, y - 1) || IsSky(x, y + 1);
        }

        private static bool IsSky(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return !tile.HasTile && tile.WallType == 0;
        }

        // Slopes every exposed corner of the library's bricks: smooth roof lines, rounded arches,
        // ramps on the staircases and softer hull edges.
        private static void SmoothEdges(Rectangle bounds, HashSet<Point> keepFlat)
        {
            for (int x = bounds.Left + 1; x < bounds.Right - 1; x++)
            {
                for (int y = bounds.Top + 1; y < bounds.Bottom - 1; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile || !smoothTypes.Contains(tile.TileType) || tile.Slope != SlopeType.Solid || tile.IsHalfBlock
                        || keepFlat.Contains(new Point(x, y)))
                    {
                        continue;
                    }

                    bool up = IsSolid(x, y - 1);
                    bool down = IsSolid(x, y + 1);
                    bool left = IsSolid(x - 1, y);
                    bool right = IsSolid(x + 1, y);

                    if (!up && down)
                    {
                        if (!left && right)
                        {
                            tile.Slope = SlopeType.SlopeDownRight; // bottom and right solid
                        }
                        else if (left && !right)
                        {
                            tile.Slope = SlopeType.SlopeDownLeft;  // bottom and left solid
                        }
                    }
                    else if (up && !down)
                    {
                        if (!left && right)
                        {
                            tile.Slope = SlopeType.SlopeUpRight;   // top and right solid
                        }
                        else if (left && !right)
                        {
                            tile.Slope = SlopeType.SlopeUpLeft;    // top and left solid
                        }
                    }
                }
            }
        }

        private static bool IsSolid(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
        }

        // Lamp posts on the landings and plants on the grass around the building.
        private static void PlazaDecor(LibraryLayout l)
        {
            Place(HallOuterLeft(l) - LandingWidth, l.GroundY - 1, ItemID.LampPost);
            Place(HallOuterRight(l) + LandingWidth - 1, l.GroundY - 1, ItemID.LampPost);

            int podiumEnd = LandingWidth + PlazaSteps * PlazaStepRun;
            for (int x = HallOuterLeft(l) - PlazaMargin; x < HallOuterRight(l) + PlazaMargin; x++)
            {
                int distance = x < HallOuterLeft(l) ? HallOuterLeft(l) - x : x >= HallOuterRight(l) ? x - HallOuterRight(l) + 1 : 0;
                if (distance <= podiumEnd)
                {
                    continue;
                }
                int y = l.GroundY + PlazaSteps;
                if (Main.tile[x, y].TileType == TileID.Grass && !Main.tile[x, y - 1].HasTile && WorldGen.genRand.NextFloat() < PlantChance)
                {
                    WorldGen.PlaceTile(x, y - 1, TileID.Plants, true, false, -1, WorldGen.genRand.Next(0, 45));
                }
            }
        }

        // Porches: carpet floor (from TrimRows), a banner and two lamps.
        private static void WingFurniture(LibraryLayout l)
        {
            foreach (bool left in new[] { true, false })
            {
                Rectangle wing = Wing(l, left);
                int center = wing.X + wing.Width / 2;
                Place(center, wing.Y, ItemID.RedBanner);
                Place(wing.X + 1, l.GroundY - 1, ItemID.MarbleLamp);
                Place(wing.Right - 2, l.GroundY - 1, ItemID.MarbleLamp);
            }
        }

        // Raises the ceiling of one bay into a round dome (with its own brick shell), opening up
        // above the arch. Updates `depth` so things hung at HangY hang from the dome.
        private static void Dome(Rectangle room, (int a, int c, int center) bay, int[] depth, ushort brick, ushort trim, ushort wall)
        {
            if (bay.c <= bay.a)
            {
                return;
            }

            float half = (bay.c - bay.a) / 2f - DomeInset;
            float mid = (bay.a + bay.c) / 2f;
            for (int x = bay.a + 1; x < bay.c; x++)
            {
                float d = Math.Abs(x - mid) / half;
                if (d >= 1f)
                {
                    continue;
                }
                int height = (int)Math.Round(DomeHeight * Math.Sqrt(1f - d * d));
                if (height <= 0)
                {
                    continue;
                }

                int oldDepth = Math.Max(0, depth[x - room.X]);
                for (int y = room.Y - height; y < room.Y + oldDepth; y++)
                {
                    Tile tile = Main.tile[x, y];
                    tile.HasTile = false;
                    SetWall(x, y, wall);
                }
                for (int y = room.Y - height - DomeShell; y < room.Y - height; y++)
                {
                    SetTile(x, y, y == room.Y - height - 1 ? trim : brick);
                    SetWall(x, y, wall);
                }
                depth[x - room.X] = -height;
            }
        }

        // ================= Arches and pillars =================

        private static List<int> EvenPillars(Rectangle room, int spacing)
        {
            var pillars = new List<int>();
            for (int x = room.X + spacing; x < room.Right - spacing / 2; x += spacing)
            {
                pillars.Add(x);
            }
            return pillars;
        }

        // Throne room pillars are spaced from the far wall (where the dais is), so the throne
        // always sits in a clear bay.
        private static List<int> ThronePillars(LibraryLayout l)
        {
            var pillars = new List<int>();
            for (int offset = ThronePillarSpacing; offset < ThroneRoomWidth - ThronePillarSpacing / 2; offset += ThronePillarSpacing)
            {
                pillars.Add(FromFarWall(l, offset));
            }
            pillars.Sort();
            return pillars;
        }

        // Fills the ceiling between pillars to form round arches. Returns, per column, how far the
        // arch reaches down (things hang from room.Y + depth).
        private static int[] ArchedCeiling(Rectangle room, List<int> pillars, int archHeight, ushort brick, ushort trim)
        {
            int[] depth = new int[room.Width];
            var bounds = new List<int> { room.X - 1 };
            bounds.AddRange(pillars);
            bounds.Add(room.Right);

            for (int b = 0; b < bounds.Count - 1; b++)
            {
                int a = bounds[b];
                int c = bounds[b + 1];
                float half = (c - a) / 2f;
                float mid = (a + c) / 2f;
                for (int x = a + 1; x < c; x++)
                {
                    float d = Math.Abs(x - mid) / half;
                    depth[x - room.X] = (int)Math.Round(archHeight * (1 - Math.Sqrt(Math.Max(0f, 1f - d * d))));
                }
            }
            // Flat under the whole pillar shaft so the column meets the arch cleanly.
            foreach (int p in pillars)
            {
                for (int x = p - PillarShaftHalfWidth; x <= p + PillarShaftHalfWidth; x++)
                {
                    if (x >= room.X && x < room.Right)
                    {
                        depth[x - room.X] = archHeight;
                    }
                }
            }

            for (int x = room.X; x < room.Right; x++)
            {
                int dep = depth[x - room.X];
                for (int y = room.Y; y < room.Y + dep; y++)
                {
                    SetTile(x, y, y == room.Y + dep - 1 ? trim : brick);
                }
            }
            return depth;
        }

        private static int HangY(Rectangle room, int[] depth, int x) => room.Y + depth[x - room.X];

        // Thick walk-through pillar: a 3-wide shaft of decorative column tiles in front of a wall
        // silhouette that steps out at the base and the capital (7 wide, then 5, then the 3-wide shaft).
        private static void ThickPillar(Rectangle room, int archHeight, int p, int columnTile, ushort pillarWall, byte paint = 0)
        {
            int top = room.Y + archHeight;
            int floor = room.Bottom;
            for (int y = top; y < floor; y++)
            {
                int halfWidth = PillarShaftHalfWidth;
                if (y == top || y == floor - 1)
                {
                    halfWidth = PillarFootHalfWidth;
                }
                else if (y == top + 1 || y == floor - 2)
                {
                    halfWidth = PillarStepHalfWidth;
                }

                for (int x = p - halfWidth; x <= p + halfWidth; x++)
                {
                    if (x >= room.X && x < room.Right)
                    {
                        SetWall(x, y, pillarWall);
                    }
                }
                for (int x = p - PillarShaftHalfWidth; x <= p + PillarShaftHalfWidth; x++)
                {
                    SetTile(x, y, columnTile);
                    Tile tile = Main.tile[x, y];
                    tile.TileColor = paint;
                }
            }
        }

        // Arched alcove in the back wall: a pointed shape framed by `frameWall`, filled with `fillWall`.
        private static void Niche(int cx, int top, int bottom, int halfWidth, ushort frameWall, ushort fillWall)
        {
            NicheShape(cx, top - 1, bottom, halfWidth + 1, frameWall);
            NicheShape(cx, top, bottom, halfWidth, fillWall);
        }

        private static void NicheShape(int cx, int top, int bottom, int halfWidth, ushort wall)
        {
            for (int y = top; y <= bottom; y++)
            {
                int row = y - top;
                int hw = row == 0 ? Math.Max(0, halfWidth - 2) : row == 1 ? halfWidth - 1 : halfWidth;
                for (int x = cx - hw; x <= cx + hw; x++)
                {
                    SetWall(x, y, wall);
                }
            }
        }

        // Bay boundaries: the walls plus every pillar.
        private static List<(int a, int c, int center)> Bays(Rectangle room, List<int> pillars)
        {
            var bays = new List<(int, int, int)>();
            int prev = room.X - 1;
            foreach (int p in pillars)
            {
                bays.Add((prev, p, (prev + p) / 2));
                prev = p;
            }
            bays.Add((prev, room.Right, (prev + room.Right) / 2));
            return bays;
        }

        // ================= Staircases =================

        // 45-degree staircase carved through the ground, lined with brick. The top half uses the upper
        // floor's materials and the bottom half the lower floor's, so the theme shifts as you descend.
        private static void Staircase(int startX, int startY, int depth, int dir,
            ushort upperTop, ushort upperBrick, ushort upperWall, int upperTorch,
            ushort lowerTop, ushort lowerBrick, ushort lowerWall, int lowerTorch,
            Rectangle[] interiors, List<(int, int, int)> torches)
        {
            for (int i = 0; i <= depth; i++)
            {
                int x = startX + dir * i;
                int step = startY + i;
                bool lowerHalf = i > depth / 2;
                ushort top = lowerHalf ? lowerTop : upperTop;
                ushort brick = lowerHalf ? lowerBrick : upperBrick;
                ushort wall = lowerHalf ? lowerWall : upperWall;

                for (int y = step - StairHeadroom - StairLining; y < step - StairHeadroom; y++)
                {
                    if (!InsideAny(interiors, x, y))
                    {
                        SetTile(x, y, y == step - StairHeadroom - 1 ? top == carpet ? hallTrim : top : brick);
                        SetWall(x, y, wall);
                    }
                }
                for (int y = step - StairHeadroom; y < step; y++)
                {
                    Tile tile = Main.tile[x, y];
                    tile.HasTile = false;
                    tile.LiquidAmount = 0;
                    if (MysticalLibrary.FloorOfWall(tile.WallType) == MysticalLibrary.NoFloor)
                    {
                        SetWall(x, y, wall);
                    }
                }
                for (int y = step; y < step + StairThickness; y++)
                {
                    if (!InsideAny(interiors, x, y))
                    {
                        SetTile(x, y, y == step ? top : brick);
                        SetWall(x, y, wall);
                    }
                }

                if (i % StairTorchSpacing == StairTorchSpacing / 2)
                {
                    torches.Add((x, step - 4, lowerHalf ? lowerTorch : upperTorch));
                }
            }
        }

        // ================= Floor 1: entrance hall =================

        private static void HallStructure(LibraryLayout l, List<int> pillars, int[] arch)
        {
            Rectangle hall = l.Hall;
            int floor = l.GroundY;
            int galleryY = floor - GalleryHeight;
            var bays = Bays(hall, pillars);

            // Doors on both sides, framed in gold.
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
                    SetTile(x, floor, hallTrim);
                }
            }

            // Windows: short ones above the balconies, tall ones in the open bays, a rose window in the middle.
            int windowTop = hall.Y + HallArchHeight + WindowTopGap;
            for (int b = 0; b < bays.Count; b++)
            {
                int center = bays[b].center;
                if (b == bays.Count / 2)
                {
                    RoseWindow(center, windowTop + RoseRadius);
                    continue;
                }
                int bottom = IsGalleryBay(b, bays.Count) ? galleryY - 2 : floor - TallWindowFloorGap;
                LancetWindow(center - WindowWidth / 2, windowTop, bottom, windowWall);
            }

            foreach (int p in pillars)
            {
                if (!NearStairMouth(l, p))
                {
                    ThickPillar(hall, HallArchHeight, p, TileID.MarbleColumn, hallPillarWall);
                }
            }

            // Balconies over the end bays, each reached by ladder platforms.
            int galleryLength = GalleryBays * HallBaySpacing;
            int marblePlatform = Style(ItemID.MarblePlatform);
            for (int x = hall.X; x < hall.X + galleryLength; x++)
            {
                SetPlatform(x, galleryY, marblePlatform);
            }
            for (int x = hall.Right - galleryLength; x < hall.Right; x++)
            {
                SetPlatform(x, galleryY, marblePlatform);
            }
            for (int k = 1; k <= 4; k++)
            {
                for (int h = 4; h < GalleryHeight; h += 4)
                {
                    SetPlatform(hall.X + galleryLength + k, floor - h, marblePlatform);
                    SetPlatform(hall.Right - galleryLength - 1 - k, floor - h, marblePlatform);
                }
            }
        }

        private static bool IsGalleryBay(int bay, int bayCount) => bay < GalleryBays || bay >= bayCount - GalleryBays;

        // Tall narrow window with a pointed top.
        private static void LancetWindow(int left, int top, int bottom, ushort wall)
        {
            for (int y = top; y <= bottom; y++)
            {
                for (int k = 0; k < WindowWidth; k++)
                {
                    if (y == top && (k == 0 || k == WindowWidth - 1))
                    {
                        continue;
                    }
                    SetWall(left + k, y, wall);
                }
            }
        }

        // Round window: purple glass with blue spokes and a blue center.
        private static void RoseWindow(int cx, int cy)
        {
            for (int dx = -RoseRadius; dx <= RoseRadius; dx++)
            {
                for (int dy = -RoseRadius; dy <= RoseRadius; dy++)
                {
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist > RoseRadius + 0.3f)
                    {
                        continue;
                    }
                    bool center = dist <= RoseRadius * 0.35f;
                    bool spoke = dx == 0 || dy == 0 || Math.Abs(dx) == Math.Abs(dy);
                    SetWall(cx + dx, cy + dy, center || spoke ? windowWall : roseWall);
                }
            }
        }

        private static void HallFurniture(LibraryLayout l, List<int> pillars, int[] arch, List<(int, int, int)> torches)
        {
            Rectangle hall = l.Hall;
            int floor = l.GroundY;
            int galleryY = floor - GalleryHeight;
            var bays = Bays(hall, pillars);

            for (int b = 0; b < bays.Count; b++)
            {
                var (a, c, center) = bays[b];

                // Gold chandelier at the top of every arch.
                Place(center, HangY(hall, arch, center), ItemID.MarbleChandelier);

                if (b == bays.Count / 2)
                {
                    // Centerpiece under the rose window: crystal ball on a table, flanked by candelabras and clocks.
                    Place(center, floor - 1, ItemID.MarbleTable);
                    Place(center, floor - 3, ItemID.CrystalBall);
                    Place(center - 5, floor - 1, ItemID.MarbleCandelabra);
                    Place(center + 4, floor - 1, ItemID.MarbleCandelabra);
                    Place(center - 8, floor - 1, ItemID.MarbleClock);
                    Place(center + 7, floor - 1, ItemID.MarbleClock);
                }
                else if (IsGalleryBay(b, bays.Count))
                {
                    // Shelves under the balcony...
                    for (int x = a + 4; x < c - 2; x += 4)
                    {
                        if (!NearStairMouth(l, x))
                        {
                            Place(x, floor - 1, ItemID.MarbleBookcase);
                        }
                    }
                    // ...and on it, with lamps and loose books.
                    Place(a + 5, galleryY - 1, ItemID.MarbleBookcase);
                    Place(a + 10, galleryY - 1, ItemID.MarbleLamp);
                    Place(a + 15, galleryY - 1, ItemID.MarbleBookcase);
                    PlaceBooks(a + 8, galleryY - 1);
                    PlaceBooks(a + 12, galleryY - 1);
                }
                else
                {
                    // Reading nook: table with a candle, two chairs, lamps and bookcases.
                    Place(center, floor - 1, ItemID.MarbleTable);
                    PlaceSingle(center, floor - 3, ItemID.MarbleCandle);
                    Place(center - 2, floor - 1, ItemID.MarbleChair, 1);
                    Place(center + 2, floor - 1, ItemID.MarbleChair, -1);
                    Place(center - 5, floor - 1, ItemID.MarbleLamp);
                    Place(center + 5, floor - 1, ItemID.MarbleLamp);
                    Place(a + 3, floor - 1, ItemID.MarbleBookcase);
                    Place(c - 3, floor - 1, ItemID.MarbleBookcase);
                }
            }

            // Red banners and white torches either side of each pillar.
            int side = PillarFootHalfWidth + 1;
            foreach (int p in pillars)
            {
                Place(p - side, HangY(hall, arch, p - side), ItemID.RedBanner);
                Place(p + side, HangY(hall, arch, p + side), ItemID.RedBanner);
                if (!NearStairMouth(l, p))
                {
                    torches.Add((p - side, floor - 7, ItemID.WhiteTorch));
                    torches.Add((p + side, floor - 7, ItemID.WhiteTorch));
                }
            }
        }

        // ================= Floor 2: forbidden archive =================

        private static void ArchiveStructure(LibraryLayout l, List<int> pillars, int[] arch)
        {
            Rectangle archive = l.Archive;
            int floor = archive.Bottom;
            int galleryY = floor - GalleryHeight;
            int platform = Style(ItemID.ObsidianPlatform);
            var bays = Bays(archive, pillars);
            int vaultBay = bays.Count / 2;

            // Demonite wall bands: a dado along the floor and a beam under the gallery.
            for (int x = archive.X; x < archive.Right; x++)
            {
                SetWall(x, floor - 1, archivePillarWall);
                SetWall(x, floor - 2, archivePillarWall);
                SetWall(x, galleryY + 1, archivePillarWall);
            }

            // Arched alcoves for the shelves: one under the gallery, one above it, in every bay.
            for (int b = 0; b < bays.Count; b++)
            {
                int center = bays[b].center;
                if (b != vaultBay)
                {
                    Niche(center, galleryY + 4, floor - 3, LowerNicheHalfWidth, archivePillarWall, nicheWall);
                }
                Niche(center, archive.Y + 3, galleryY - 1, UpperNicheHalfWidth, archivePillarWall, nicheWall);
            }

            // Sealed vault in the middle bay: glowing purple backdrop framed by iron bars.
            var vault = bays[vaultBay];
            for (int x = vault.a + 4; x <= vault.c - 4; x++)
            {
                for (int y = galleryY + 2; y < floor; y++)
                {
                    bool frame = x == vault.a + 4 || x == vault.c - 4 || y == galleryY + 2;
                    SetWall(x, y, frame ? vaultFence : vaultWall);
                }
            }

            foreach (int p in pillars)
            {
                ThickPillar(archive, ArchiveArchHeight, p, TileID.GraniteColumn, archivePillarWall, PaintID.DeepPurplePaint);
            }

            // Gallery along the whole room; ladder bays get a gap with platforms up from the floor.
            for (int b = 0; b < bays.Count; b++)
            {
                var (a, c, center) = bays[b];
                bool ladderBay = IsArchiveLadderBay(b, vaultBay);
                for (int x = Math.Max(a, archive.X); x < Math.Min(c + 1, archive.Right); x++)
                {
                    if (ladderBay && Math.Abs(x - center) <= 3)
                    {
                        for (int h = 4; h < GalleryHeight; h += 4)
                        {
                            SetPlatform(x, floor - h, platform);
                        }
                    }
                    else
                    {
                        SetPlatform(x, galleryY, platform);
                    }
                }
            }

            // Chains hanging from the arches, cobwebs gathered in the arch corners.
            foreach (var (a, c, center) in bays)
            {
                foreach (int x in new[] { center - 6, center + 6 })
                {
                    if (x < archive.X || x >= archive.Right)
                    {
                        continue;
                    }
                    int top = HangY(archive, arch, x);
                    int length = WorldGen.genRand.Next(ChainMinLength, ChainMaxLength + 1);
                    for (int y = top; y < top + length; y++)
                    {
                        if (!Main.tile[x, y].HasTile)
                        {
                            SetTile(x, y, TileID.Chain);
                        }
                    }
                }
            }
            for (int x = archive.X; x < archive.Right; x++)
            {
                int top = HangY(archive, arch, x);
                for (int y = top; y < top + 2; y++)
                {
                    if (!Main.tile[x, y].HasTile && arch[x - archive.X] > 0 && WorldGen.genRand.NextFloat() < CobwebChance)
                    {
                        SetTile(x, y, TileID.Cobweb);
                    }
                }
            }
        }

        private static bool IsArchiveLadderBay(int bay, int vaultBay) => bay % ArchiveLadderEvery == 1 && bay != vaultBay;

        private static void ArchiveFurniture(LibraryLayout l, List<int> pillars, int[] arch, List<(int, int, int)> torches)
        {
            Rectangle archive = l.Archive;
            int floor = archive.Bottom;
            int galleryY = floor - GalleryHeight;
            var bays = Bays(archive, pillars);
            int vaultBay = bays.Count / 2;
            int outside = LowerNicheHalfWidth + 3; // bookcases flanking each lower alcove

            for (int b = 0; b < bays.Count; b++)
            {
                var (a, c, center) = bays[b];

                // Dim lighting: lanterns, with an obsidian chandelier every other bay.
                Place(center, HangY(archive, arch, center), b % 2 == 0 ? ItemID.ObsidianChandelier : ItemID.ObsidianLantern);

                if (b == vaultBay)
                {
                    // Inside the vault: a bewitching table with a crystal ball, guarded by shelves.
                    Place(center, floor - 1, ItemID.BewitchingTable);
                    Place(center, floor - 3, ItemID.CrystalBall);
                    Place(center - 4, floor - 1, ItemID.ObsidianBookcase);
                    Place(center + 4, floor - 1, ItemID.ObsidianBookcase);
                }
                else
                {
                    if (b % ArchiveLadderEvery == 2)
                    {
                        // Abandoned study in the alcove.
                        Place(center, floor - 1, ItemID.ObsidianTable);
                        PlaceSingle(center, floor - 3, ItemID.WaterCandle);
                        Place(center - 3, floor - 1, ItemID.ObsidianChair, 1);
                        Place(center + 3, floor - 1, ItemID.ObsidianLamp);
                    }
                    else if (!IsArchiveLadderBay(b, vaultBay))
                    {
                        // Two shelves in the alcove (some missing).
                        if (WorldGen.genRand.NextFloat() < ArchiveBookcaseChance)
                        {
                            Place(center - 2, floor - 1, ItemID.ObsidianBookcase);
                        }
                        if (WorldGen.genRand.NextFloat() < ArchiveBookcaseChance)
                        {
                            Place(center + 2, floor - 1, ItemID.ObsidianBookcase);
                        }
                    }

                    // Shelves standing either side of the alcove.
                    Place(center - outside, floor - 1, ItemID.ObsidianBookcase);
                    Place(center + outside, floor - 1, ItemID.ObsidianBookcase);
                }

                // Upper alcove: a shelf framed by loose books, lamps either side.
                if (WorldGen.genRand.NextFloat() < ArchiveBookcaseChance)
                {
                    Place(center, galleryY - 1, ItemID.ObsidianBookcase);
                }
                PlaceBooks(center - 2, galleryY - 1);
                PlaceBooks(center + 2, galleryY - 1);
                Place(center - UpperNicheHalfWidth - 2, galleryY - 1, ItemID.ObsidianLamp);
                Place(center + UpperNicheHalfWidth + 2, galleryY - 1, ItemID.ObsidianLamp);
            }

            int side = PillarFootHalfWidth + 1;
            foreach (int p in pillars)
            {
                torches.Add((p - side, floor - 6, ItemID.PurpleTorch));
                torches.Add((p + side, floor - 6, ItemID.PurpleTorch));
                torches.Add((p - side, galleryY - 5, ItemID.PurpleTorch));
                torches.Add((p + side, galleryY - 5, ItemID.PurpleTorch));
            }
        }

        // ================= Floor 3: throne room =================

        private static void ThroneStructure(LibraryLayout l, List<int> pillars, int[] arch)
        {
            Rectangle room = l.ThroneRoom;
            int floor = room.Bottom;
            int platform = Style(ItemID.GranitePlatform);

            // Night sky: the upper part of the room is painted deep blue and scattered with glowing stars.
            int skyBottom = room.Y + ThroneArchHeight + StarBandDepth;
            for (int x = room.X; x < room.Right; x++)
            {
                int top = HangY(room, arch, x);
                for (int y = top; y < skyBottom; y++)
                {
                    Tile tile = Main.tile[x, y];
                    tile.WallColor = PaintID.DeepBluePaint;
                    // Stars thin out toward the bottom of the band.
                    float density = StarChance * (1f - (y - top) / (float)(skyBottom - top + 1));
                    if (WorldGen.genRand.NextFloat() < density)
                    {
                        tile.WallType = starWall;
                        tile.WallColor = 0;
                    }
                }
            }

            foreach (int p in pillars)
            {
                ThickPillar(room, ThroneArchHeight, p, TileID.GraniteColumn, thronePillarWall);
            }

            // Stepped dais against the far wall, silver-edged.
            for (int t = 0; t < DaisTiers; t++)
            {
                int width = DaisWidth - t * DaisTierStep;
                int top = floor - (t + 1) * DaisTierHeight;
                for (int offset = 0; offset < width; offset++)
                {
                    int x = FromFarWall(l, offset);
                    for (int y = top; y < top + DaisTierHeight; y++)
                    {
                        SetTile(x, y, y == top ? throneTrim : throneBrick);
                    }
                }
            }

            // Glowing midnight-blue arch behind the throne.
            int throneX = ThroneX(l);
            int daisTop = DaisTop(l);
            for (int row = 0; row < BackdropHeight; row++)
            {
                int y = daisTop - BackdropHeight + row;
                int halfWidth = Math.Min(BackdropHalfWidth, 1 + row);
                for (int x = throneX - halfWidth; x <= throneX + halfWidth; x++)
                {
                    SetWall(x, y, backdropWall);
                    Tile tile = Main.tile[x, y];
                    tile.WallColor = 0;
                }
            }

            // Balconies on every pillar, two per pillar at staggered heights so they can be climbed.
            for (int i = 0; i < pillars.Count; i++)
            {
                int p = pillars[i];
                if (DistanceFromFarWall(l, p) < DaisWidth + 2)
                {
                    continue;
                }
                foreach (int height in i % 2 == 0 ? BalconyHeightsEven : BalconyHeightsOdd)
                {
                    for (int x = p - BalconyWideHalfWidth; x <= p + BalconyWideHalfWidth; x++)
                    {
                        SetPlatform(x, floor - height, platform);
                    }
                }
            }

            // Ledges on the entrance-side wall.
            foreach (int height in WallLedgeHeights)
            {
                for (int offset = 0; offset < WallLedgeWidth; offset++)
                {
                    SetPlatform(FromFarWall(l, ThroneRoomWidth - 1 - offset), floor - height, platform);
                }
            }
        }

        private static void ThroneFurniture(LibraryLayout l, List<int> pillars, int[] arch, List<(int, int, int)> torches)
        {
            Rectangle room = l.ThroneRoom;
            int floor = room.Bottom;
            int throneX = ThroneX(l);
            int daisTop = DaisTop(l);
            var bays = Bays(room, pillars);

            Place(throneX, daisTop - 1, ItemID.Throne);
            Place(throneX - 5, daisTop - 1, ItemID.GraniteCandelabra);
            Place(throneX + 4, daisTop - 1, ItemID.GraniteCandelabra);
            int lowerTier = floor - DaisTierHeight - 1;
            Place(FromFarWall(l, DaisWidth - 3), lowerTier, ItemID.GraniteLamp);
            int bannerOffset = BackdropHalfWidth + 1;
            Place(throneX - bannerOffset, HangY(room, arch, throneX - bannerOffset), ItemID.BlueBanner);
            Place(throneX + bannerOffset, HangY(room, arch, throneX + bannerOffset), ItemID.BlueBanner);

            for (int b = 0; b < bays.Count; b++)
            {
                if (b % 2 == 1)
                {
                    int center = bays[b].center;
                    Place(center, HangY(room, arch, center), ItemID.SilverChandelier);
                }
            }

            int side = PillarFootHalfWidth + 1;
            foreach (int p in pillars)
            {
                Place(p - side, HangY(room, arch, p - side), ItemID.BlueBanner);
                Place(p + side, HangY(room, arch, p + side), ItemID.BlueBanner);
                if (DistanceFromFarWall(l, p) >= DaisWidth + 2)
                {
                    torches.Add((p - side, floor - 6, ItemID.BlueTorch));
                    torches.Add((p + side, floor - 6, ItemID.BlueTorch));
                }
            }
        }

        // The throne-room wall farthest from where the staircase comes in.
        private static int FromFarWall(LibraryLayout l, int offset)
        {
            return l.Side > 0 ? l.ThroneRoom.X + offset : l.ThroneRoom.Right - 1 - offset;
        }

        private static int DistanceFromFarWall(LibraryLayout l, int x)
        {
            return l.Side > 0 ? x - l.ThroneRoom.X : l.ThroneRoom.Right - 1 - x;
        }

        private static int ThroneX(LibraryLayout l) => FromFarWall(l, (DaisWidth - (DaisTiers - 1) * DaisTierStep) / 2);
        private static int DaisTop(LibraryLayout l) => l.ThroneRoom.Bottom - DaisTiers * DaisTierHeight;

        // ================= Helpers =================

        // Keeps columns and furniture out of the hall floor opening above the first staircase.
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

        // Vanilla furniture is placed by item, so the right style always comes from the game itself.
        private static int Style(int itemType) => ContentSamples.ItemsByType[itemType].placeStyle;

        private static void Place(int x, int y, int itemType, int direction = -1)
        {
            Item item = ContentSamples.ItemsByType[itemType];
            WorldGen.PlaceObject(x, y, item.createTile, true, item.placeStyle, 0, -1, direction);
        }

        // Torches, candles and other 1x1 items.
        private static void PlaceSingle(int x, int y, int itemType)
        {
            Item item = ContentSamples.ItemsByType[itemType];
            WorldGen.PlaceTile(x, y, item.createTile, true, false, -1, item.placeStyle);
        }

        private static void PlaceBooks(int x, int y)
        {
            WorldGen.PlaceTile(x, y, TileID.Books, true, false, -1, WorldGen.genRand.Next(5));
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
            tile.TileColor = 0;
            tile.LiquidAmount = 0;
        }

        private static void SetWall(int x, int y, ushort wall)
        {
            Tile tile = Main.tile[x, y];
            tile.WallType = wall;
            tile.WallColor = 0;
        }

        // Platform of the given style; may replace a decorative column but nothing else.
        private static void SetPlatform(int x, int y, int style)
        {
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || tile.TileType == TileID.MarbleColumn || tile.TileType == TileID.GraniteColumn)
            {
                SetTile(x, y, TileID.Platforms);
                tile.TileFrameY = (short)(style * 18);
            }
        }
    }
}
