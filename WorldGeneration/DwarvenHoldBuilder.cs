using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Tiles.DwarvenHold;
using Reign_of_the_Ancients.Tiles.DwarvenHold.Decor;
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
        public Rectangle Gatehouse, Hall, Forge, Treasury, Mine, Foundry;
        public Rectangle Gate;     // the sealed gate blocks
        public Rectangle Bounds;   // everything the builder may touch (except the approach tunnel)
        public Point Machine;      // top-left tile of the Titan control machine
    }

    // Layout (side view, gate on the left when Side = +1), styled after Andy's reference screenshots:
    //
    //  [lava-moss cave] |GATE| grey keep | == great hall: red brick, V gables, pillars, windows; lift bay in the middle == |
    //                                                              lift
    //            | workshop | ===== great forge: symmetric, ceiling steps up to the middle ===== | workshop | treasury |
    //                 |  hanging crucible under the landing, gear trains, boilers, tanks, pipes, cargo      |
    //   | ========== mine rail tunnel ============================================================= | ==== foundry ==== |
    //        excavation cavern  ==== lower rail tunnel ====  excavation cavern                         (Titan arena)
    //        side tunnels wander off everywhere, ending in small mining pockets
    //
    // Everything is carved into one grid first, then wrapped in a solid lining of hold blocks, so the hold is sealed
    // all around (its blocks can't be broken before Skeletron) and rooms automatically share walls. Glowing molten
    // seams run through the lining: one tile under each floor, and as an outline around the halls, forge and foundry.
    // Prototyped with a preview renderer (layout preview script), so the numbers below are tuned together.
    public static class DwarvenHoldBuilder
    {
        public const int Lining = 4;

        // ---- Gate, keep and great hall (local coords: u = tiles from the gate along Side, v = rows below the hall floor) ----
        public const int GateHeight = 7;
        public const int KeepWidth = 26, KeepHeight = 22, KeepChamfer = 4;
        public const int HallWidth = 154, HallHeight = 24;   // 7 bays; the middle one holds the lift
        public const int PillarSpacing = 22;
        public const int GableDepth = 6;
        public const int PillarHalfWidth = 2;
        public const int LiftWidth = 8;
        public const int LevelGap = 6;                       // thin stacked floors between levels

        // ---- Great forge, workshops, treasury ----
        public const int ForgeWidth = 124, ForgeHeight = 32;
        public const int ForgeFlatHalfWidth = 18;            // high flat middle of the stepped ceiling
        public const int ForgeMaxStep = 14;
        public const int StepWidth = 4, StepHeight = 2;
        public const int WorkshopWidth = 40, WorkshopHeight = 14;
        public const int ForgeDoorHeight = 8;
        public const int TreasuryWidth = 20, TreasuryHeight = 10, TreasuryDoorHeight = 5;
        public const int ScaffoldBeamSpacing = 6;            // wooden posts under the walkable machine platforms

        // ---- Mines ----
        public const int MineLength = 334, MineHeight = 9;
        public const int LowerTunnelTop = 88, LowerTunnelFloor = 96, LowerTunnelStart = 104, LowerTunnelEnd = 232;
        public const int BeamSpacing = 8;
        public const int SideTunnels = 10;
        public const int TunnelMinSteps = 35, TunnelMaxSteps = 70;
        public const int TunnelRadius = 2, PocketRadius = 4;
        public const float OreVeinChance = 0.035f;
        private static readonly (int u, int v, int rx, int ry)[] Caverns = { (90, 82, 24, 11), (250, 92, 26, 12) };

        // ---- Titan foundry ----
        public const int FoundryWidth = 140, FoundryHeight = 40;
        public const int FoundryFlatHalfWidth = 25, FoundryMaxStep = 12;
        public const int FoundryPillarSpacing = 28;
        public const int FarChannelWidth = 6;
        public const int DaisTiers = 3;

        // ---- Approach outside the gate ----
        public const int ApproachRadiusX = 12, ApproachRadiusY = 7;
        public const int ApproachTunnelSteps = 140;
        public const int LavaMossRange = 30;

        // Derived local coordinates.
        private const int KeepStart = Lining;
        private const int HallStart = KeepStart + KeepWidth + Lining;
        private const int HallEnd = HallStart + HallWidth;
        private const int Axis = HallStart + HallWidth / 2;
        private const int ForgeStart = Axis - ForgeWidth / 2;
        private const int ForgeEnd = ForgeStart + ForgeWidth;
        private const int ForgeTop = LevelGap;
        private const int ForgeFloor = ForgeTop + ForgeHeight;
        private const int WestShopStart = ForgeStart - Lining - WorkshopWidth;
        private const int EastShopStart = ForgeEnd + Lining;
        private const int TreasuryStart = EastShopStart + WorkshopWidth + Lining;
        private const int MineTop = ForgeFloor + LevelGap + 4;
        private const int MineFloor = MineTop + MineHeight;
        private const int MineEnd = MineLength;
        private const int WestShaft = WestShopStart + 10;
        private const int EastShaft = EastShopStart + WorkshopWidth - 18;
        private const int FoundryStart = MineEnd + Lining;
        private const int FoundryEnd = FoundryStart + FoundryWidth;
        private const int FoundryTop = MineFloor - FoundryHeight;
        private const int FoundryAxis = FoundryStart + FoundryWidth / 2;
        private const int Landing = ForgeFloor - 15;         // lift landing, just above the crucible
        private const int CrucibleTop = Landing + 2;         // hanging crucible, chains up to the landing

        private const int UMin = -40, UMax = FoundryEnd + 10, VMin = -36, VMax = 150;

        private static ushort brick, redBrick, moltenTrim, mineRock, foundryBrick, gateTile;
        private static ushort keepWall, hallWall, pillarWall, windowWall, forgeWall, cogWall, pipeWall, streamWall, treasuryWall;
        private static ushort mineWall, foundryWall, foundryPillarWall, foundryBandWall, moltenWall;

        private static byte[,] zone;     // carved space, by hold zone (0 = not carved)
        private static byte[,] lining;   // lining blocks, by zone of the space they wrap
        private static byte[,] dist;     // lining: distance to the nearest carved cell
        private static bool[,] gateCell;

        public static HoldLayout Plan(int gateX, int floorY, int side)
        {
            var l = new HoldLayout { GateX = gateX, FloorY = floorY, Side = side };
            l.Gatehouse = R(l, KeepStart, -KeepHeight, KeepWidth, KeepHeight);
            l.Hall = R(l, HallStart, -HallHeight, HallWidth, HallHeight);
            l.Forge = R(l, ForgeStart, ForgeTop, ForgeWidth, ForgeHeight);
            l.Treasury = R(l, TreasuryStart, ForgeFloor - TreasuryHeight, TreasuryWidth, TreasuryHeight);
            l.Mine = R(l, 0, MineTop, MineLength, MineHeight);
            l.Foundry = R(l, FoundryStart, FoundryTop, FoundryWidth, FoundryHeight);
            l.Gate = R(l, 0, -GateHeight, Lining, GateHeight);
            l.Bounds = R(l, UMin, VMin, UMax - UMin, VMax - VMin);
            l.Machine = R(l, FoundryStart + 7, MineFloor - DaisTiers - 3, 3, 3).Location;
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

        public static void Build(HoldLayout l)
        {
            LoadTypes();
            zone = new byte[UMax - UMin, VMax - VMin];
            lining = new byte[UMax - UMin, VMax - VMin];
            dist = new byte[UMax - UMin, VMax - VMin];
            gateCell = new bool[UMax - UMin, VMax - VMin];

            CarveRooms();
            CarveMines();
            BuildLining();
            ApplyToWorld(l);
            Approach(l);

            KeepDetails(l);
            HallDetails(l);
            ForgeDetails(l);
            WorkshopDetails(l);
            MineDetails(l);
            FoundryDetails(l);
            FramePipes(l);

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
            keepWall = (ushort)ModContent.WallType<HoldGatehouseWall>();
            hallWall = (ushort)ModContent.WallType<HoldHallWall>();
            pillarWall = (ushort)ModContent.WallType<HoldPillarWall>();
            windowWall = (ushort)ModContent.WallType<HoldWindowWall>();
            forgeWall = (ushort)ModContent.WallType<HoldForgeWall>();
            cogWall = (ushort)ModContent.WallType<HoldCogWall>();
            pipeWall = (ushort)ModContent.WallType<HoldPipeWall>();
            streamWall = (ushort)ModContent.WallType<HoldMoltenStreamWall>();
            treasuryWall = (ushort)ModContent.WallType<HoldTreasuryWall>();
            mineWall = (ushort)ModContent.WallType<HoldMineWall>();
            foundryWall = (ushort)ModContent.WallType<FoundryWall>();
            foundryPillarWall = (ushort)ModContent.WallType<FoundryPillarWall>();
            foundryBandWall = (ushort)ModContent.WallType<FoundryBandWall>();
            moltenWall = (ushort)ModContent.WallType<FoundryMoltenWall>();
        }

        // ================= Grid =================

        private static bool InGrid(int u, int v) => u >= UMin && u < UMax && v >= VMin && v < VMax;
        private static int ZoneAt(int u, int v) => InGrid(u, v) ? zone[u - UMin, v - VMin] : 0;
        private static int LiningAt(int u, int v) => InGrid(u, v) ? lining[u - UMin, v - VMin] : 0;
        private static bool InKeep(int u, int v) => u < HallStart && v < 0;

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

        private static void SetZone(int u, int v, int z)
        {
            if (InGrid(u, v))
            {
                zone[u - UMin, v - VMin] = (byte)z;
            }
        }

        // V gables: deepest at the pillars, peak in the middle of each bay.
        private static int GableDepthAt(int u)
        {
            int inBay = (u - HallStart) % PillarSpacing;
            return Math.Max(0, GableDepth - Math.Min(inBay, PillarSpacing - inBay) / 2);
        }

        // A ceiling that is high and flat in the middle and steps down toward the walls.
        private static int SteppedDepth(int u, int axis, int flatHalfWidth, int maxDepth)
        {
            int off = Math.Abs(u - axis) - flatHalfWidth;
            return off <= 0 ? 0 : Math.Min(maxDepth, (off / StepWidth + 1) * StepHeight);
        }

        private static int ForgeCeiling(int u) => ForgeTop + SteppedDepth(u, Axis, ForgeFlatHalfWidth, ForgeMaxStep);
        private static int FoundryCeiling(int u) => FoundryTop + SteppedDepth(u, FoundryAxis, FoundryFlatHalfWidth, FoundryMaxStep);

        private static void CarveRooms()
        {
            // Grey keep with stepped upper corners, the gate passage, and the doorway into the hall.
            for (int u = KeepStart; u < KeepStart + KeepWidth; u++)
            {
                int inner = Math.Min(u - KeepStart, KeepStart + KeepWidth - 1 - u);
                Mark(u, u + 1, -KeepHeight + Math.Max(0, KeepChamfer - inner), 0, DwarvenHold.GreatHall);
            }
            Mark(0, Lining, -GateHeight, 0, DwarvenHold.GreatHall);
            for (int u = 0; u < Lining; u++)
            {
                for (int v = -GateHeight; v < 0; v++)
                {
                    gateCell[u - UMin, v - VMin] = true;
                }
            }
            Mark(KeepStart + KeepWidth, HallStart, -9, 0, DwarvenHold.GreatHall);

            // Great hall under V-shaped gables, and the lift down to the forge in its middle bay.
            for (int u = HallStart; u < HallEnd; u++)
            {
                Mark(u, u + 1, -HallHeight + GableDepthAt(u), 0, DwarvenHold.GreatHall);
            }
            Mark(Axis - LiftWidth / 2, Axis + LiftWidth / 2, 0, ForgeTop, DwarvenHold.GreatHall);

            // Great forge with its stepped ceiling, the two workshops, the treasury and the doorways between them.
            for (int u = ForgeStart; u < ForgeEnd; u++)
            {
                Mark(u, u + 1, ForgeCeiling(u), ForgeFloor, DwarvenHold.Forges);
            }
            Mark(WestShopStart, WestShopStart + WorkshopWidth, ForgeFloor - WorkshopHeight, ForgeFloor, DwarvenHold.Forges);
            Mark(EastShopStart, EastShopStart + WorkshopWidth, ForgeFloor - WorkshopHeight, ForgeFloor, DwarvenHold.Forges);
            Mark(WestShopStart + WorkshopWidth, ForgeStart, ForgeFloor - ForgeDoorHeight, ForgeFloor, DwarvenHold.Forges);
            Mark(ForgeEnd, EastShopStart, ForgeFloor - ForgeDoorHeight, ForgeFloor, DwarvenHold.Forges);
            Mark(TreasuryStart, TreasuryStart + TreasuryWidth, ForgeFloor - TreasuryHeight, ForgeFloor, DwarvenHold.Forges);
            Mark(EastShopStart + WorkshopWidth, TreasuryStart, ForgeFloor - TreasuryDoorHeight, ForgeFloor, DwarvenHold.Forges);

            // Shafts from the workshops down to the mines.
            Mark(WestShaft, WestShaft + 8, ForgeFloor, MineTop, DwarvenHold.Forges);
            Mark(EastShaft, EastShaft + 8, ForgeFloor, MineTop, DwarvenHold.Forges);

            // Titan foundry with its stepped ceiling.
            for (int u = FoundryStart; u < FoundryEnd; u++)
            {
                Mark(u, u + 1, FoundryCeiling(u), MineFloor, DwarvenHold.Foundry);
            }
        }

        private static void CarveMines()
        {
            Mark(0, MineEnd, MineTop, MineFloor, DwarvenHold.Mines);
            Mark(MineEnd, FoundryStart, MineFloor - 10, MineFloor, DwarvenHold.Mines);
            foreach (var c in Caverns)
            {
                Cavern(c.u, c.v, c.rx, c.ry);
            }
            Mark(LowerTunnelStart, LowerTunnelEnd, LowerTunnelTop, LowerTunnelFloor, DwarvenHold.Mines);

            // Winding passages from the rail tunnel down into each cavern.
            Connect(70, MineFloor, Caverns[0].u - 6, Caverns[0].v - 8);
            Connect(268, MineFloor, Caverns[1].u + 4, Caverns[1].v - 10);

            // Side tunnels off the rail tunnel and off each cavern.
            for (int i = 0; i < SideTunnels; i++)
            {
                int u = 20 + i * 31 + WorldGen.genRand.Next(-5, 6);
                bool down = i % 2 == 1;
                Wander(u, down ? MineFloor - 1 : MineTop, 0, down ? 1 : -1, WorldGen.genRand.Next(TunnelMinSteps, TunnelMaxSteps + 1));
            }
            foreach (var c in Caverns)
            {
                int dir = WorldGen.genRand.NextBool() ? 1 : -1;
                Wander(c.u + dir * c.rx, c.v, dir, 0, WorldGen.genRand.Next(30, 56));
            }
        }

        // A lumpy elliptical cave with a flattened floor.
        private static void Cavern(int cu, int cv, int rx, int ry)
        {
            var harmonics = new (float amp, int k, float phase)[3];
            for (int i = 0; i < harmonics.Length; i++)
            {
                harmonics[i] = (WorldGen.genRand.NextFloat(0.06f, 0.16f), WorldGen.genRand.Next(2, 6), WorldGen.genRand.NextFloat(MathHelper.TwoPi));
            }
            for (int u = cu - rx - 4; u <= cu + rx + 4; u++)
            {
                for (int v = cv - ry - 4; v <= cv + ry + 4; v++)
                {
                    float nu = (u - cu) / (float)rx, nv = (v - cv) / (float)ry;
                    float angle = (float)Math.Atan2(nv, nu);
                    float r = 1f;
                    foreach (var h in harmonics)
                    {
                        r += h.amp * (float)Math.Sin(h.k * angle + h.phase);
                    }
                    if (nu * nu + nv * nv <= r * r && v < cv + ry * 0.75f)
                    {
                        SetZone(u, v, DwarvenHold.Mines);
                    }
                }
            }
        }

        private static void Connect(int u, int v, int u1, int v1)
        {
            while (u != u1 || v != v1)
            {
                if (WorldGen.genRand.NextFloat() < 0.6f && v != v1)
                {
                    v += Math.Sign(v1 - v);
                }
                else if (u != u1)
                {
                    u += Math.Sign(u1 - u);
                }
                else
                {
                    v += Math.Sign(v1 - v);
                }
                Blob(u, v, TunnelRadius);
            }
        }

        // A tunnel that mostly keeps going and sometimes turns, ending in a small mining pocket.
        private static void Wander(int u, int v, int du, int dv, int steps)
        {
            for (int s = 0; s < steps; s++)
            {
                if (WorldGen.genRand.NextBool(6))
                {
                    (du, dv) = WorldGen.genRand.Next(4) switch
                    {
                        0 => (1, 0),
                        1 => (-1, 0),
                        2 => (0, dv != 0 ? dv : 1),
                        _ => (du != 0 ? du : 1, 0),
                    };
                }
                int nu = u + du, nv = v + dv;
                if (nu <= 4 || nu >= MineEnd - 6 || nv <= MineTop - 2 || nv >= VMax - 8)
                {
                    du = -du;
                    dv = -dv;
                    continue;
                }
                u = nu;
                v = nv;
                Blob(u, v, TunnelRadius);
            }
            Blob(u, v, PocketRadius);
        }

        // Carves a round mine blob, but never closer than a full lining to the rooms.
        private static void Blob(int cu, int cv, int r)
        {
            for (int u = cu - r; u <= cu + r; u++)
            {
                for (int v = cv - r; v <= cv + r; v++)
                {
                    if ((u - cu) * (u - cu) + (v - cv) * (v - cv) > r * r + 1 || !InGrid(u, v) || ZoneAt(u, v) != 0)
                    {
                        continue;
                    }
                    if (!NearOtherZone(u, v, Lining + 1))
                    {
                        zone[u - UMin, v - VMin] = DwarvenHold.Mines;
                    }
                }
            }
        }

        private static bool NearOtherZone(int u, int v, int range)
        {
            for (int a = u - range; a <= u + range; a++)
            {
                for (int b = v - range; b <= v + range; b++)
                {
                    int z = ZoneAt(a, b);
                    if (z != 0 && z != DwarvenHold.Mines)
                    {
                        return true;
                    }
                }
            }
            return u < Lining * 2 + 2; // keep clear of the approach cave
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
                        SetWall(x, y, SpaceWall(z, u, v));
                    }
                    else if (lz != 0)
                    {
                        SetTile(x, y, LiningBlock(u, v, lz));
                        SetWall(x, y, SpaceWall(lz, u, v));
                    }
                }
            }
        }

        private static ushort SpaceWall(int z, int u, int v) => z switch
        {
            DwarvenHold.GreatHall => InKeep(u, v) ? keepWall : hallWall,
            DwarvenHold.Forges => forgeWall,
            DwarvenHold.Mines => mineWall,
            _ => foundryWall,
        };

        // Grey stone floor tops, molten seams, or the zone's brick.
        private static ushort LiningBlock(int u, int v, int z)
        {
            if (z == DwarvenHold.Mines)
            {
                return mineRock;
            }
            if (z == DwarvenHold.GreatHall && InKeep(u, v))
            {
                return brick;
            }
            int underFloor = 0; // rows below a carved space directly above (0 = none within the lining)
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
                return brick;
            }
            int d = dist[u - UMin, v - VMin];
            if (underFloor == 2 || (d == Lining - 1 && (underFloor == 0 || underFloor > 3)))
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

            float tu = cu, tv = cv, dirU = -1f, dirV = -0.6f;
            int minU = cu - ApproachRadiusX - 6, minV = cv - ApproachRadiusY - 6;
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
                    minU = Math.Min(minU, (int)tu - 6);
                    minV = Math.Min(minV, (int)tv - 6);
                }
            }

            for (int u = minU; u <= 0; u++)
            {
                for (int v = minV; v <= 4; v++)
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

        private static bool TouchesAir(int x, int y) =>
            !Main.tile[x + 1, y].HasTile || !Main.tile[x - 1, y].HasTile || !Main.tile[x, y + 1].HasTile || !Main.tile[x, y - 1].HasTile;

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

        private static void KeepDetails(HoldLayout l)
        {
            Place(X(l, KeepStart + 5), Y(l, -1), ItemID.ArmorStatue, l.Side);
            Place(X(l, KeepStart + 9), Y(l, -1), ItemID.ArmorStatue, l.Side);
            Place(X(l, KeepStart + KeepWidth - 5), Y(l, -1), ItemID.Keg);
            Place(X(l, KeepStart + KeepWidth / 2), Y(l, -12), ItemID.WeaponRack);
            Place(X(l, KeepStart + KeepWidth / 2), Y(l, -KeepHeight), ItemID.ChainLantern);
            PlaceSingle(X(l, KeepStart + 2), Y(l, -10), ItemID.Torch);
            PlaceSingle(X(l, KeepStart + KeepWidth - 2), Y(l, -10), ItemID.Torch);
        }

        private static void HallDetails(HoldLayout l)
        {
            int[] pots = { ItemID.PottedLavaPlantPalm, ItemID.PottedLavaPlantBush, ItemID.PottedLavaPlantBramble, ItemID.PottedLavaPlantBulb, ItemID.PottedLavaPlantTendrils };
            int bays = HallWidth / PillarSpacing;

            // Walk-through pillars: dark shaft with a wider base and capital.
            for (int k = 1; k < bays; k++)
            {
                int p = HallStart + k * PillarSpacing;
                int top = -HallHeight + GableDepth;
                for (int u = p - PillarHalfWidth - 1; u <= p + PillarHalfWidth + 1; u++)
                {
                    for (int v = top; v < 0; v++)
                    {
                        bool shaft = Math.Abs(u - p) <= PillarHalfWidth;
                        bool foot = v >= -2 || v < top + 2;
                        if ((shaft || foot) && ZoneAt(u, v) == DwarvenHold.GreatHall)
                        {
                            SetWall(X(l, u), Y(l, v), pillarWall);
                        }
                    }
                }
            }

            for (int b = 0; b < bays; b++)
            {
                int mid = HallStart + b * PillarSpacing + PillarSpacing / 2;
                Place(X(l, mid), Y(l, -HallHeight), ItemID.ChainLantern);
                if (Math.Abs(mid - Axis) <= 1)
                {
                    // Lift bay: a cog frame around the shaft opening.
                    for (int u = Axis - 6; u < Axis + 6; u++)
                    {
                        for (int v = -18; v < 0; v++)
                        {
                            bool frame = u < Axis - LiftWidth / 2 || u >= Axis + LiftWidth / 2 || v < -14;
                            if (frame && ZoneAt(u, v) == DwarvenHold.GreatHall)
                            {
                                SetWall(X(l, u), Y(l, v), cogWall);
                            }
                        }
                    }
                    continue;
                }
                // Tall framed window, potted lava plants either side, kegs and barrels in some bays.
                for (int u = mid - 2; u <= mid + 1; u++)
                {
                    for (int v = -19; v <= -4; v++)
                    {
                        bool frame = u == mid - 2 || u == mid + 1 || v == -19 || v == -4;
                        SetWall(X(l, u), Y(l, v), frame ? pillarWall : windowWall);
                    }
                }
                Place(X(l, mid - 4), Y(l, -1), pots[b % pots.Length]);
                Place(X(l, mid + 4), Y(l, -1), pots[(b + 2) % pots.Length]);
                if (b % 3 == 0)
                {
                    Place(X(l, mid - 8), Y(l, -1), ItemID.Keg);
                    Place(X(l, mid + 8), Y(l, -1), ItemID.Barrel);
                }
            }

            // Lift platforms from the hall floor down to the landing above the crucible.
            for (int v = 0; v <= Landing; v += 5)
            {
                Platforms(l, Axis - LiftWidth / 2, Axis + LiftWidth / 2, v);
            }
            Platforms(l, Axis - LiftWidth / 2 - 2, Axis + LiftWidth / 2 + 2, Landing);
        }

        private static void ForgeDetails(HoldLayout l)
        {
            // Cog panel behind the lift column.
            for (int u = Axis - 7; u < Axis + 7; u++)
            {
                for (int v = ForgeTop; v < Landing; v++)
                {
                    if (ZoneAt(u, v) == DwarvenHold.Forges)
                    {
                        SetWall(X(l, u), Y(l, v), cogWall);
                    }
                }
            }

            // Iron crucible hanging on two chains under the lift landing, pouring a molten stream into a floor pool.
            Decor<HoldCrucible>(l, Axis - 3, CrucibleTop);
            SetTile(X(l, Axis - 2), Y(l, CrucibleTop - 1), TileID.Chain);
            SetTile(X(l, Axis + 1), Y(l, CrucibleTop - 1), TileID.Chain);
            for (int v = CrucibleTop + 4; v < ForgeFloor; v++)
            {
                SetWall(X(l, Axis - 1), Y(l, v), streamWall);
                SetWall(X(l, Axis), Y(l, v), streamWall);
            }
            for (int u = Axis - 2; u <= Axis + 1; u++)
            {
                Lava(X(l, u), Y(l, ForgeFloor));
            }

            // Gear trains turning on either side of the lift (large -> medium -> small, alternating direction).
            for (int s = -1; s <= 1; s += 2)
            {
                bool cw = s > 0;
                Gear(l, Axis + s * 10, 12, HoldGear.Large, cw);
                Gear(l, Axis + s * 15, 16, HoldGear.Medium, !cw);
                Gear(l, Axis + s * 19, 14, HoldGear.Small, cw);
            }

            // Mirrored machinery on both sides of the crucible. a = offset of an object's inner edge from the axis.
            for (int s = -1; s <= 1; s += 2)
            {
                // Boiler on the floor beside the pool, its steam pipe running out to a tank on a raised platform.
                Decor<HoldBoiler>(l, SymStart(Axis, s, 6, 8), ForgeFloor - 6);
                int boilerPipe = SymCell(Axis, s, 9);   // over the boiler's steam dome
                int tankPipe = SymCell(Axis, s, 23);
                Pipe(l, (boilerPipe, ForgeFloor - 7), (boilerPipe, 26), (SymCell(Axis, s, 20), 26));

                Scaffold(l, SymStart(Axis, s, 18, 13), 13, 28, DwarvenHold.Forges);
                Decor<HoldPressureTank>(l, SymStart(Axis, s, 21, 4), 22);
                Pipe(l, (tankPipe, 21), (tankPipe, ForgeCeiling(tankPipe)));
                PipeVariant(l, tankPipe, 17, HoldPipe.Valve);
                PipeVariant(l, tankPipe, 13, HoldPipe.Gauge);

                // Toward the wall: a cargo crane whose jib reaches in toward the middle, with its load hanging on a
                // platform you can stand on.
                Crane(l, s, Axis, 56, ForgeFloor, ForgeFloor - 5);

                // Cargo on chains from the stepped ceiling, and a couple of lanterns (the boilers are the main light).
                int cargoA = SymStart(Axis, s, 32, 3), cargoB = SymStart(Axis, s, 37, 3);
                Hang(l, cargoA, ForgeCeiling(cargoA + 1), s > 0 ? HoldHangingCargo.Crate : HoldHangingCargo.Barrel, 4);
                Hang(l, cargoB, ForgeCeiling(cargoB + 1), s > 0 ? HoldHangingCargo.OreBucket : HoldHangingCargo.Pulley, 5);
                foreach (int a in new[] { 28, 48 })
                {
                    int u = SymCell(Axis, s, a);
                    Place(X(l, u), Y(l, ForgeCeiling(u)), ItemID.ChainLantern);
                }
            }
        }

        private static void WorkshopDetails(HoldLayout l)
        {
            int floor = ForgeFloor - 1;
            int[][] stations =
            {
                new int[] { ItemID.Hellforge, ItemID.IronAnvil, ItemID.HeavyWorkBench, ItemID.Keg },
                new int[] { ItemID.Extractinator, ItemID.Sawmill, ItemID.Furnace, ItemID.Barrel },
            };
            int[] starts = { WestShopStart, EastShopStart };
            int[] shafts = { WestShaft, EastShaft };
            for (int w = 0; w < 2; w++)
            {
                int start = starts[w];
                int s = 0;
                for (int u = start + 3; u < start + WorkshopWidth - 4 && s < stations[w].Length; u += 8)
                {
                    if (u + 3 >= shafts[w] - 1 && u <= shafts[w] + 8)
                    {
                        continue; // keep the shaft clear
                    }
                    Place(X(l, u + 1), Y(l, floor), stations[w][s++]);
                }
                foreach (int pu in new[] { start + 1, start + WorkshopWidth - 2 })
                {
                    for (int v = ForgeFloor - WorkshopHeight; v < ForgeFloor; v++)
                    {
                        SetWall(X(l, pu), Y(l, v), pipeWall);
                    }
                }
                Place(X(l, start + WorkshopWidth / 2), Y(l, ForgeFloor - WorkshopHeight), ItemID.ChainLantern);
                Place(X(l, start + WorkshopWidth / 2), Y(l, ForgeFloor - 8), ItemID.WeaponRack);

                for (int v = ForgeFloor; v < MineTop; v += 5)
                {
                    Platforms(l, shafts[w], shafts[w] + 8, v);
                }
            }

            // Treasury vault: gold brick walls, coin piles, a lantern. (Its chests come with the hold loot.)
            for (int u = TreasuryStart; u < TreasuryStart + TreasuryWidth; u++)
            {
                for (int v = ForgeFloor - TreasuryHeight; v < ForgeFloor; v++)
                {
                    SetWall(X(l, u), Y(l, v), treasuryWall);
                }
            }
            for (int u = TreasuryStart + 7; u < TreasuryStart + 13; u++)
            {
                PlaceSingle(X(l, u), Y(l, floor), u % 2 == 0 ? ItemID.GoldCoin : ItemID.SilverCoin);
            }
            Place(X(l, TreasuryStart + TreasuryWidth / 2), Y(l, ForgeFloor - TreasuryHeight), ItemID.ChainLantern);
        }

        private static void MineDetails(HoldLayout l)
        {
            // Rail track and timber supports in both tunnels.
            Track(l, 1, MineEnd - 1, MineFloor - 1);
            Track(l, LowerTunnelStart + 1, LowerTunnelEnd - 1, LowerTunnelFloor - 1);
            Supports(l, 4, MineEnd - 4, MineTop, MineFloor);
            Supports(l, LowerTunnelStart + 4, LowerTunnelEnd - 4, LowerTunnelTop, LowerTunnelFloor);

            // Wooden scaffolding inside each cavern: two decks on posts.
            foreach (var c in Caverns)
            {
                foreach (int deck in new[] { c.v - 2, c.v + 5 })
                {
                    int u0 = c.u - c.rx / 2 + WorldGen.genRand.Next(-3, 4);
                    for (int u = u0; u < u0 + c.rx; u++)
                    {
                        if (ZoneAt(u, deck) == DwarvenHold.Mines && !Main.tile[X(l, u), Y(l, deck)].HasTile)
                        {
                            SetTile(X(l, u), Y(l, deck), TileID.Platforms);
                        }
                    }
                    for (int u = u0; u < u0 + c.rx; u += 6)
                    {
                        for (int v = deck + 1; ZoneAt(u, v) == DwarvenHold.Mines && v < VMax - 1 && !Main.tile[X(l, u), Y(l, v)].HasTile; v++)
                        {
                            SetTile(X(l, u), Y(l, v), TileID.WoodenBeam);
                        }
                    }
                    PlaceSingle(X(l, u0 + 2), Y(l, deck - 1), ItemID.Torch);
                }
            }

            // Ore veins showing in the tunnel walls.
            int[] ores = { TileID.Iron, TileID.Lead, TileID.Silver, TileID.Tungsten, TileID.Gold, TileID.Platinum };
            for (int u = UMin; u < UMax; u++)
            {
                for (int v = VMin; v < VMax; v++)
                {
                    if (LiningAt(u, v) != DwarvenHold.Mines || dist[u - UMin, v - VMin] != 1 || WorldGen.genRand.NextFloat() >= OreVeinChance)
                    {
                        continue;
                    }
                    int ore = WorldGen.genRand.Next(ores);
                    for (int a = u - 1; a <= u + 1; a++)
                    {
                        for (int b = v - 1; b <= v + 1; b++)
                        {
                            Tile t = Main.tile[X(l, a), Y(l, b)];
                            if (LiningAt(a, b) == DwarvenHold.Mines && t.HasTile && t.TileType == mineRock && WorldGen.genRand.NextFloat() < 0.6f)
                            {
                                t.TileType = (ushort)ore;
                            }
                        }
                    }
                }
            }
        }

        private static void Track(HoldLayout l, int u0, int u1, int row)
        {
            for (int u = u0; u < u1; u++)
            {
                WorldGen.PlaceTile(X(l, u), Y(l, row), TileID.MinecartTrack, true, true);
            }
        }

        // Timber posts across a tunnel, skipping spots where something opens above or below; a torch on every other one.
        private static void Supports(HoldLayout l, int u0, int u1, int top, int floor)
        {
            int n = 0;
            for (int u = u0; u < u1; u += BeamSpacing)
            {
                if (ZoneAt(u, top - 1) != 0 || ZoneAt(u, floor) != 0)
                {
                    continue;
                }
                for (int v = top; v < floor - 1; v++)
                {
                    SetTile(X(l, u), Y(l, v), TileID.WoodenBeam);
                }
                if (n++ % 2 == 0)
                {
                    PlaceSingle(X(l, u + 1), Y(l, top + 3), ItemID.Torch);
                }
            }
        }

        private static void FoundryDetails(HoldLayout l)
        {
            // Giant piston columns with brass bands.
            foreach (int off in new[] { -2, -1, 1, 2 })
            {
                int p = FoundryAxis + off * FoundryPillarSpacing;
                for (int u = p - 2; u <= p + 2; u++)
                {
                    for (int v = FoundryTop; v < MineFloor; v++)
                    {
                        if (ZoneAt(u, v) == DwarvenHold.Foundry)
                        {
                            SetWall(X(l, u), Y(l, v), (v - FoundryTop) % 8 == 0 ? foundryBandWall : foundryPillarWall);
                        }
                    }
                }
            }

            // Molten fall pouring down the far wall into a lava channel.
            for (int u = FoundryEnd - FarChannelWidth; u < FoundryEnd; u++)
            {
                for (int v = MineFloor; v < MineFloor + 2; v++)
                {
                    Lava(X(l, u), Y(l, v));
                }
            }
            for (int u = FoundryEnd - 4; u <= FoundryEnd - 3; u++)
            {
                for (int v = FoundryTop; v < MineFloor; v++)
                {
                    if (ZoneAt(u, v) == DwarvenHold.Foundry)
                    {
                        SetWall(X(l, u), Y(l, v), moltenWall);
                    }
                }
            }

            // Stepped dais with the Titan control machine on top, just inside the entrance.
            for (int i = 0; i < DaisTiers; i++)
            {
                for (int u = FoundryStart + 4 + i; u < FoundryStart + 14 - i; u++)
                {
                    SetTile(X(l, u), Y(l, MineFloor - 1 - i), foundryBrick);
                }
            }
            WorldGen.PlaceObject(l.Machine.X + 1, l.Machine.Y + 2, ModContent.TileType<TitanControlMachine>(), true);

            // A huge iron gear high above the arena, meshing with gear trains on both sides. The floor stays clear
            // for the Titan's treads.
            const int F = FoundryTop;
            Gear(l, FoundryAxis, F + 8, HoldGear.Huge, true);
            for (int s = -1; s <= 1; s += 2)
            {
                Gear(l, FoundryAxis + s * 9, F + 11, HoldGear.Large, false);
                Gear(l, FoundryAxis + s * 14, F + 15, HoldGear.Medium, true);
                Gear(l, FoundryAxis + s * 18, F + 13, HoldGear.Small, false);
            }

            for (int s = -1; s <= 1; s += 2)
            {
                int midCargo = SymStart(FoundryAxis, s, 21, 3);
                Hang(l, midCargo, FoundryCeiling(midCargo + 1), s > 0 ? HoldHangingCargo.OreBucket : HoldHangingCargo.Crate, 4);
                int lantern = SymCell(FoundryAxis, s, 25);
                Place(X(l, lantern), Y(l, FoundryCeiling(lantern)), ItemID.ChainLantern);

                // Between the piston columns: a walkable platform on wooden posts carrying a boiler and a tank.
                const int deck = MineFloor - 12;
                Scaffold(l, SymStart(FoundryAxis, s, 33, 19), 19, deck, DwarvenHold.Foundry);
                Decor<HoldBoiler>(l, SymStart(FoundryAxis, s, 34, 8), deck - 6);
                Decor<HoldPressureTank>(l, SymStart(FoundryAxis, s, 45, 4), deck - 6);
                int boilerPipe = SymCell(FoundryAxis, s, 37);   // over the boiler's steam dome
                int tankPipe = SymCell(FoundryAxis, s, 47);
                Pipe(l, (boilerPipe, deck - 7), (boilerPipe, FoundryCeiling(boilerPipe)));
                Pipe(l, (tankPipe, deck - 7), (tankPipe, FoundryCeiling(tankPipe)));
                Pipe(l, (SymCell(FoundryAxis, s, 41), deck - 2), (SymCell(FoundryAxis, s, 44), deck - 2));
                PipeVariant(l, boilerPipe, deck - 15, HoldPipe.Valve);
                PipeVariant(l, boilerPipe, deck - 18, HoldPipe.Gauge);
                PipeVariant(l, tankPipe, deck - 11, HoldPipe.Gauge);
                int deckCargo = SymStart(FoundryAxis, s, 41, 3);
                Hang(l, deckCargo, FoundryCeiling(deckCargo + 1), s > 0 ? HoldHangingCargo.Barrel : HoldHangingCargo.Pulley, 4);
            }

            // Far end: a pipe down the wall beside the molten fall and cargo over the channel.
            int farPipe = FoundryAxis + 61;
            Pipe(l, (farPipe, FoundryCeiling(farPipe)), (farPipe, MineFloor - 1));
            PipeVariant(l, farPipe, MineFloor - 7, HoldPipe.Valve);
            PipeVariant(l, farPipe, MineFloor - 12, HoldPipe.Gauge);
            Hang(l, FoundryAxis + 63, FoundryCeiling(FoundryAxis + 64), HoldHangingCargo.Crate, 6);

            // Entrance end: the control machine is piped up into the ceiling, with a lantern beside it.
            int machinePipe = FoundryStart + 8;
            Pipe(l, (machinePipe, MineFloor - DaisTiers - 4), (machinePipe, FoundryCeiling(machinePipe)));
            PipeVariant(l, machinePipe, MineFloor - 13, HoldPipe.Gauge);
            PipeVariant(l, machinePipe, MineFloor - 19, HoldPipe.Valve);
            Hang(l, FoundryStart + 2, FoundryCeiling(FoundryStart + 3), HoldHangingCargo.OreBucket, 4);
            Place(X(l, FoundryStart + 12), Y(l, FoundryCeiling(FoundryStart + 12)), ItemID.ChainLantern);
        }

        // ================= Decor helpers =================

        // Mirroring around a room's axis (the axis is the edge between cells axis-1 and axis).
        // s = +1 for the axis side, -1 for the mirrored side; a = distance of the object's inner edge from the axis.
        private static int SymStart(int axis, int s, int a, int width) => s > 0 ? axis + a : axis - a - width;
        private static int SymCell(int axis, int s, int a) => s > 0 ? axis + a : axis - 1 - a;

        // A multi-tile decor object with its top-left at local (u0, v0) (left/right handled for either Side).
        private static void Decor<T>(HoldLayout l, int u0, int v0, int style = 0) where T : HoldDecorBase
        {
            T decor = ModContent.GetInstance<T>();
            int left = l.Side > 0 ? X(l, u0) : X(l, u0 + decor.Width - 1);
            HoldDecorBase.Place(left, Y(l, v0), decor.Type, decor.Width, decor.Height, style);
        }

        // A gear centred on the tile corner (cu, cv).
        private static void Gear(HoldLayout l, int cu, int cv, int size, bool clockwise)
        {
            Decor<HoldGear>(l, cu - 1, cv - 1, HoldGear.StyleFor(size, clockwise));
        }

        // A pipe through the given corner points (straight runs between them). Skips cells that are already taken;
        // a flange collar every few tiles. Connections are framed at the end by FramePipes.
        private static void Pipe(HoldLayout l, params (int u, int v)[] points)
        {
            ushort pipe = (ushort)ModContent.TileType<HoldPipe>();
            int run = 0;
            for (int k = 0; k + 1 < points.Length; k++)
            {
                (int u, int v) = points[k];
                (int u1, int v1) = points[k + 1];
                int du = Math.Sign(u1 - u), dv = Math.Sign(v1 - v);
                while (true)
                {
                    int x = X(l, u), y = Y(l, v);
                    if (!Main.tile[x, y].HasTile)
                    {
                        SetTile(x, y, pipe);
                        Main.tile[x, y].TileFrameY = (short)((run % 4 == 2 ? HoldPipe.Flange : HoldPipe.Plain) * 18);
                    }
                    run++;
                    if (u == u1 && v == v1)
                    {
                        break;
                    }
                    u += du;
                    v += dv;
                }
            }
        }

        private static void PipeVariant(HoldLayout l, int u, int v, int variant)
        {
            Tile tile = Main.tile[X(l, u), Y(l, v)];
            if (tile.HasTile && tile.TileType == ModContent.TileType<HoldPipe>())
            {
                tile.TileFrameY = (short)(variant * 18);
            }
        }

        // Joins neighbouring pipe tiles, and lets loose ends plug into whatever they touch
        // (the ceiling, the floor, a boiler, a tank, the crucible or the Titan machine).
        private static void FramePipes(HoldLayout l)
        {
            int pipe = ModContent.TileType<HoldPipe>();
            int[] dirs = { HoldPipe.Up, HoldPipe.Right, HoldPipe.Down, HoldPipe.Left };
            Point[] offsets = { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };
            for (int x = l.Bounds.Left; x < l.Bounds.Right; x++)
            {
                for (int y = l.Bounds.Top; y < l.Bounds.Bottom; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile || tile.TileType != pipe)
                    {
                        continue;
                    }
                    int mask = 0, count = 0;
                    for (int d = 0; d < 4; d++)
                    {
                        Tile n = Main.tile[x + offsets[d].X, y + offsets[d].Y];
                        if (n.HasTile && n.TileType == pipe)
                        {
                            mask |= dirs[d];
                            count++;
                        }
                    }
                    for (int d = 0; d < 4 && count < 2; d++)
                    {
                        bool opposite = count == 1 && (mask & dirs[(d + 2) % 4]) != 0;
                        if ((count == 0 || opposite) && PipeConnects(x + offsets[d].X, y + offsets[d].Y))
                        {
                            mask |= dirs[d];
                            count++;
                        }
                    }
                    tile.TileFrameX = (short)(mask * 18);
                }
            }
        }

        private static bool PipeConnects(int x, int y)
        {
            Tile t = Main.tile[x, y];
            if (!t.HasTile)
            {
                return false;
            }
            int type = t.TileType;
            return (Main.tileSolid[type] && !Main.tileSolidTop[type]) || type == ModContent.TileType<HoldBoiler>()
                || type == ModContent.TileType<HoldPressureTank>() || type == ModContent.TileType<HoldCrucible>()
                || type == ModContent.TileType<TitanControlMachine>();
        }

        // A walkable wooden platform at row v, held up by wooden posts down to the floor.
        private static void Scaffold(HoldLayout l, int u0, int width, int v, int zoneId)
        {
            for (int u = u0; u < u0 + width; u++)
            {
                if (!Main.tile[X(l, u), Y(l, v)].HasTile)
                {
                    SetTile(X(l, u), Y(l, v), TileID.Platforms);
                }
            }
            for (int k = 0; k < width; k += ScaffoldBeamSpacing)
            {
                Post(l, u0 + k, v + 1, zoneId);
            }
            if ((width - 1) % ScaffoldBeamSpacing != 0)
            {
                Post(l, u0 + width - 1, v + 1, zoneId);
            }
        }

        private static void Post(HoldLayout l, int u, int v, int zoneId)
        {
            for (; ZoneAt(u, v) == zoneId && !Main.tile[X(l, u), Y(l, v)].HasTile; v++)
            {
                SetTile(X(l, u), Y(l, v), TileID.WoodenBeam);
            }
        }

        // Cargo crane standing on `floor` with its tower centred on the edge `towerEdge` tiles from the axis (mirrored
        // by s) and its jib reaching back toward the axis. The load hangs on a rope from the jib tip and stands on a
        // row of platforms at `loadPlatformRow`.
        private static void Crane(HoldLayout l, int s, int axis, int towerEdge, int floor, int loadPlatformRow)
        {
            // Local edge -> world edge (the edge between local cells e-1 and e).
            int edge = s > 0 ? axis + towerEdge : axis - towerEdge;
            int worldEdge = l.Side > 0 ? l.GateX + edge : l.GateX - edge + 1;
            bool jibLeftInWorld = s * l.Side > 0; // the jib points back toward the axis
            int style = jibLeftInWorld ? HoldCrane.JibLeft : HoldCrane.JibRight;
            int anchorX = worldEdge - 1, anchorY = Y(l, floor - 2);
            HoldDecorBase.Place(anchorX, anchorY, ModContent.TileType<HoldCrane>(), 2, 2, style);

            int ropeX = anchorX + HoldCrane.RopeOffsetX(style);
            int loadTop = Y(l, loadPlatformRow - 3);
            for (int y = anchorY + HoldCrane.RopeStartOffsetY; y < loadTop; y++)
            {
                SetTile(ropeX, y, TileID.Rope);
            }
            HoldDecorBase.Place(ropeX - 2, loadTop, ModContent.TileType<HoldCraneLoad>(), 5, 3);
            for (int x = ropeX - 3; x <= ropeX + 3; x++)
            {
                SetTile(x, Y(l, loadPlatformRow), TileID.Platforms);
            }
        }

        // 3-wide cargo hanging on a chain from the ceiling row `top` (the chain runs down the middle column).
        private static void Hang(HoldLayout l, int u0, int top, int style, int chainLength)
        {
            for (int v = top; v < top + chainLength; v++)
            {
                SetTile(X(l, u0 + 1), Y(l, v), TileID.Chain);
            }
            Decor<HoldHangingCargo>(l, u0, top + chainLength, style);
        }

        private static void Platforms(HoldLayout l, int u0, int u1, int v)
        {
            for (int u = u0; u < u1; u++)
            {
                SetTile(X(l, u), Y(l, v), TileID.Platforms);
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
