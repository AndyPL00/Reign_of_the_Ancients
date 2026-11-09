using Terraria.ModLoader;

namespace Reign_of_the_Ancients.DamageClasses
{
    // Hybrid class: benefits fully from generic, melee AND magic damage/crit bonuses (they stack additively).
    public class MeleeMagicDamageClass : DamageClass
    {
        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            if (damageClass == Generic || damageClass == Melee || damageClass == Magic)
            {
                return StatInheritanceData.Full;
            }

            return StatInheritanceData.None;
        }

        // Lets melee- and magic-specific effects (e.g. flask imbues, magic-only accessories) apply.
        public override bool GetEffectInheritance(DamageClass damageClass)
        {
            return damageClass == Melee || damageClass == Magic;
        }
    }
}
