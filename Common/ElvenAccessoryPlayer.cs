using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Buffs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Common
{
    // Effects of the elven accessories that need more than a stat bonus:
    // - Squire's Bracer: extra melee knockback against enemies that are targeting you.
    // - Guardian's Crest: after CrestChargeTicks without being hit, the next hit is reduced.
    //   ModifyHurt/OnHurt run for the player being hit, so this works in multiplayer.
    // - Archivist's Lantern: treasure close by sparkles (local visual only).
    public class ElvenAccessoryPlayer : ModPlayer
    {
        public const float BracerKnockbackBonus = 0.5f;   // +50% knockback
        public const int CrestChargeTicks = 360;          // 6 seconds without being hit
        public const float CrestDamageReduction = 0.3f;   // next hit takes 30% less damage
        public const int LanternSenseRadius = 10;         // tiles
        public const int LanternScanInterval = 8;         // ticks between treasure scans
        public const int LanternSparkleChance = 3;        // 1 in N treasure tiles sparkle per scan

        public bool squiresBracer;
        public bool guardiansCrest;
        public bool archivistsLantern;

        private int crestTimer;
        private int lanternTimer;

        public bool CrestReady => guardiansCrest && crestTimer >= CrestChargeTicks;

        public override void ResetEffects()
        {
            squiresBracer = false;
            guardiansCrest = false;
            archivistsLantern = false;
        }

        public override void PostUpdateEquips()
        {
            if (!guardiansCrest)
            {
                crestTimer = 0; // swapping the Crest in doesn't give an instant block
                return;
            }

            if (crestTimer < CrestChargeTicks)
            {
                crestTimer++;
                if (crestTimer == CrestChargeTicks && Player.whoAmI == Main.myPlayer)
                {
                    SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.5f }, Player.Center);
                }
            }
            else
            {
                Player.AddBuff(ModContent.BuffType<GuardiansCrestReadyBuff>(), 2);
            }
        }

        public override void PostUpdate()
        {
            if (CrestReady && Main.rand.NextBool(6))
            {
                Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.SilverCoin, 0f, -0.5f, 120, default, 0.9f);
                d.noGravity = true;
            }

            if (archivistsLantern && Player.whoAmI == Main.myPlayer && Main.netMode != NetmodeID.Server
                && ++lanternTimer >= LanternScanInterval)
            {
                lanternTimer = 0;
                SparkleTreasure();
            }
        }

        private void SparkleTreasure()
        {
            Point center = Player.Center.ToTileCoordinates();
            int r = LanternSenseRadius;
            for (int x = center.X - r; x <= center.X + r; x++)
            {
                for (int y = center.Y - r; y <= center.Y + r; y++)
                {
                    if (!WorldGen.InWorld(x, y, 1) || (x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) > r * r)
                    {
                        continue;
                    }
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && Main.tileSpelunker[tile.TileType] && Main.rand.NextBool(LanternSparkleChance))
                    {
                        Dust d = Dust.NewDustDirect(new Vector2(x * 16, y * 16), 16, 16, DustID.TreasureSparkle, 0f, 0f, 150, default, 0.4f);
                        d.noGravity = true;
                        d.velocity *= 0.1f;
                    }
                }
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (CrestReady)
            {
                modifiers.FinalDamage *= 1f - CrestDamageReduction;
            }
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (CrestReady)
            {
                SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.6f }, Player.Center);
                for (int i = 0; i < 16; i++)
                {
                    Dust d = Dust.NewDustPerfect(Player.Center, DustID.SilverCoin, Main.rand.NextVector2CircularEdge(3f, 3f), 100, default, 1.1f);
                    d.noGravity = true;
                }
            }
            crestTimer = 0;
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            ApplyBracer(item.DamageType, target, ref modifiers);
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            ApplyBracer(proj.DamageType, target, ref modifiers);
        }

        private void ApplyBracer(DamageClass damageClass, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (squiresBracer && damageClass.CountsAsClass(DamageClass.Melee) && target.target == Player.whoAmI)
            {
                modifiers.Knockback += BracerKnockbackBonus;
            }
        }
    }
}
