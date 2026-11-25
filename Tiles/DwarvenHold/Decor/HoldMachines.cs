using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.DwarvenHold.Decor
{
    // Horizontal copper boiler on an iron sled, with smokestacks, a steam dome (pipe outlet, columns 3-4), gauges and
    // a glowing firebox hatch: the main warm light in the forge and the foundry. Smoke puffs from the stacks;
    // embers drift out of the grate.
    public class HoldBoiler : HoldDecorBase
    {
        private const int FrameCount = 3;
        private const int FrameTicks = 7;
        private const int SteamChance = 45;   // 1 in N drawn frames
        private const int EmberChance = 40;
        private static readonly Vector3 FireLight = new Vector3(1f, 0.5f, 0.15f);

        public override int Width => 8;
        public override int Height => 6;
        protected override Color MapColor => new Color(90, 88, 96);
        protected override int DecorDust => DustID.Iron;

        protected override void SetDecorDefaults()
        {
            Main.tileLighted[Type] = true;
            AnimationFrameHeight = Height * 18;
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            if (++frameCounter >= FrameTicks)
            {
                frameCounter = 0;
                frame = (frame + 1) % FrameCount;
            }
        }

        // The firebox hatch (columns 1-2, rows 3-4) glows and flickers.
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Point cell = CellOf(i, j);
            if (cell.X < 1 || cell.X > 2 || cell.Y < 3 || cell.Y > 4)
            {
                return;
            }
            float flicker = 0.85f + 0.15f * (float)Math.Sin(Main.GameUpdateCount * 0.21f + i * 1.7f);
            r = FireLight.X * flicker;
            g = FireLight.Y * flicker;
            b = FireLight.Z * flicker;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (CellOf(i, j) != Point.Zero || Main.gamePaused)
            {
                return;
            }
            Vector2 topLeft = new Vector2(i * 16, j * 16);
            if (Main.rand.NextBool(SteamChance))
            {
                // Smoke from one of the two stacks (tall iron one at x 99, short copper one at x 31).
                Vector2 stack = Main.rand.NextBool() ? new Vector2(99f, 2f) : new Vector2(31f, 10f);
                Dust smoke = Dust.NewDustPerfect(topLeft + stack, DustID.Smoke, new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -1.4f), 120, default, 1.4f);
                smoke.noGravity = true;
            }
            if (Main.rand.NextBool(EmberChance))
            {
                Dust ember = Dust.NewDustPerfect(topLeft + new Vector2(Main.rand.NextFloat(24f, 46f), 70f), DustID.Torch, new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), -0.8f), 100, default, 0.9f);
                ember.noGravity = true;
            }
        }
    }

    // Still-style copper tank on a stepped base, with two glowing portholes (row 4) and a gauge on its neck.
    public class HoldPressureTank : HoldDecorBase
    {
        private static readonly Vector3 GlassLight = new Vector3(0.28f, 0.13f, 0.03f);

        public override int Width => 4;
        public override int Height => 6;
        protected override Color MapColor => new Color(170, 92, 54);

        protected override void SetDecorDefaults()
        {
            Main.tileLighted[Type] = true;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Point cell = CellOf(i, j);
            if ((cell.X == 1 || cell.X == 2) && cell.Y == 4)
            {
                r = GlassLight.X;
                g = GlassLight.Y;
                b = GlassLight.Z;
            }
        }
    }

    // Iron crucible hanging on chains, brimming with molten metal and dripping from its spout.
    public class HoldCrucible : HoldDecorBase
    {
        private const int FrameCount = 3;
        private const int FrameTicks = 9;
        private const int DripChance = 6;
        private static readonly Vector3 MoltenLight = new Vector3(1f, 0.55f, 0.15f);

        public override int Width => 6;
        public override int Height => 4;
        protected override Color MapColor => new Color(235, 120, 40);
        protected override int DecorDust => DustID.Iron;

        protected override void SetDecorDefaults()
        {
            Main.tileLighted[Type] = true;
            AnimationFrameHeight = Height * 18;
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            if (++frameCounter >= FrameTicks)
            {
                frameCounter = 0;
                frame = (frame + 1) % FrameCount;
            }
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Point cell = CellOf(i, j);
            if (cell.Y == 0 || (cell.Y == 3 && (cell.X == 2 || cell.X == 3)))
            {
                r = MoltenLight.X;
                g = MoltenLight.Y;
                b = MoltenLight.Z;
            }
        }

        // Sparks spitting off the molten pour.
        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (CellOf(i, j) != Point.Zero || Main.gamePaused || !Main.rand.NextBool(DripChance))
            {
                return;
            }
            Vector2 spout = new Vector2(i * 16 + 48, j * 16 + 64);
            Dust spark = Dust.NewDustPerfect(spout + new Vector2(Main.rand.NextFloat(-6f, 6f), 0f), DustID.Torch,
                new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 0.5f)), 100, default, 1.1f);
            spark.noGravity = Main.rand.NextBool();
        }
    }

    // Cargo hanging from the ceiling on a chain: 0 crate, 1 ore bucket, 2 barrel, 3 pulley with a sack.
    public class HoldHangingCargo : HoldDecorBase
    {
        public const int Crate = 0, OreBucket = 1, Barrel = 2, Pulley = 3;

        public override int Width => 3;
        public override int Height => 3;
        protected override Color MapColor => new Color(116, 80, 50);
        protected override int DecorDust => DustID.WoodFurniture;
    }
}
