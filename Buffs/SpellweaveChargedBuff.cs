using Reign_of_the_Ancients.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Buffs
{
    // Shown while the Spellweave charge is full. The effect itself lives in SpellweavePlayer; this is just the icon.
    public class SpellweaveChargedBuff : ModBuff
    {
        public override LocalizedText Description => base.Description.WithFormatArgs((int)(SpellweavePlayer.EmpoweredMagicDamage * 100));

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
}
