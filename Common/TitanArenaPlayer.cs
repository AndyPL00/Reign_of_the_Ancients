using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.NPCs.Bosses;
using Reign_of_the_Ancients.WorldGeneration;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // While the Dwarven Titan is awake, players inside the foundry can't leave it (the same lock as Volodar's throne room).
    public class TitanArenaPlayer : ModPlayer
    {
        private const int GrabMargin = 48; // pixels: players this close to the foundry count as inside

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || !DwarvenHoldSystem.Exists || !NPC.AnyNPCs(ModContent.NPCType<DwarvenTitan>()))
            {
                return;
            }

            Rectangle room = DwarvenHoldSystem.Layout.Foundry;
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
