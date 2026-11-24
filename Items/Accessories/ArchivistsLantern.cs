using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Common;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Accessories
{
    // Archivist's Lantern (placeholder name). Library chest loot (cathedral floor).
    // Gives light, and treasure (ores, chests, pots...) close by sparkles (logic in ElvenAccessoryPlayer).
    public class ArchivistsLantern : ModItem
    {
        private static readonly Vector3 LightColor = new Vector3(0.75f, 0.85f, 1f);

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(ElvenAccessoryPlayer.LanternSenseRadius);

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            Lighting.AddLight(player.Center, LightColor);
            player.GetModPlayer<ElvenAccessoryPlayer>().archivistsLantern = true;
        }
    }
}
