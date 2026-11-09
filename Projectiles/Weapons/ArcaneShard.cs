using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Small homing arcane shard, released by Volodar's Bow's arcane arrow and Volodar's Grimoire's orb.
    // ai[1] = 1 makes it magic damage (Grimoire); otherwise ranged (Bow).
    public class ArcaneShard : ModProjectile
    {
        private const int Lifetime = 60;
        private const int HomingDelay = 8;       // flies straight briefly before seeking
        private const float HomingRange = 400f;
        private const float HomingTurn = 0.12f;
        private const float Speed = 10f;

        private static readonly Color ShardColor = new Color(190, 130, 255);

        public override string Texture => "Reign_of_the_Ancients/Projectiles/Bosses/VolodarOrb"; // placeholder art

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.ai[1] == 1f)
            {
                Projectile.DamageType = DamageClass.Magic;
            }

            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > HomingDelay)
            {
                NPC target = FindTarget();
                if (target != null)
                {
                    Vector2 wanted = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * Speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, HomingTurn);
                }
            }

            Projectile.rotation += 0.3f;
            Lighting.AddLight(Projectile.Center, ShardColor.ToVector3() * 0.5f);
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Vector2.Zero, 100, default, 0.9f);
                d.noGravity = true;
            }
        }

        private NPC FindTarget()
        {
            NPC best = null;
            float bestDistance = HomingRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                {
                    continue;
                }
                float distance = Vector2.Distance(npc.Center, Projectile.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = npc;
                }
            }
            return best;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 4; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, Main.rand.NextVector2Circular(2f, 2f), 100, default, 1f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() / 2f;
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float fade = 1f - (i + 1f) / (Projectile.oldPos.Length + 1f);
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, pos, null, (ShardColor * 0.5f * fade) with { A = 0 }, 0f, origin, 0.5f * fade, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, ShardColor with { A = 0 }, Projectile.rotation, origin, 0.55f, SpriteEffects.None);
            return false;
        }
    }
}
