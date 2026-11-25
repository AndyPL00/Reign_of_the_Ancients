using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Tiles.DwarvenHold;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace Reign_of_the_Ancients.WorldGeneration
{
    // Places the Dwarven Hold deep in the caverns during world gen, remembers where it is (saved + synced),
    // and opens its great gate the first time Skeletron is defeated.
    public class DwarvenHoldSystem : ModSystem
    {
        private const int Attempts = 600;
        private const double MinWorldFraction = 0.12;   // horizontal range for the gate...
        private const double MaxWorldFraction = 0.88;
        private const double MinDepthFraction = 0.35;   // ...and the hall floor, as a fraction of the cavern layer
        private const double MaxDepthFraction = 0.55;
        private const int WorldEdgePadding = 60;
        private const int UnderworldPadding = 25;
        private const int ProtectedScanStep = 3;
        private static readonly Color GateMessageColor = new Color(230, 160, 80);

        public static bool Exists;
        public static bool GateOpen;
        public static HoldLayout Layout;

        public override void ClearWorld()
        {
            Exists = false;
            GateOpen = false;
            Layout = default;
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            int index = tasks.FindIndex(pass => pass.Name.Equals("Final Cleanup"));
            var pass = new PassLegacy("Dwarven Hold", GenerateHold);
            if (index != -1)
            {
                tasks.Insert(index, pass);
            }
            else
            {
                tasks.Add(pass);
            }
        }

        private static void GenerateHold(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Carving the Dwarven Hold";
            int cavernTop = (int)Main.rockLayer;
            int cavernDepth = Main.UnderworldLayer - cavernTop;

            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                int gateX = (int)(Main.maxTilesX * (MinWorldFraction + WorldGen.genRand.NextDouble() * (MaxWorldFraction - MinWorldFraction)));
                int floorY = cavernTop + (int)(cavernDepth * (MinDepthFraction + WorldGen.genRand.NextDouble() * (MaxDepthFraction - MinDepthFraction)));
                int side = gateX < Main.maxTilesX / 2 ? 1 : -1; // extend toward the middle of the world

                HoldLayout layout = DwarvenHoldBuilder.Plan(gateX, floorY, side);
                if (!FitsInWorld(layout) || !IsClearSite(layout))
                {
                    continue;
                }
                Build(layout);
                return;
            }

            ModContent.GetInstance<Reign_of_the_Ancients>().Logger.Warn("Dwarven Hold: no valid site found, skipped.");
        }

        // Used by the /buildhold debug command: the gate floor is at the given tile.
        public static bool BuildAt(int gateX, int floorY)
        {
            int side = gateX < Main.maxTilesX / 2 ? 1 : -1;
            HoldLayout layout = DwarvenHoldBuilder.Plan(gateX, floorY, side);
            if (!FitsInWorld(layout))
            {
                return false;
            }
            Build(layout);
            return true;
        }

        private static void Build(HoldLayout layout)
        {
            DwarvenHoldBuilder.Build(layout);
            Layout = layout;
            Exists = true;
            GateOpen = false; // if Skeletron is already dead, PostUpdateWorld opens it right away
        }

        private static bool FitsInWorld(HoldLayout l)
        {
            return l.Bounds.Left > WorldEdgePadding
                && l.Bounds.Right < Main.maxTilesX - WorldEdgePadding
                && l.Bounds.Top > WorldEdgePadding
                && l.Bounds.Bottom < Main.UnderworldLayer - UnderworldPadding;
        }

        // Stay clear of the Mystical Library, the Dungeon, the Jungle Temple and the Shimmer.
        private static bool IsClearSite(HoldLayout l)
        {
            if (MysticalLibrarySystem.Exists && MysticalLibrarySystem.Layout.Bounds.Intersects(l.Bounds))
            {
                return false;
            }
            for (int x = l.Bounds.Left; x < l.Bounds.Right; x += ProtectedScanStep)
            {
                for (int y = l.Bounds.Top; y < l.Bounds.Bottom; y += ProtectedScanStep)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && (Main.tileDungeon[tile.TileType] || tile.TileType == TileID.LihzahrdBrick))
                    {
                        return false;
                    }
                    if (tile.WallType == WallID.LihzahrdBrickUnsafe || Main.wallDungeon[tile.WallType])
                    {
                        return false;
                    }
                    if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Shimmer)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override void PostUpdateWorld()
        {
            if (Exists && !GateOpen && NPC.downedBoss3)
            {
                OpenGate();
            }
        }

        // Removes the gate blocks (server / singleplayer) and announces it.
        public static void OpenGate()
        {
            GateOpen = true;
            Rectangle gate = Layout.Gate;
            int gateType = ModContent.TileType<HoldGate>();
            for (int x = gate.Left; x < gate.Right; x++)
            {
                for (int y = gate.Top; y < gate.Bottom; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && tile.TileType == gateType)
                    {
                        tile.HasTile = false;
                    }
                }
            }
            WorldGen.RangeFrame(gate.Left - 1, gate.Top - 1, gate.Right + 1, gate.Bottom + 1);
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendTileSquare(-1, gate.Left - 1, gate.Top - 1, gate.Width + 2, gate.Height + 2);
                NetMessage.SendData(MessageID.WorldData);
            }

            string key = "Mods.Reign_of_the_Ancients.HoldGateOpened";
            if (Language.GetTextValue(key).Length == 0)
            {
                return;
            }
            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromKey(key), GateMessageColor);
            }
            else
            {
                Main.NewText(Language.GetTextValue(key), GateMessageColor);
            }
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (Exists)
            {
                tag["holdGateX"] = Layout.GateX;
                tag["holdFloorY"] = Layout.FloorY;
                tag["holdSide"] = Layout.Side;
                tag["holdGateOpen"] = GateOpen;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            Exists = tag.ContainsKey("holdGateX");
            if (Exists)
            {
                Layout = DwarvenHoldBuilder.Plan(tag.GetInt("holdGateX"), tag.GetInt("holdFloorY"), tag.GetInt("holdSide"));
                GateOpen = tag.GetBool("holdGateOpen");
            }
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(Exists);
            writer.Write(GateOpen);
            writer.Write(Layout.GateX);
            writer.Write(Layout.FloorY);
            writer.Write(Layout.Side);
        }

        public override void NetReceive(BinaryReader reader)
        {
            Exists = reader.ReadBoolean();
            GateOpen = reader.ReadBoolean();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int side = reader.ReadInt32();
            Layout = Exists ? DwarvenHoldBuilder.Plan(x, y, side) : default;
        }
    }
}
