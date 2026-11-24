using Reign_of_the_Ancients.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Buffs
{
    // Shown while the Guardian's Crest is ready to block. The effect lives in ElvenAccessoryPlayer.
    public class GuardiansCrestReadyBuff : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs((int)(ElvenAccessoryPlayer.CrestDamageReduction * 100));

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}
