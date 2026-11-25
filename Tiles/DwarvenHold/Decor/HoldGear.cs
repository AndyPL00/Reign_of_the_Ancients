using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Tiles.DwarvenHold.Decor
{
    // A slowly turning gear. The tile itself is only a 2x2 axle; the whole gear (up to 12 tiles across) is drawn
    // centred on it, so meshing gears can sit close together. Style = size * 2 + (0 clockwise, 1 counter-clockwise).
    // All gears move their teeth at the same speed, so neighbours that touch look like they really mesh.
    public class HoldGear : HoldDecorBase
    {
        public const int Small = 0, Medium = 1, Large = 2, Huge = 3;

        private const float ToothSpeed = 9f; // pixels per second along the pitch circle

        // Pitch radius in pixels for each size (matches the generated textures: outer radius minus half a tooth).
        public static readonly float[] PitchRadius = { 28.5f, 44f, 60f, 91f };
        private static readonly string[] SizeNames = { "Small", "Medium", "Large", "Huge" };
        private static Asset<Texture2D>[] gearTextures;

        public override int Width => 2;
        public override int Height => 2;

        public static int StyleFor(int size, bool clockwise) => size * 2 + (clockwise ? 0 : 1);

        protected override void SetDecorDefaults()
        {
            if (!Main.dedServ)
            {
                gearTextures = new Asset<Texture2D>[SizeNames.Length];
                for (int s = 0; s < SizeNames.Length; s++)
                {
                    gearTextures[s] = ModContent.Request<Texture2D>(Texture + "_" + SizeNames[s]);
                }
            }
        }

        public override void Unload()
        {
            gearTextures = null;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            if (CellOf(i, j) != Point.Zero || gearTextures == null)
            {
                return false;
            }
            int style = StyleOf(i, j);
            int size = Utils.Clamp(style / 2, Small, Huge);
            float direction = style % 2 == 0 ? 1f : -1f;
            Texture2D texture = gearTextures[size].Value;

            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Vector2 center = new Vector2(i * 16 + 16, j * 16 + 16);
            float seconds = Main.GameUpdateCount / 60f;
            float rotation = direction * seconds * ToothSpeed / PitchRadius[size];
            Color light = Lighting.GetColor(i, j);

            spriteBatch.Draw(texture, center - Main.screenPosition + zero, null, light, rotation, texture.Size() / 2f, 1f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
