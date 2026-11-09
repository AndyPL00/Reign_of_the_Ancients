using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace Reign_of_the_Ancients.Tiles.Trophies
{
    // Master Mode relic: a 3x4 pedestal (this tile's texture) with a golden Volodar figure floating above it
    // (VolodarRelic_Relic.png), drawn with the same bob and glow as vanilla relics.
    public class VolodarRelic : ModTile
    {
        public const int FrameWidth = 18 * 3;
        public const int FrameHeight = 18 * 4;

        private Asset<Texture2D> relicTexture;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                relicTexture = ModContent.Request<Texture2D>(Texture + "_Relic");
            }
        }

        public override void Unload()
        {
            relicTexture = null;
        }

        public override void SetStaticDefaults()
        {
            Main.tileShine[Type] = 400;
            Main.tileFrameImportant[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
            TileObjectData.newTile.StyleHorizontal = false;
            TileObjectData.newTile.StyleWrapLimitVisualOverride = 2;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newTile.StyleWrapLimit = 2;
            TileObjectData.newTile.styleLineSkipVisualOverride = 0;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(233, 207, 94), Language.GetText("MapObject.Relic"));
        }

        public override bool CreateDust(int i, int j, ref int type) => false;

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            // The pedestal always uses the first style; only the floating figure flips with direction.
            tileFrameX %= FrameWidth;
            tileFrameY %= FrameHeight * 2;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            // Register the top-left tile so SpecialDraw runs once per relic.
            if (drawData.tileFrameX % FrameWidth == 0 && drawData.tileFrameY % FrameHeight == 0)
            {
                Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
            }
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Vector2 offScreen = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Tile tile = Main.tile[i, j];
            if (!tile.HasTile || relicTexture == null)
            {
                return;
            }

            Texture2D texture = relicTexture.Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() / 2f;
            Vector2 worldPos = new Point(i, j).ToWorldCoordinates(24f, 64f);
            Color color = Lighting.GetColor(i, j);
            bool facingRight = tile.TileFrameY / FrameHeight != 0;
            SpriteEffects effects = facingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            const float TwoPi = MathHelper.TwoPi;
            float bob = (float)Math.Sin(Main.GlobalTimeWrappedHourly * TwoPi / 5f);
            Vector2 drawPos = worldPos + offScreen - Main.screenPosition + new Vector2(0f, -40f + bob * 4f);
            spriteBatch.Draw(texture, drawPos, frame, color, 0f, origin, 1f, effects, 0f);

            // Soft pulsing glow around the figure.
            float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * TwoPi / 2f) * 0.3f + 0.7f;
            Color glow = (color with { A = 0 }) * 0.1f * pulse;
            for (float t = 0f; t < 1f; t += 1f / 6f)
            {
                spriteBatch.Draw(texture, drawPos + (TwoPi * t).ToRotationVector2() * (6f + bob * 2f), frame, glow, 0f, origin, 1f, effects, 0f);
            }
        }
    }
}
