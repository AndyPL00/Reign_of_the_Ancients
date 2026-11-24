using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Elven Ranger armor effects.
    // - Tunic: chance not to consume ammo.
    // - Set bonus "Arcane Volley" (placeholder name): every Nth ranged shot also fires homing spectral arrows.
    // Shoot only runs on the owning client, so the projectiles are spawned (and synced) by their owner.
    public class ElvenRangerPlayer : ModPlayer
    {
        public const int ShotsPerVolley = 6;
        public const int VolleyArrowCount = 2;
        public const float VolleyDamageRatio = 0.5f;  // of the shot that triggered it
        public const float VolleySpread = 0.25f;       // radians between the spectral arrows
        public const float VolleySpeed = 11f;

        public bool setActive;
        public float ammoSaveChance;
        private int shotCounter;

        public override void ResetEffects()
        {
            setActive = false;
            ammoSaveChance = 0f;
        }

        public override void PostUpdateEquips()
        {
            if (!setActive)
            {
                shotCounter = 0;
            }
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            return !(ammoSaveChance > 0f && Main.rand.NextFloat() < ammoSaveChance);
        }

        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!setActive || !item.CountsAsClass(DamageClass.Ranged))
            {
                return true;
            }

            if (++shotCounter >= ShotsPerVolley)
            {
                shotCounter = 0;
                Vector2 forward = velocity.SafeNormalize(Vector2.UnitX * Player.direction) * VolleySpeed;
                float start = -VolleySpread * (VolleyArrowCount - 1) / 2f;
                for (int i = 0; i < VolleyArrowCount; i++)
                {
                    Projectile.NewProjectile(source, position, forward.RotatedBy(start + VolleySpread * i),
                        ModContent.ProjectileType<ElvenSpectralArrow>(), (int)(damage * VolleyDamageRatio), knockback * 0.5f, Player.whoAmI);
                }
            }
            return true;
        }
    }
}
