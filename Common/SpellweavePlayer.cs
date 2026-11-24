using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Buffs;
using Reign_of_the_Ancients.Items.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Spellblade set bonus "Spellweave" (placeholder name).
    // - Pure melee hits build an arcane charge. When full, the next magic attack costs no mana and deals extra damage.
    //   (The Elven Greatsword's right-click magic slash counts as a magic attack.)
    // - Magic hits (incl. the MeleeMagic slash) grant Spellweave Haste: bonus melee speed for a few seconds.
    // Everything runs on the owning client: hits, mana and shooting are all handled there.
    public class SpellweavePlayer : ModPlayer
    {
        public const int HitsToCharge = 4;
        public const int ChargeHitCooldown = 12;          // ticks; one swing hitting several enemies counts once
        public const float EmpoweredMagicDamage = 0.25f;  // +25% magic damage on the empowered attack
        public const int HasteTicks = 240;                // 4 seconds

        private static readonly Color ChargeColor = new Color(170, 120, 255);

        public bool setActive;
        private int charge;
        private int chargeCooldown;

        public bool Charged => charge >= HitsToCharge;

        public override void ResetEffects()
        {
            setActive = false;
        }

        public override void PostUpdateEquips()
        {
            if (!setActive)
            {
                charge = 0;
                return;
            }

            if (Charged)
            {
                // Applied every frame while charged; the charge is spent in Shoot, after this frame's damage is calculated.
                Player.GetDamage(DamageClass.Magic) += EmpoweredMagicDamage;
                Player.AddBuff(ModContent.BuffType<SpellweaveChargedBuff>(), 2);
            }
        }

        public override void PostUpdate()
        {
            if (chargeCooldown > 0)
            {
                chargeCooldown--;
            }

            if (setActive && Charged && Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.PurpleTorch, 0f, -1f, 100, default, 1.1f);
                d.noGravity = true;
            }
        }

        private bool IsMagicAttack(Item item)
        {
            return item.CountsAsClass(DamageClass.Magic)
                || (item.ModItem is ElvenGreatsword && Player.altFunctionUse == 2);
        }

        public override void ModifyManaCost(Item item, ref float reduce, ref float mult)
        {
            if (setActive && Charged && IsMagicAttack(item))
            {
                mult = 0f;
            }
        }

        public override bool Shoot(Item item, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (setActive && Charged && IsMagicAttack(item))
            {
                charge = 0;
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = 0.3f }, Player.Center);
                for (int i = 0; i < 14; i++)
                {
                    Dust d = Dust.NewDustPerfect(Player.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.4f);
                    d.noGravity = true;
                }
            }
            return true;
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            OnAnyHit(item.DamageType);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            OnAnyHit(proj.DamageType);
        }

        private void OnAnyHit(DamageClass damageClass)
        {
            if (!setActive)
            {
                return;
            }

            // MeleeMagic counts as both classes; treat it as magic so the slash grants haste.
            if (damageClass.CountsAsClass(DamageClass.Magic))
            {
                Player.AddBuff(ModContent.BuffType<SpellweaveHasteBuff>(), HasteTicks);
            }
            else if (damageClass.CountsAsClass(DamageClass.Melee) && !Charged && chargeCooldown <= 0)
            {
                charge++;
                chargeCooldown = ChargeHitCooldown;
                if (Charged)
                {
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.4f }, Player.Center);
                    for (int i = 0; i < 20; i++)
                    {
                        Dust d = Dust.NewDustPerfect(Player.Center, DustID.PurpleTorch, Main.rand.NextVector2CircularEdge(3f, 3f), 100, ChargeColor, 1.3f);
                        d.noGravity = true;
                    }
                }
            }
        }
    }
}
