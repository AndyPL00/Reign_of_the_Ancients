using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Purely visual: pale-blue motes and arcane sparks drifting around the player inside the Mystical Library.
    // Runs only on the local client, and only spawns particles in front of library walls.
    public class LibraryAmbiencePlayer : ModPlayer
    {
        private const int SpawnRadiusTiles = 40;
        private const int MoteAttemptsPerTick = 2;
        private const int SparkChance = 6;          // 1 in N ticks
        private const float MoteDriftSpeed = 0.25f;
        private const float MoteRiseSpeed = 0.2f;
        private const float MoteScaleMin = 0.6f, MoteScaleMax = 1f;
        private const float SparkScale = 0.9f;

        public override void PostUpdate()
        {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer || !Player.InModBiome<MysticalLibraryBiome>())
            {
                return;
            }

            for (int i = 0; i < MoteAttemptsPerTick; i++)
            {
                if (TryRandomLibraryPoint(out Vector2 pos))
                {
                    Dust mote = Dust.NewDustPerfect(pos, DustID.IceTorch,
                        new Vector2(Main.rand.NextFloat(-MoteDriftSpeed, MoteDriftSpeed), -Main.rand.NextFloat(MoteRiseSpeed)),
                        150, default, Main.rand.NextFloat(MoteScaleMin, MoteScaleMax));
                    mote.noGravity = true;
                    mote.noLight = true;
                    mote.fadeIn = 1.2f;
                }
            }

            if (Main.rand.NextBool(SparkChance) && TryRandomLibraryPoint(out Vector2 sparkPos))
            {
                Dust spark = Dust.NewDustPerfect(sparkPos, DustID.GemSapphire, Main.rand.NextVector2Circular(0.4f, 0.4f), 100, default, SparkScale);
                spark.noGravity = true;
            }
        }

        // A random open point near the player that sits in front of a library wall (not inside blocks).
        private bool TryRandomLibraryPoint(out Vector2 pos)
        {
            pos = Player.Center + Main.rand.NextVector2Circular(SpawnRadiusTiles * 16f, SpawnRadiusTiles * 16f);
            Point tile = pos.ToTileCoordinates();
            if (!WorldGen.InWorld(tile.X, tile.Y) || MysticalLibrary.FloorAt(tile.X, tile.Y) == MysticalLibrary.NoFloor)
            {
                return false;
            }
            Tile t = Main.tile[tile.X, tile.Y];
            return !(t.HasTile && Main.tileSolid[t.TileType]);
        }
    }
}
