using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Buffs
{
    // Arcane mark from the Elven Whip: minions deal bonus damage to the tagged enemy, and their hits
    // sometimes burst into homing arcane shards.
    public class ElvenWhipTag : ModBuff
    {
        public const int TagDamage = 6;
        public const int ShardChance = 4;           // 1 in N minion hits
        public const int ShardCount = 3;
        public const float ShardDamageRatio = 0.4f; // of the minion hit's damage
        public const float ShardSpeed = 6f;

        public override string Texture => "Reign_of_the_Ancients/Buffs/GuardiansCrestReadyBuff"; // placeholder, never shown (NPC-only buff)

        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
        }
    }

    public class ElvenWhipTagNPC : GlobalNPC
    {
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (IsTaggedMinionHit(npc, projectile))
            {
                modifiers.FlatBonusDamage += ElvenWhipTag.TagDamage * ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
            }
        }

        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            // Hits are processed on the client that owns the minion, so that client spawns the shards.
            if (!IsTaggedMinionHit(npc, projectile) || projectile.owner != Main.myPlayer || !Main.rand.NextBool(ElvenWhipTag.ShardChance))
            {
                return;
            }

            int damage = System.Math.Max(1, (int)(damageDone * ElvenWhipTag.ShardDamageRatio));
            float start = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < ElvenWhipTag.ShardCount; i++)
            {
                Vector2 velocity = (start + MathHelper.TwoPi * i / ElvenWhipTag.ShardCount).ToRotationVector2() * ElvenWhipTag.ShardSpeed;
                Projectile.NewProjectile(projectile.GetSource_FromThis(), npc.Center, velocity, ModContent.ProjectileType<ArcaneShard>(),
                    damage, 0f, projectile.owner, 0f, 2f); // ai[1] = 2: summon damage
            }
        }

        private static bool IsTaggedMinionHit(NPC npc, Projectile projectile)
        {
            return !projectile.npcProj && !projectile.trap && projectile.IsMinionOrSentryRelated
                && npc.HasBuff(ModContent.BuffType<ElvenWhipTag>());
        }
    }
}
