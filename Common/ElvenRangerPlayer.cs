using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Elven ranged effects (all spawn ElvenSpectralArrow).
    // - Elven Ranger Tunic: chance not to consume ammo.
    // - Set bonus "Arcane Volley" (placeholder name): every Nth ranged shot also fires homing spectral arrows.
    // - Elven Quiver: its own counter, every Nth ranged shot fires one spectral arrow. Stacks with the set bonus.
    // Shoot only runs on the owning client, so the projectiles are spawned (and synced) by their owner.
    public class ElvenRangerPlayer : ModPlayer
    {
        public const int ShotsPerVolley = 6;
        public const int VolleyArrowCount = 2;
        public const float VolleyDamageRatio = 0.5f;   // of the shot that triggered it
        public const float VolleySpread = 0.25f;       // radians between the spectral arrows
        public const float VolleySpeed = 11f;

        public const int QuiverShotsPerArrow = 3;
        public const float QuiverDamageRatio = 0.45f;
        public const float QuiverSpread = 0.08f;       // small random angle so it doesn't overlap the shot

        public bool setActive;
        public bool elvenQuiver;
        public float ammoSaveChance;
        private int volleyCounter;
        private int quiverCounter;

        public override void ResetEffects()
        {
            setActive = false;
            elvenQuiver = false;
            ammoSaveChance = 0f;
        }

        public override void PostUpdateEquips()
        {
            if (!setActive)
            {
                volleyCounter = 0;
            }
            if (!elvenQuiver)
            {
                quiverCounter = 0;
            }
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo)
        {
            return !(ammoSaveChance > 0f && Main.rand.NextFloat() < ammoSaveChance);
        }

        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (!item.CountsAsClass(DamageClass.Ranged))
            {
                return true;
            }

            Vector2 forward = velocity.SafeNormalize(Vector2.UnitX * Player.direction) * VolleySpeed;

            if (setActive && ++volleyCounter >= ShotsPerVolley)
            {
                volleyCounter = 0;
                float start = -VolleySpread * (VolleyArrowCount - 1) / 2f;
                for (int i = 0; i < VolleyArrowCount; i++)
                {
                    SpawnArrow(source, position, forward.RotatedBy(start + VolleySpread * i), damage * VolleyDamageRatio, knockback);
                }
            }

            if (elvenQuiver && ++quiverCounter >= QuiverShotsPerArrow)
            {
                quiverCounter = 0;
                SpawnArrow(source, position, forward.RotatedBy(Main.rand.NextFloat(-QuiverSpread, QuiverSpread)), damage * QuiverDamageRatio, knockback);
            }
            return true;
        }

        private void SpawnArrow(EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, float damage, float knockback)
        {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ElvenSpectralArrow>(),
                (int)damage, knockback * 0.5f, Player.whoAmI);
        }
    }
}
