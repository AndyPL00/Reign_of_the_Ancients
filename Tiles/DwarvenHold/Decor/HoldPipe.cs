using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.DwarvenHold.Decor
{
    // One tile of brass steam pipe (non-solid, placed by world gen). The frame column is the connection mask
    // (1 up, 2 right, 4 down, 8 left, worked out by DwarvenHoldBuilder after all pipes are laid);
    // the row is the variant: plain, flange collar, valve wheel or pressure gauge. Valves hiss out a little steam.
    public class HoldPipe : ModTile
    {
        public const int Plain = 0, Flange = 1, Valve = 2, Gauge = 3;
        public const int Up = 1, Right = 2, Down = 4, Left = 8;

        private const int SteamChance = 240; // 1 in N drawn frames, per valve

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileLavaDeath[Type] = false;
            Main.tileNoAttach[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            DustType = DustID.Copper;
            HitSound = SoundID.Tink;
            MinPick = HoldBlockBase.RequiredPickPower;
            AddMapEntry(new Color(196, 136, 62));
        }

        public override bool CanKillTile(int i, int j, ref bool blockDamaged) => NPC.downedBoss3;

        public override bool CanExplode(int i, int j) => false;

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (Main.gamePaused || Main.tile[i, j].TileFrameY / 18 != Valve || !Main.rand.NextBool(SteamChance))
            {
                return;
            }
            Dust steam = Dust.NewDustPerfect(new Vector2(i * 16 + 8, j * 16 + 8), DustID.Smoke,
                new Vector2(Main.rand.NextFloat(-1f, 1f), -1f), 150, default, 1.1f);
            steam.noGravity = true;
        }
    }
}
