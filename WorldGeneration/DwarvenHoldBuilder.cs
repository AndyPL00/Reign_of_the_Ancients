using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Tiles.DwarvenHold;
using Reign_of_the_Ancients.Walls.DwarvenHold;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.WorldGeneration
{
    // World positions of the hold's main spaces (interiors; the floor of each room is the row at rect.Bottom).
    public struct HoldLayout
    {
        public int GateX;      // column of the gate's outer face
        public int FloorY;     // great hall floor row
        public int Side;       // +1: the hold extends right of the gate, -1: left
        public Rectangle Gatehouse, Hall, Forge, Mine, Foundry;
        public Rectangle Gate;     // the sealed gate blocks
        public Rectangle Bounds;   // everything the builder may touch (except the approach tunnel)
        public Point Machine;      // top-left tile of the Titan control machine
    }

    // Layout (side view, the gate on the left when Side = +1), styled after Andy's reference screenshots:
    //
    //   [lava-moss cave] |GATE| grey gatehouse | ==== great hall: red brick, V gables, pillars, tall windows ==== |
    //                                                         (glowing molten seam under the floor)        shaft
    //        | ==== forges: ceilings stepping up per bay; crucible + furnace banks, water tanks, ore buckets ==== |
    //          shaft
    //   | ======== mine rail tunnel (track, timber supports) ====================================== | foundry |
    //        side tunnels wander up and down from the rail tunnel, ending in small mining pockets    (Titan arena)
    //
    // Everything is carved into one grid first, then wrapped in a solid lining of hold blocks, so the hold is
    // sealed all around (its blocks can't be broken before Skeletron) and rooms automatically share walls.
    // Glowing molten seams run through the lining: one tile under each floor, and as an outline around the
    // halls, forges and foundry.
    public static class DwarvenHoldBuilder
    {
        public const int Lining = 4;               // thickness of the sealed shell around every space

        // ---- Rooms, in local coords: u = tiles from the gate along Side, v = rows below the hall floor ----
        public const int GateHeight = 7;
        public const int GatehouseWidth = 26, GatehouseHeight = 20;
        public const int HallWidth = 154, HallHeight = 24;
        public const int HallPillarSpacing = 22;
        public const int HallGableDepth = 6;       // V-shaped gables: lowest at the pillars, peak mid-bay
        public const int PillarHalfWidth = 2;      // 5-wide pillars
        public const int WindowHeight = 14;
        public const int LevelGap = 6;             // thin stacked floors between levels, like the reference
        public const int ForgeWidth = 170, ForgeHeight = 20;
        public const int ForgeBayWidth = 34;
        public const int ForgeVaultDepth = 5;      // ceilings step up toward the middle of each bay
        public const int MineLength = 300, MineHeight = 9;
        public const int MineBeamSpacing = 8;
        public const int FoundryWidth = 130, FoundryHeight = 36;
        public const int ShaftWidth = 8;
        public const int ShaftPlatformEvery = 5;

        // ---- Forge machinery ----
        public const int BigFurnaceWidth = 9, BigFurnaceHeight = 7;
        public const int SmallFurnaceWidth = 7, SmallFurnaceHeight = 5;
        public const int SmokestackHeight = 4;
        public const int TankWidth = 8, TankHeight = 6;
        public const int CrucibleHalfWidth = 3;

        // ---- Mines ----
        public const int BranchCount = 7;
        public const int BranchMinSteps = 40, BranchMaxSteps = 75;
        public const int BranchRadius = 2;
        public const int PocketRadius = 4;
        public const int BranchMaxDepth = 108;

        // ---- Foundry ----
        public const int TroughHeightAboveFloor = 25;
        public const int TroughInset = 20;
        public const int MoltenFallSpacing = 18;
        public const int FarPitWidth = 6;

        // ---- Approach outside the gate ----
        public const int ApproachRadiusX = 12, ApproachRadiusY = 7;
        public const int ApproachTunnelSteps = 140;
        public const int LavaMossRange = 30;

        // Local grid extents.
        private const int UMin = -40, UMax = 460, VMin = -36, VMax = 126;

        private static ushort brick, redBrick, moltenTrim, mineRock, foundryBrick, gateTile;
        private static ushort gatehouseWall, hallWall, pillarWall, windowWall, forgeWall, cogWall, pipeWall, streamWall, mineWall, foundryWall, moltenWall;

        private static byte[,] zone;     // carved space, by hold zone (0 = not carved)
        private static byte[,] lining;   // lining blocks, by zone of the space they wrap
        private static byte[,] dist;     // lining: distance to the nearest carved cell
        private static bool[,] gateCell;

        public static HoldLayout Plan(int gateX, int floorY, int side)
        {
            var l = new HoldLayout { GateX = gateX, FloorY = floorY, Side = side };
            l.Gatehouse = R(l, Lining, -GatehouseHeight, GatehouseWidth, GatehouseHeight);
            l.Hall = R(l, HallStart, -HallHeight, HallWidth, HallHeight);
            l.Forge = R(l, ForgeStart, ForgeTop, ForgeWidth, ForgeHeight);
            l.Mine = R(l, MineStart, MineTop, MineLength, MineHeight);
            l.Foundry = R(l, FoundryStart, FoundryTop, FoundryWidth, FoundryHeight);
            l.Gate = R(l, 0, -GateHeight, Lining, GateHeight);
            l.Bounds = R(l, UMin, VMin, UMax - UMin, VMax - VMin);
            l.Machine = R(l, FoundryStart + 4, MineFloor - 3, 3, 3).Location;
            return l;
        }

        // Rectangle from local coords (u along Side, v down from the hall floor).
        private static Rectangle R(HoldLayout l, int u0, int v0, int w, int h)
        {
            int x = l.Side > 0 ? l.GateX + u0 : l.GateX - u0 - w + 1;
            return new Rectangle(x, l.FloorY + v0, w, h);
        }

        private static int X(HoldLayout l, int u) => l.GateX + l.Side * u;
        private static int Y(HoldLayout l, int v) => l.FloorY + v;

        // Local coords of each room.
        private static int HallStart => Lining + GatehouseWidth + Lining;
        private static int HallEnd => HallStart + HallWidth;
        private static int ForgeTop => LevelGap;
        private static int ForgeFloor => ForgeTop + ForgeHeight;
        private static int ForgeStart => HallEnd - ForgeWidth;
        private static int MineTop => ForgeFloor + LevelGap + 2;
        private static int MineFloor => MineTop + MineHeight;
        private static int MineStart => 10;
        private static int MineEnd => MineStart + MineLength;
        private static int FoundryStart => MineEnd + Lining;
        private static int FoundryEnd => FoundryStart + FoundryWidth;
        private static int FoundryTop => MineFloor - FoundryHeight;
        private static int Shaft1Start => HallEnd - 12;
        private static int Shaft2Start => ForgeStart + 6;

        public static void Build(HoldLayout l)
        {
            LoadTypes();
            zone = new byte[UMax - UMin, VMax - VMin];
            lining = new byte[UMax - UMin, VMax - VMin];
            dist = new byte[UMax - UMin, VMax - VMin];
            gateCell = new bool[UMax - UMin, VMax - VMin];

            CarveSpaces();
            CarveBranches();
            BuildLining();
            ApplyToWorld(l);
            Approach(l);

            GatehouseDetails(l);
            HallDetails(l);
            ForgeDetails(l);
            MineDetails(l);
            FoundryDetails(l);

            WorldGen.RangeFrame(l.Bounds.Left, l.Bounds.Top, l.Bounds.Right, l.Bounds.Bottom);
            zone = null;
            lining = null;
            dist = null;
            gateCell = null;
        }

        private static void LoadTypes()
        {
            brick = (ushort)ModContent.TileType<HoldBrick>();
            redBrick = (ushort)ModContent.TileType<HoldRedBrick>();
            moltenTrim = (ushort)ModContent.TileType<HoldMoltenTrim>();
            mineRock = (ushort)ModContent.TileType<HoldMineRock>();
            foundryBrick = (ushort)ModContent.TileType<FoundryBrick>();
            gateTile = (ushort)ModContent.TileType<HoldGate>();
            gatehouseWall = (ushort)ModContent.WallType<HoldGatehouseWall>();
            hallWall = (ushort)ModContent.WallType<HoldHallWall>();
            pillarWall = (ushort)ModContent.WallType<HoldPillarWall>();
            windowWall = (ushort)ModContent.WallType<HoldWindowWall>();
            forgeWall = (ushort)ModContent.WallType<HoldForgeWall>();
            cogWall = (ushort)ModContent.WallType<HoldCogWall>();
            pipeWall = (ushort)ModContent.WallType<HoldPipeWall>();
            streamWall = (ushort)ModContent.WallType<HoldMoltenStreamWall>();
            mineWall = (ushort)ModContent.WallType<HoldMineWall>();
            foundryWall = (ushort)ModContent.WallType<FoundryWall>();
            moltenWall = (ushort)ModContent.WallType<FoundryMoltenWall>();
        }

        // ================= Grid =================

        private static bool InGrid(int u, int v) => u >= UMin && u < UMax && v >= VMin && v < VMax;
        private static int ZoneAt(int u, int v) => InGrid(u, v) ? zone[u - UMin, v - VMin] : 0;
        private static int LiningAt(int u, int v) => InGrid(u, v) ? lining[u - UMin, v - VMin] : 0;
        private static bool IsGatehouse(int u) => u < HallStart;

        private static void Mark(int u0, int u1, int v0, int v1, int z)
        {
            for (int u = u0; u < u1; u++)
            {
                for (int v = v0; v < v1; v++)
                {
                    if (InGrid(u, v))
                    {
                        zone[u - UMin, v - VMin] = (byte)z;
                    }
                }
            }
        }

        private static void Unmark(int u0, int u1, int v0, int v1) => Mark(u0, u1, v0, v1, DwarvenHold.NoZone);

        private static void CarveSpaces()
        {
            // Gatehouse, gate passage and the doorway into the hall.
            Mark(Lining, Lining + GatehouseWidth, -GatehouseHeight, 0, DwarvenHold.GreatHall);
            Mark(0, Lining, -GateHeight, 0, DwarvenHold.GreatHall);
            for (int u = 0; u < Lining; u++)
            {
                for (int v = -GateHeight; v < 0; v++)
                {
                    gateCell[u - UMin, v - VMin] = true;
                }
            }
            Mark(Lining + GatehouseWidth, HallStart, -9, 0, DwarvenHold.GreatHall);

            // Great hall under V-shaped stepped gables.
            Mark(HallStart, HallEnd, -HallHeight, 0, DwarvenHold.GreatHall);
            for (int u = HallStart; u < HallEnd; u++)
            {
                Unmark(u, u + 1, -HallHeight, -HallHeight + HallGableDepthAt(u));
            }

            // Shaft from the hall down to the forges.
            Mark(Shaft1Start, Shaft1Start + ShaftWidth, 0, ForgeTop, DwarvenHold.GreatHall);

            // Forges, each bay's ceiling stepping up toward its middle.
            Mark(ForgeStart, HallEnd, ForgeTop, ForgeFloor, DwarvenHold.Forges);
            for (int u = ForgeStart; u < HallEnd; u++)
            {
                Unmark(u, u + 1, ForgeTop, ForgeTop + ForgeVaultDepthAt(u));
            }

            // Shaft from the forges down to the mines.
            Mark(Shaft2Start, Shaft2Start + ShaftWidth, ForgeFloor, MineTop, DwarvenHold.Forges);

            // Mine rail tunnel, and the doorway into the foundry.
            Mark(MineStart, MineEnd, MineTop, MineFloor, DwarvenHold.Mines);
            Mark(MineEnd, FoundryStart, MineFloor - 9, MineFloor, DwarvenHold.Mines);

            // Titan foundry.
            Mark(FoundryStart, FoundryEnd, FoundryTop, MineFloor, DwarvenHold.Foundry);
        }

        // Straight, 1-tile steps: deepest at the pillars, flush at the peak in the middle of each bay.
        private static int HallGableDepthAt(int u)
        {
            int inBay = (u - HallStart) % HallPillarSpacing;
            int fromPillar = Math.Min(inBay, HallPillarSpacing - inBay);
            return Math.Max(0, HallGableDepth - fromPillar / 2);
        }

        // Low at the bay edges, stepping up 1 tile every 2 tiles to a flat high middle.
        private static int ForgeVaultDepthAt(int u)
        {
            int inBay = (u - ForgeStart) % ForgeBayWidth;
            int fromEdge = Math.Min(inBay, ForgeBayWidth - 1 - inBay);
            return Math.Max(0, ForgeVaultDepth - fromEdge / 2);
        }

        private static List<(int a, int b)> ForgeBays()
        {
            var bays = new List<(int a, int b)>();
            for (int a = ForgeStart; a < HallEnd; a += ForgeBayWidth)
            {
                bays.Add((a, Math.Min(a + ForgeBayWidth, HallEnd) - 1));
            }
            return bays;
        }

        // Side tunnels wander off the rail tunnel and end in small mining pockets.
        private static void CarveBranches()
        {
            int spacing = (MineLength - 40) / BranchCount;
            for (int i = 0; i < BranchCount; i++)
            {
                int u = MineStart + 25 + i * spacing + WorldGen.genRand.Next(-6, 7);
                bool up = i % 2 == 0;
                int v = up ? MineTop : MineFloor - 1;
                int du = 0, dv = up ? -1 : 1;
                int steps = WorldGen.genRand.Next(BranchMinSteps, BranchMaxSteps + 1);

                for (int s = 0; s < steps; s++)
                {
                    if (WorldGen.genRand.NextBool(6))
                    {
                        int pick = WorldGen.genRand.Next(4);
                        (du, dv) = pick switch
                        {
                            0 => (1, 0),
                            1 => (-1, 0),
                            2 => (0, up ? -1 : 1),
                            _ => (WorldGen.genRand.NextBool() ? 1 : -1, 0),
                        };
                    }
                    int nu = u + du, nv = v + dv;
                    if (!BranchCellAllowed(nu, nv))
                    {
                        du = -du;
                        dv = -dv;
                        continue;
                    }
                    u = nu;
                    v = nv;
                    CarveBlob(u, v, BranchRadius);
                }
                CarveBlob(u, v, PocketRadius);
            }
        }

        private static bool BranchCellAllowed(int u, int v) => u > 4 && u < MineEnd - 10 && v > VMin + 10 && v < BranchMaxDepth;

        // Carves a round mine blob, but never closer than a full lining to the hall, forges or foundry.
        private static void CarveBlob(int cu, int cv, int r)
        {
            for (int u = cu - r; u <= cu + r; u++)
            {
                for (int v = cv - r; v <= cv + r; v++)
                {
                    if ((u - cu) * (u - cu) + (v - cv) * (v - cv) > r * r + 1 || !InGrid(u, v) || ZoneAt(u, v) != 0)
                    {
                        continue;
                    }
                    if (!NearOtherZone(u, v, Lining + 1, DwarvenHold.Mines))
                    {
                        zone[u - UMin, v - VMin] = DwarvenHold.Mines;
                    }
                }
            }
        }

        private static bool NearOtherZone(int u, int v, int range, int own)
        {
            for (int a = u - range; a <= u + range; a++)
            {
                for (int b = v - range; b <= v + range; b++)
                {
                    int z = ZoneAt(a, b);
                    if (z != 0 && z != own)
                    {
                        return true;
                    }
                }
            }
            return u < Lining * 2 + 2; // keep clear of the approach cave in front of the gate
        }

        // Every non-carved cell within Lining of a carved one becomes a hold block; remember how far it is.
        private static void BuildLining()
        {
            for (int u = UMin; u < UMax; u++)
            {
                for (int v = VMin; v < VMax; v++)
                {
                    byte z = zone[u - UMin, v - VMin];
                    if (z == 0)
                    {
                        continue;
                    }
                    for (int a = u - Lining; a <= u + Lining; a++)
                    {
                        for (int b = v - Lining; b <= v + Lining; b++)
                        {
                            if (!InGrid(a, b) || zone[a - UMin, b - VMin] != 0 || IsGateFront(a, b))
                            {
                                continue;
                            }
                            byte d = (byte)Math.Max(Math.Abs(a - u), Math.Abs(b - v));
                            if (lining[a - UMin, b - VMin] == 0)
                            {
                                lining[a - UMin, b - VMin] = z;
                                dist[a - UMin, b - VMin] = d;
                            }
                            else if (d < dist[a - UMin, b - VMin])
                            {
                                dist[a - UMin, b - VMin] = d;
                            }
                        }
                    }
                }
            }
        }

        // The open space right in front of the gate stays clear (it's the approach cave).
        private static bool IsGateFront(int u, int v) => u < 0 && u >= -Lining && v >= -GateHeight && v < 0;

        private static void ApplyToWorld(HoldLayout l)
        {
            for (int u = UMin; u < UMax; u++)
            {
                for (int v = VMin; v < VMax; v++)
                {
                    int x = X(l, u), y = Y(l, v);
                    if (!WorldGen.InWorld(x, y, 2))
                    {
                        continue;
                    }
                    int z = zone[u - UMin, v - VMin];
                    int lz = lining[u - UMin, v - VMin];
                    Tile tile = Main.tile[x, y];
                    if (z != 0)
                    {
                        if (gateCell[u - UMin, v - VMin])
                        {
                            SetTile(x, y, gateTile);
                        }
                        else
                        {
                            tile.ClearTile();
                        }
                        tile.LiquidAmount = 0;
                        SetWall(x, y, SpaceWall(z, u));
                    }
                    else if (lz != 0)
                    {
                        SetTile(x, y, LiningBlock(u, v, lz));
                        SetWall(x, y, SpaceWall(lz, u));
                    }
                }
            }
        }

        private static ushort SpaceWall(int z, int u) => z switch
        {
            DwarvenHold.GreatHall => IsGatehouse(u) ? gatehouseWall : hallWall,
            DwarvenHold.Forges => forgeWall,
            DwarvenHold.Mines => mineWall,
            _ => foundryWall,
        };

        // Which block a lining cell gets: grey stone floor tops, molten seams, or the zone's brick.
        private static ushort LiningBlock(int u, int v, int z)
        {
            if (z == DwarvenHold.Mines)
            {
                return mineRock;
            }
            if (z == DwarvenHold.GreatHall && IsGatehouse(u))
            {
                return brick;
            }

            // How far below a carved floor this cell is (0 = not under a floor).
            int underFloor = 0;
            for (int k = 1; k <= Lining; k++)
            {
                if (ZoneAt(u, v - k) != 0)
                {
                    underFloor = k;
                    break;
                }
            }

            if (z != DwarvenHold.Foundry && underFloor == 1)
            {
                return brick; // thin grey floor top
            }
            int d = dist[u - UMin, v - VMin];
            bool seam = underFloor == 2 || (d == Lining - 1 && (underFloor == 0 || underFloor > 3));
            if (seam)
            {
                return moltenTrim;
            }
            return z == DwarvenHold.Foundry ? foundryBrick : redBrick;
        }

        // A natural cave in front of the gate, a rough tunnel winding up and away from it so it can be found,
        // and glowing lava moss on the bare stone around it.
        private static void Approach(HoldLayout l)
        {
            int cu = -Lining - ApproachRadiusX, cv = -ApproachRadiusY + 1;
            for (int u = cu - ApproachRadiusX; u <= cu + ApproachRadiusX; u++)
            {
                for (int v = cv - ApproachRadiusY; v <= 0; v++)
                {
                    float du = (u - cu) / (float)ApproachRadiusX, dv = (v - cv) / (float)ApproachRadiusY;
                    if (du * du + dv * dv <= 1f || IsGateFront(u, v))
                    {
                        ClearNatural(l, u, v);
                    }
                }
            }

            float tu = cu, tv = cv;
            float dirU = -1f, dirV = -0.6f;
            var tunnel = new List<Point>();
            for (int s = 0; s < ApproachTunnelSteps; s++)
            {
                dirU = MathHelper.Clamp(dirU + WorldGen.genRand.NextFloat(-0.3f, 0.3f), -1f, 0.3f);
                dirV = MathHelper.Clamp(dirV + WorldGen.genRand.NextFloat(-0.3f, 0.3f), -1f, 0.4f);
                tu += dirU;
                tv += dirV;
                int r = WorldGen.genRand.Next(2, 4);
                for (int u = (int)tu - r; u <= (int)tu + r; u++)
                {
                    for (int v = (int)tv - r; v <= (int)tv + r; v++)
                    {
                        if ((u - tu) * (u - tu) + (v - tv) * (v - tv) <= r * r)
                        {
                            ClearNatural(l, u, v);
                        }
                    }
                }
                if (s < LavaMossRange)
                {
                    tunnel.Add(new Point((int)tu, (int)tv));
                }
            }

            // Lava moss on exposed stone around the cave and the first stretch of the tunnel.
            int minU = cu - ApproachRadiusX - 6, maxU = 0, minV = cv - ApproachRadiusY - 6, maxV = 4;
            foreach (Point p in tunnel)
            {
                minU = Math.Min(minU, p.X - 6);
                minV = Math.Min(minV, p.Y - 6);
            }
            for (int u = minU; u <= maxU; u++)
            {
                for (int v = minV; v <= maxV; v++)
                {
                    int x = X(l, u), y = Y(l, v);
                    if (ZoneAt(u, v) != 0 || LiningAt(u, v) != 0 || !WorldGen.InWorld(x, y, 10))
                    {
                        continue;
                    }
                    Tile t = Main.tile[x, y];
                    if (t.HasTile && t.TileType == TileID.Stone && TouchesAir(x, y))
                    {
                        t.TileType = TileID.LavaMoss;
                    }
                }
            }
        }

        private static bool TouchesAir(int x, int y)
        {
            return !Main.tile[x + 1, y].HasTile || !Main.tile[x - 1, y].HasTile || !Main.tile[x, y + 1].HasTile || !Main.tile[x, y - 1].HasTile;
        }

        // Clears a natural tile, but never one of the hold's or another structure's furniture.
        private static void ClearNatural(HoldLayout l, int u, int v)
        {
            if (ZoneAt(u, v) != 0 || LiningAt(u, v) != 0)
            {
                return;
            }
            int x = X(l, u), y = Y(l, v);
            if (!WorldGen.InWorld(x, y, 10))
            {
                return;
            }
            Tile tile = Main.tile[x, y];
            if (tile.HasTile && (Main.tileFrameImportant[tile.TileType] || TileID.Sets.BasicChest[tile.TileType]))
            {
                return;
            }
            tile.HasTile = false;
            tile.LiquidAmount = 0;
        }

        // ================= Details =================

        private static void GatehouseDetails(HoldLayout l)
        {
            int gx = Lining + 4;
            Place(X(l, gx), Y(l, -1), ItemID.ArmorStatue, l.Side);
            Place(X(l, gx + 4), Y(l, -1), ItemID.ArmorStatue, l.Side);
            Place(X(l, Lining + GatehouseWidth - 5), Y(l, -1), ItemID.Keg);
            Place(X(l, Lining + GatehouseWidth / 2), Y(l, -12), ItemID.WeaponRack);
            Place(X(l, Lining + GatehouseWidth / 2), Y(l, -GatehouseHeight), ItemID.ChainLantern);
            PlaceSingle(X(l, Lining + 2), Y(l, -10), ItemID.Torch);
            PlaceSingle(X(l, Lining + GatehouseWidth - 2), Y(l, -10), ItemID.Torch);
        }

        private static void HallDetails(HoldLayout l)
        {
            int[] pots = { ItemID.PottedLavaPlantPalm, ItemID.PottedLavaPlantBush, ItemID.PottedLavaPlantBramble, ItemID.PottedLavaPlantBulb, ItemID.PottedLavaPlantTendrils };
            int bay = 0;
            for (int p = HallStart; p < HallEnd; p += HallPillarSpacing, bay++)
            {
                // Pillar (walk-through): dark shaft with a wider base and capital.
                if (p > HallStart)
                {
                    Pillar(l, p);
                }

                int mid = p + HallPillarSpacing / 2;
                if (mid >= HallEnd - 3)
                {
                    break;
                }
                bool nearShaft = mid >= Shaft1Start - 4 && mid <= Shaft1Start + ShaftWidth + 4;

                // Tall framed window in the middle of the bay, a lantern hanging from the gable's peak.
                Window(l, mid);
                Place(X(l, mid), Y(l, -HallHeight), ItemID.ChainLantern);

                if (!nearShaft)
                {
                    // Potted lava plants either side of the window.
                    Place(X(l, mid - 4), Y(l, -1), pots[bay % pots.Length]);
                    Place(X(l, mid + 4), Y(l, -1), pots[(bay + 2) % pots.Length]);
                    if (bay % 3 == 1)
                    {
                        Place(X(l, p + 5), Y(l, -1), ItemID.Keg);
                        Place(X(l, p + HallPillarSpacing - 5), Y(l, -1), ItemID.Barrel);
                    }
                }
            }

            ShaftPlatforms(l, Shaft1Start, 0, ForgeTop);
        }

        private static void Pillar(HoldLayout l, int p)
        {
            int top = -HallHeight + HallGableDepth;
            for (int u = p - PillarHalfWidth - 1; u <= p + PillarHalfWidth + 1; u++)
            {
                bool shaft = Math.Abs(u - p) <= PillarHalfWidth;
                for (int v = top; v < 0; v++)
                {
                    bool foot = v >= -2 || v < top + 2; // wider base and capital
                    if ((shaft || foot) && ZoneAt(u, v) == DwarvenHold.GreatHall)
                    {
                        SetWall(X(l, u), Y(l, v), pillarWall);
                    }
                }
            }
        }

        private static void Window(HoldLayout l, int mid)
        {
            int bottom = -4, top = bottom - WindowHeight;
            for (int u = mid - 2; u <= mid + 1; u++)
            {
                for (int v = top - 1; v <= bottom; v++)
                {
                    bool frame = u == mid - 2 || u == mid + 1 || v == top - 1 || v == bottom;
                    SetWall(X(l, u), Y(l, v), frame ? pillarWall : windowWall);
                }
            }
        }

        private static void ForgeDetails(HoldLayout l)
        {
            List<(int a, int b)> bays = ForgeBays();
            int centre = bays.Count / 2;
            for (int i = 0; i < bays.Count; i++)
            {
                (int a, int b) = bays[i];
                int mid = (a + b) / 2;
                bool hasShaft = a <= Shaft2Start + ShaftWidth && b >= Shaft2Start;

                // Steam pipes up the back wall at both ends of the bay.
                foreach (int pu in new[] { a + 1, b - 2 })
                {
                    for (int v = ForgeTop; v < ForgeFloor; v++)
                    {
                        if (ZoneAt(pu, v) == DwarvenHold.Forges && ZoneAt(pu + 1, v) == DwarvenHold.Forges)
                        {
                            SetWall(X(l, pu), Y(l, v), pipeWall);
                            SetWall(X(l, pu + 1), Y(l, v), pipeWall);
                        }
                    }
                }
                foreach (int lu in new[] { a + 7, b - 7 })
                {
                    Place(X(l, lu), Y(l, ForgeTop + ForgeVaultDepthAt(lu)), ItemID.ChainLantern);
                }

                if (hasShaft)
                {
                    continue;
                }
                if (i == centre)
                {
                    Crucible(l, mid);
                }
                else
                {
                    switch (i % 3)
                    {
                        case 0:
                            Furnace(l, mid - 8, SmallFurnaceWidth, SmallFurnaceHeight, true);
                            Place(X(l, mid + 4), Y(l, ForgeFloor - 1), ItemID.Hellforge);
                            Place(X(l, mid + 9), Y(l, ForgeFloor - 1), ItemID.IronAnvil);
                            break;
                        case 1:
                            WaterTank(l, mid - 10);
                            Place(X(l, mid + 4), Y(l, ForgeFloor - 1), ItemID.HeavyWorkBench);
                            Place(X(l, mid + 9), Y(l, ForgeFloor - 1), ItemID.Extractinator);
                            break;
                        default:
                            OreBuckets(l, a, b);
                            Place(X(l, mid - 6), Y(l, ForgeFloor - 1), ItemID.Furnace);
                            Place(X(l, mid + 6), Y(l, ForgeFloor - 1), ItemID.Barrel);
                            break;
                    }
                }
            }

            ShaftPlatforms(l, Shaft2Start, ForgeFloor, MineTop);
        }

        // Centrepiece: a crucible pouring a molten stream into a floor pool between two big furnace banks,
        // in front of a cog panel.
        private static void Crucible(HoldLayout l, int c)
        {
            for (int u = c - 6; u <= c + 6; u++)
            {
                for (int v = ForgeTop + 1; v < ForgeTop + 10; v++)
                {
                    if (ZoneAt(u, v) == DwarvenHold.Forges)
                    {
                        SetWall(X(l, u), Y(l, v), cogWall);
                    }
                }
            }

            int cupV = ForgeTop + 7; // bottom row of the crucible
            for (int u = c - CrucibleHalfWidth; u <= c + CrucibleHalfWidth; u++)
            {
                SetTile(X(l, u), Y(l, cupV), foundryBrick);
                bool side = Math.Abs(u - c) == CrucibleHalfWidth;
                for (int v = cupV - 2; v < cupV; v++)
                {
                    if (side)
                    {
                        SetTile(X(l, u), Y(l, v), foundryBrick);
                    }
                    else
                    {
                        Lava(X(l, u), Y(l, v));
                    }
                }
            }
            for (int v = cupV + 1; v < ForgeFloor; v++)
            {
                SetWall(X(l, c), Y(l, v), streamWall);
            }
            for (int u = c - 2; u <= c + 2; u++)
            {
                Lava(X(l, u), Y(l, ForgeFloor));
            }

            Furnace(l, c - 4 - BigFurnaceWidth, BigFurnaceWidth, BigFurnaceHeight, false);
            Furnace(l, c + 4, BigFurnaceWidth, BigFurnaceHeight, false);
        }

        // A brick furnace block standing on the forge floor, with a grid of fire windows (living fire blocks).
        private static void Furnace(HoldLayout l, int left, int width, int height, bool smokestack)
        {
            int floor = ForgeFloor;
            for (int u = left; u < left + width; u++)
            {
                for (int v = floor - height; v < floor; v++)
                {
                    int du = u - left, dv = v - (floor - height);
                    bool window = du % 2 == 1 && dv % 2 == 1 && du < width - 1 && dv < height - 1;
                    SetTile(X(l, u), Y(l, v), window ? TileID.LivingFire : foundryBrick);
                }
            }
            if (smokestack)
            {
                int su = left + width / 2;
                for (int v = floor - height - SmokestackHeight; v < floor - height; v++)
                {
                    SetTile(X(l, su), Y(l, v), foundryBrick);
                    SetTile(X(l, su + 1), Y(l, v), foundryBrick);
                }
            }
        }

        // Glass cooling tank filled with water, with pipes running up to the ceiling.
        private static void WaterTank(HoldLayout l, int left)
        {
            int floor = ForgeFloor;
            for (int u = left; u < left + TankWidth; u++)
            {
                for (int v = floor - TankHeight; v < floor; v++)
                {
                    bool shell = u == left || u == left + TankWidth - 1 || v == floor - TankHeight || v == floor - 1;
                    if (shell)
                    {
                        SetTile(X(l, u), Y(l, v), TileID.Glass);
                    }
                    else
                    {
                        Tile t = Main.tile[X(l, u), Y(l, v)];
                        t.ClearTile();
                        t.LiquidType = LiquidID.Water;
                        t.LiquidAmount = (byte)(v == floor - TankHeight + 1 ? 128 : 255);
                    }
                }
            }
            foreach (int pu in new[] { left + 2, left + TankWidth - 3 })
            {
                for (int v = ForgeTop; v < floor - TankHeight; v++)
                {
                    if (ZoneAt(pu, v) == DwarvenHold.Forges)
                    {
                        SetWall(X(l, pu), Y(l, v), pipeWall);
                    }
                }
            }
        }

        // Ore buckets hanging on chains across the bay.
        private static void OreBuckets(HoldLayout l, int a, int b)
        {
            int hangV = ForgeTop + ForgeVaultDepth + 4;
            for (int u = a + 6; u + 2 < b - 4; u += 8)
            {
                for (int v = ForgeTop; v < hangV; v++)
                {
                    if (ZoneAt(u + 1, v) == DwarvenHold.Forges && !Main.tile[X(l, u + 1), Y(l, v)].HasTile)
                    {
                        SetTile(X(l, u + 1), Y(l, v), TileID.Chain);
                    }
                }
                for (int k = 0; k < 3; k++)
                {
                    SetTile(X(l, u + k), Y(l, hangV), TileID.Platforms);
                }
                SetTile(X(l, u + 1), Y(l, hangV - 1), WorldGen.genRand.NextBool() ? TileID.Gold : TileID.Iron);
            }
        }

        private static void MineDetails(HoldLayout l)
        {
            int railRow = Y(l, MineFloor - 1);
            for (int u = MineStart + 1; u < MineEnd - 1; u++)
            {
                WorldGen.PlaceTile(X(l, u), railRow, TileID.MinecartTrack, true, true);
            }

            int n = 0;
            for (int u = MineStart + 4; u < MineEnd - 4; u += MineBeamSpacing, n++)
            {
                if (u >= Shaft2Start - 2 && u <= Shaft2Start + ShaftWidth + 1)
                {
                    continue;
                }
                for (int v = MineTop; v < MineFloor - 1; v++)
                {
                    SetTile(X(l, u), Y(l, v), TileID.WoodenBeam);
                }
                if (n % 2 == 0)
                {
                    PlaceSingle(X(l, u + 1), Y(l, MineTop + 3), ItemID.Torch);
                }
            }
        }

        private static void FoundryDetails(HoldLayout l)
        {
            int floorV = MineFloor;

            // Lava aqueduct near the ceiling, held in a brick trough, with molten seams glowing below it.
            int troughV = floorV - TroughHeightAboveFloor;
            int ta = FoundryStart + TroughInset, tb = FoundryEnd - TroughInset;
            for (int u = ta; u <= tb; u++)
            {
                SetTile(X(l, u), Y(l, troughV), foundryBrick);
                bool side = u == ta || u == tb;
                for (int v = troughV - 2; v < troughV; v++)
                {
                    if (side)
                    {
                        SetTile(X(l, u), Y(l, v), foundryBrick);
                    }
                    else
                    {
                        Lava(X(l, u), Y(l, v));
                    }
                }
            }
            for (int u = ta + MoltenFallSpacing / 2; u < tb; u += MoltenFallSpacing)
            {
                for (int v = troughV + 1; v < floorV; v++)
                {
                    SetWall(X(l, u), Y(l, v), moltenWall);
                    SetWall(X(l, u + 1), Y(l, v), moltenWall);
                }
            }

            // Lava channel along the far wall.
            for (int u = FoundryEnd - FarPitWidth; u < FoundryEnd; u++)
            {
                for (int v = floorV; v < floorV + 2; v++)
                {
                    Lava(X(l, u), Y(l, v));
                }
            }

            // Chains hanging from the ceiling.
            for (int u = FoundryStart + 8; u < FoundryEnd - 8; u += WorldGen.genRand.Next(7, 13))
            {
                int length = WorldGen.genRand.Next(3, 9);
                for (int v = FoundryTop; v < FoundryTop + length; v++)
                {
                    if (!Main.tile[X(l, u), Y(l, v)].HasTile)
                    {
                        SetTile(X(l, u), Y(l, v), TileID.Chain);
                    }
                }
            }

            // The Titan control machine just inside the entrance, and a pair of big furnace banks behind it.
            WorldGen.PlaceObject(l.Machine.X + 1, l.Machine.Y + 2, ModContent.TileType<TitanControlMachine>(), true);
            PlaceSingle(X(l, FoundryStart + 2), Y(l, floorV - 6), ItemID.Torch);
            PlaceSingle(X(l, FoundryStart + 24), Y(l, floorV - 6), ItemID.Torch);
        }

        // Wooden platforms across a shaft: one flush with the upper floor, then one every few rows.
        private static void ShaftPlatforms(HoldLayout l, int start, int topV, int bottomV)
        {
            for (int v = topV; v < bottomV; v += ShaftPlatformEvery)
            {
                for (int u = start; u < start + ShaftWidth; u++)
                {
                    SetTile(X(l, u), Y(l, v), TileID.Platforms);
                }
            }
        }

        // ================= Tile helpers (same approach as the Library builder) =================

        private static void Place(int x, int y, int itemType, int direction = -1)
        {
            Item item = ContentSamples.ItemsByType[itemType];
            WorldGen.PlaceObject(x, y, item.createTile, true, item.placeStyle, 0, -1, direction);
        }

        private static void PlaceSingle(int x, int y, int itemType)
        {
            Item item = ContentSamples.ItemsByType[itemType];
            WorldGen.PlaceTile(x, y, item.createTile, true, false, -1, item.placeStyle);
        }

        private static void Lava(int x, int y)
        {
            Tile t = Main.tile[x, y];
            t.ClearTile();
            t.LiquidType = LiquidID.Lava;
            t.LiquidAmount = 255;
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
    }
}
