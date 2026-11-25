using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Purely visual: embers rising and faint steam drifting through the air of the great forge and the Titan foundry.
    // Local client only; particles only appear in open air in front of forge/foundry walls.
    // (Boilers, valves and the crucible add their own steam and sparks, see Tiles/DwarvenHold/Decor.)
    public class DwarvenHoldAmbiencePlayer : ModPlayer
    {
        private const int SpawnRadiusTiles = 40;
        private const int EmberChance = 2;     // 1 in N ticks
        private const int SteamChance = 7;
        private const float EmberRiseMin = 0.4f, EmberRiseMax = 1.1f;
        private const float EmberScaleMin = 0.5f, EmberScaleMax = 0.9f;

        public override void PostUpdate()
        {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer || !Player.InModBiome<DwarvenHoldBiome>())
            {
                return;
            }

            if (Main.rand.NextBool(EmberChance) && TryRandomPoint(out Vector2 pos))
            {
                Dust ember = Dust.NewDustPerfect(pos, DustID.Torch,
                    new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(EmberRiseMin, EmberRiseMax)),
                    120, default, Main.rand.NextFloat(EmberScaleMin, EmberScaleMax));
                ember.noGravity = true;
                ember.noLight = true;
            }

            if (Main.rand.NextBool(SteamChance) && TryRandomPoint(out Vector2 steamPos))
            {
                Dust steam = Dust.NewDustPerfect(steamPos, DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -0.3f), 200, default, 1.6f);
                steam.noGravity = true;
            }
        }

        private bool TryRandomPoint(out Vector2 pos)
        {
            pos = Player.Center + Main.rand.NextVector2Circular(SpawnRadiusTiles * 16f, SpawnRadiusTiles * 16f);
            Point tile = pos.ToTileCoordinates();
            int zone = DwarvenHold.ZoneAt(tile.X, tile.Y);
            if (zone != DwarvenHold.Forges && zone != DwarvenHold.Foundry)
            {
                return false;
            }
            Tile t = Main.tile[tile.X, tile.Y];
            return !(t.HasTile && Main.tileSolid[t.TileType]);
        }
    }
}
