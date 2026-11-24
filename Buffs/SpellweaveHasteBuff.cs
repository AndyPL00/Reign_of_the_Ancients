using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Buffs
{
    // Spellblade set: magic hits grant bonus melee speed for a few seconds.
    public class SpellweaveHasteBuff : ModBuff
    {
        public const float MeleeSpeedBonus = 0.15f;

        public override LocalizedText Description => base.Description.WithFormatArgs((int)(MeleeSpeedBonus * 100));

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeedBonus;
        }
    }
}
