using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reign_of_the_Ancients.Common.Visuals;
using Reign_of_the_Ancients.Projectiles.Bosses.Titan;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Bosses
{
    // One of the Dwarven Titan's two arm cannons (placeholder name). A separate, optional target with its own health:
    // it rides on its shoulder pylon, aims at the Titan's target and lobs explosive shells during the cannon barrage.
    // Destroying it removes that side's shells and makes the Titan angrier (DwarvenTitan.Rage).
    //   ai[0] = the Titan's whoAmI, ai[1] = side (-1 left pylon, +1 right pylon).
    public class DwarvenTitanCannon : ModNPC
    {
        private const int BaseLife = 900;
        private const int BaseDefense = 18;
        private const int ContactDamage = 20;

        private const float PivotToMuzzle = 56f;       // matches the sprite: shoulder ball at (14, 20), muzzle at x 70
        private const float AimTurnRate = 0.12f;
        private const float RestAngle = 0.35f;         // barrel droops outward-down between barrages
        private const float LeadTicks = 18f;           // aims a little ahead of a moving target
        private const float ShotSpacing = 56f;         // successive shells land this far apart around the target
        private const float ShellMaxSpeed = 17f;
        private const int RecoilTicks = 8;
        private const float RecoilDistance = 8f;
        private const float AimLineLength = 520f;
        private static readonly Color AimLineColor = new Color(255, 150, 60);

        private ref float Parent => ref NPC.ai[0];
        private ref float Side => ref NPC.ai[1];
        private ref float Aim => ref NPC.localAI[0];    // barrel angle, worked out on every client
        private ref float Recoil => ref NPC.localAI[1];

        private Vector2 Muzzle => NPC.Center + Aim.ToRotationVector2() * PivotToMuzzle;

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
        }

        public override void SetDefaults()
        {
            NPC.width = 44;
            NPC.height = 44;
            NPC.damage = ContactDamage;
            NPC.defense = BaseDefense;
            NPC.lifeMax = BaseLife;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 0f;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.npcSlots = 1f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Aim = Side > 0 ? RestAngle : MathHelper.Pi - RestAngle;
        }

        public override void AI()
        {
            NPC titan = Main.npc[(int)Parent];
            if (!titan.active || titan.type != ModContent.NPCType<DwarvenTitan>())
            {
                NPC.active = false;
                return;
            }

            int side = Side > 0 ? 1 : -1;
            NPC.Center = DwarvenTitan.ArmAnchor(titan, side);
            NPC.velocity = titan.velocity;
            NPC.target = titan.target;
            NPC.dontTakeDamage = titan.dontTakeDamage;
            NPC.alpha = titan.alpha;
            if (Recoil > 0f)
            {
                Recoil--;
            }

            Player target = Main.player[NPC.target];
            bool barrage = titan.ai[0] == DwarvenTitan.StateCannons;
            int tick = (int)titan.ai[1];
            float desired = barrage ? (target.Center + target.velocity * LeadTicks - NPC.Center).ToRotation()
                                    : (side > 0 ? RestAngle : MathHelper.Pi - RestAngle);
            Aim = Aim.AngleLerp(desired, AimTurnRate);

            if (barrage && tick >= DwarvenTitan.CannonWindupTicks)
            {
                int sinceStart = tick - DwarvenTitan.CannonWindupTicks;
                int shot = sinceStart / DwarvenTitan.CannonGapTicks;
                if (sinceStart % DwarvenTitan.CannonGapTicks == 0 && shot < DwarvenTitan.CannonShots)
                {
                    Fire(target, shot);
                }
            }
        }

        // Lobs a shell that comes down around the target after ShellFlightTicks.
        private void Fire(Player target, int shot)
        {
            Vector2 muzzle = Muzzle;
            SoundEngine.PlaySound(SoundID.Item62 with { Pitch = 0.2f, Volume = 0.8f }, muzzle);
            Recoil = RecoilTicks;
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 10; i++)
                {
                    Dust smoke = Dust.NewDustPerfect(muzzle, DustID.Smoke, Aim.ToRotationVector2().RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 4f), 100, default, 1.6f);
                    smoke.noGravity = true;
                }
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            float spread = (shot - (DwarvenTitan.CannonShots - 1) / 2f) * ShotSpacing * (Side > 0 ? 1f : -1f);
            Vector2 landing = target.Center + target.velocity * LeadTicks + new Vector2(spread, 0f);
            Vector2 delta = landing - muzzle;
            float t = DwarvenTitan.ShellFlightTicks;
            Vector2 velocity = new Vector2(delta.X / t, delta.Y / t - 0.5f * TitanShell.Gravity * t);
            if (velocity.Length() > ShellMaxSpeed)
            {
                velocity = velocity.SafeNormalize(Vector2.UnitY) * ShellMaxSpeed;
            }
            Projectile.NewProjectile(NPC.GetSource_FromAI(), muzzle, velocity, ModContent.ProjectileType<TitanShell>(),
                DwarvenTitan.ShellDamage, 3f, Main.myPlayer);
        }

        public override bool CheckActive() => false;

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                Dust spark = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Torch, hit.HitDirection * 2f, -2f, 100, default, 1.1f);
                spark.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                // The arm is blown off its pylon.
                SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
                for (int i = 0; i < 30; i++)
                {
                    Dust smoke = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, 0f, 100, default, 2f);
                    smoke.velocity *= 2.5f;
                    Dust fire = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, 0f, 100, default, 2.2f);
                    fire.noGravity = true;
                    fire.velocity *= 3f;
                }
                for (int i = 0; i < 3; i++)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.Center - new Vector2(16f), Main.rand.NextVector2Circular(3f, 3f), GoreID.Smoke1 + Main.rand.Next(3));
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            NPC titan = Main.npc[(int)Parent];
            if (titan.active && titan.ai[0] == DwarvenTitan.StateCannons && titan.ai[1] < DwarvenTitan.CannonWindupTicks)
            {
                BeamRenderer.DrawTelegraph(Muzzle, Aim, AimLineLength, 1.4f, AimLineColor, titan.ai[1] / DwarvenTitan.CannonWindupTicks, 0.85f);
            }

            Texture2D texture = TextureAssets.Npc[Type].Value;
            Vector2 origin = new Vector2(14f, texture.Height / 2f);
            SpriteEffects effects = Math.Cos(Aim) < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            Vector2 kick = -Aim.ToRotationVector2() * RecoilDistance * (Recoil / RecoilTicks);
            spriteBatch.Draw(texture, NPC.Center + kick - screenPos, null, NPC.GetAlpha(drawColor), Aim, origin, NPC.scale, effects, 0f);
            return false;
        }
    }
}
