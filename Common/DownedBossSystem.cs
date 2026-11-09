using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Reign_of_the_Ancients.Common
{
    // Remembers which of the mod's bosses have been defeated in this world.
    public class DownedBossSystem : ModSystem
    {
        public static bool downedVolodar;

        public override void ClearWorld()
        {
            downedVolodar = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (downedVolodar)
            {
                tag["downedVolodar"] = true;
            }
        }

        public override void LoadWorldData(TagCompound tag)
        {
            downedVolodar = tag.ContainsKey("downedVolodar");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(downedVolodar);
        }

        public override void NetReceive(BinaryReader reader)
        {
            downedVolodar = reader.ReadBoolean();
        }
    }
}
