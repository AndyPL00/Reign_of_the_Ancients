using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Weapons
{
    // Homing spectral arrow from the Elven Ranger set bonus (Arcane Volley).
    public class ElvenSpectralArrow : ModProjectile
    {
        private const int Lifetime = 90;
        private const int HomingDelay = 10;      // flies straight briefly before seeking
        private const float HomingRange = 500f;
        private const float HomingTurn = 0.1f;
        private const float Speed = 11f;

        private static readonly Color ArrowColor = new Color(120, 230, 255);

        public override string Texture => "Reign_of_the_Ancients/Projectiles/Bosses/VolodarArrow"; // placeholder art

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
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
            Projectile.alpha = 60;
        }

        public override void AI()
        {
            if (++Projectile.localAI[0] > HomingDelay)
            {
                NPC target = FindTarget();
                if (target != null)
                {
                    Vector2 wanted = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * Speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, HomingTurn);
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, ArrowColor.ToVector3() * 0.5f);
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, -Projectile.velocity * 0.2f, 100, default, 1f);
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
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f, Pitch = 0.4f }, Projectile.Center);
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.IceTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, 1.1f);
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
                Main.EntitySpriteDraw(texture, pos, null, (ArrowColor * 0.4f * fade) with { A = 0 }, Projectile.rotation, origin, 0.9f, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, ArrowColor with { A = 0 }, Projectile.rotation, origin, 0.9f, SpriteEffects.None);
            return false;
        }
    }
}
