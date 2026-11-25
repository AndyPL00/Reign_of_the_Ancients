using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Common.Visuals;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses.Titan
{
    // A floor vent during the Titan's steam attack (phase 2). It hisses and puffs (harmless) for ai[1] ticks,
    // then erupts in a tall column of scalding steam. ai[0] = the floor's Y (pixels); the column stands on it.
    public class TitanSteamVent : ModProjectile
    {
        private const int ColumnWidth = 36;
        private const int ColumnHeight = 240;
        private const int EruptTicks = 45;
        private const int RiseTicks = 6;
        private static readonly Color SteamOuter = new Color(170, 180, 190);
        private static readonly Color SteamCore = new Color(235, 240, 245);
        private static readonly Color WarningColor = new Color(255, 200, 150);

        // Drawn entirely with BeamRenderer and dust; the vanilla texture is only a stand-in for the loader.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flames;

        private ref float Age => ref Projectile.localAI[0];
        private int Delay => (int)Projectile.ai[1];
        private bool Erupting => Age >= Delay && Age < Delay + EruptTicks;

        public override void SetDefaults()
        {
            Projectile.width = ColumnWidth;
            Projectile.height = ColumnHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            Projectile.Bottom = new Vector2(Projectile.Center.X, Projectile.ai[0]);
            Age++;
            if (Age == Delay)
            {
                SoundEngine.PlaySound(SoundID.Item34 with { Pitch = -0.4f, Volume = 0.7f }, Projectile.Bottom);
            }
            if (Age >= Delay + EruptTicks)
            {
                Projectile.Kill();
                return;
            }
            if (Main.dedServ)
            {
                return;
            }
            if (Age < Delay)
            {
                // Warning: small puffs from the floor, more as it is about to blow.
                if (Main.rand.NextFloat() < 0.15f + 0.6f * Age / Delay)
                {
                    Dust puff = Dust.NewDustDirect(Projectile.BottomLeft - new Vector2(0f, 6f), Projectile.width, 6, DustID.Smoke, 0f, -1.5f, 150, default, 1.2f);
                    puff.noGravity = true;
                }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust steam = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, -6f, 120, default, 2f);
                    steam.noGravity = true;
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Erupting ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            if (Age < Delay)
            {
                BeamRenderer.DrawTelegraph(Projectile.Bottom, -MathHelper.PiOver2, 48f, 3f, WarningColor, Age / Delay, 0.7f);
                return false;
            }
            float since = Age - Delay;
            float reach = MathHelper.Clamp(since / RiseTicks, 0f, 1f);
            float opacity = since > EruptTicks - 10 ? (EruptTicks - since) / 10f : 1f;
            BeamRenderer.DrawBeam(Projectile.Bottom, -MathHelper.PiOver2, ColumnHeight * reach, ColumnWidth * 0.5f, SteamOuter, SteamCore, opacity * 0.8f, true, false);
            return false;
        }
    }

    // One burst of the Titan's flamethrower: a growing ball of fire that flies a short way and burns out.
    public class TitanFlame : ModProjectile
    {
        private const int LifeTicks = 38;

        // Drawn with glows and dust; the vanilla texture is only a stand-in for the loader.
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Flames;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifeTicks;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            float age = 1f - Projectile.timeLeft / (float)LifeTicks;
            Projectile.scale = 0.8f + age * 1.6f;
            Projectile.velocity *= 0.985f;
            Lighting.AddLight(Projectile.Center, 0.9f * (1f - age), 0.45f * (1f - age), 0.1f);
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                Dust fire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Torch,
                    Projectile.velocity.X * 0.4f, Projectile.velocity.Y * 0.4f, 100, default, 1.6f * Projectile.scale);
                fire.noGravity = true;
            }
            if (age > 0.6f && Main.rand.NextBool(3))
            {
                Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, -1f, 120, default, 1.4f);
                smoke.noGravity = true;
            }
        }

        // The flame grows as it travels: hit area follows the scale.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle grown = projHitbox;
            int grow = (int)(10f * (Projectile.scale - 1f));
            grown.Inflate(grow, grow);
            return grown.Intersects(targetHitbox);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float age = 1f - Projectile.timeLeft / (float)LifeTicks;
            Vector2 s = Projectile.Center - Main.screenPosition;
            BeamRenderer.Glow(s, 40f * Projectile.scale, new Color(255, 120, 30) * (0.7f * (1f - age)));
            BeamRenderer.Glow(s, 18f * Projectile.scale, new Color(255, 220, 140) * (0.8f * (1f - age)));
            return false;
        }
    }
}
