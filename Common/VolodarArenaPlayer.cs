using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.NPCs.Bosses;
using Reign_of_the_Ancients.WorldGeneration;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // While Volodar is alive, players inside the throne room can't leave it
    // (a magic barrier seals the doorway; Volodar draws its sparkles).
    public class VolodarArenaPlayer : ModPlayer
    {
        private const int GrabMargin = 48; // pixels: players this close to the room count as inside

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || !MysticalLibrarySystem.Exists || !NPC.AnyNPCs(ModContent.NPCType<Volodar>()))
            {
                return;
            }

            Rectangle room = MysticalLibrarySystem.Layout.ThroneRoom;
            var area = new Rectangle(room.X * 16, room.Y * 16, room.Width * 16, room.Height * 16);
            Rectangle grab = area;
            grab.Inflate(GrabMargin, GrabMargin);
            if (!grab.Intersects(Player.Hitbox))
            {
                return;
            }

            if (Player.position.X < area.Left)
            {
                Player.position.X = area.Left;
                Player.velocity.X = System.Math.Max(Player.velocity.X, 0f);
            }
            else if (Player.position.X > area.Right - Player.width)
            {
                Player.position.X = area.Right - Player.width;
                Player.velocity.X = System.Math.Min(Player.velocity.X, 0f);
            }

            if (Player.position.Y < area.Top)
            {
                Player.position.Y = area.Top;
                Player.velocity.Y = System.Math.Max(Player.velocity.Y, 0f);
            }
        }
    }
}
