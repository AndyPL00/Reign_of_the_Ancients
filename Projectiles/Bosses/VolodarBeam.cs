using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Common.Visuals;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Projectiles.Bosses
{
    // Volodar's sweeping beam. Follows him, shows a thin warning line, then sweeps through an arc.
    // ai[0] = Volodar's NPC index, ai[1] = starting angle, ai[2] = sweep direction (+1 / -1).
    public class VolodarBeam : ModProjectile
    {
        public const int TelegraphTicks = 35;
        public const int SweepTicks = 70;
        public const int FadeTicks = 10;
        public const int TotalTicks = TelegraphTicks + SweepTicks + FadeTicks;
        public static readonly float SweepArc = MathHelper.ToRadians(110f);

        private const float MaxLength = 1600f;
        private const float Width = 24f;
        private const float TelegraphWidth = 2.5f;
        private const float ExtendTicks = 4f;      // the beam shoots out to full length over this many ticks

        private static readonly Color CoreColor = new Color(205, 140, 255);
        private static readonly Color OuterColor = new Color(130, 60, 255);

        // Uses the arrow sprite only as a placeholder; the beam is drawn with plain rectangles.
        public override string Texture => "Reign_of_the_Ancients/Projectiles/Bosses/VolodarArrow";

        private int Age => TotalTicks - Projectile.timeLeft;
        private bool Sweeping => Age >= TelegraphTicks && Age < TelegraphTicks + SweepTicks;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            NPC owner = Main.npc[(int)Projectile.ai[0]];
            if (!owner.active || owner.type != ModContent.NPCType<NPCs.Bosses.Volodar>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            float sweep = MathHelper.Clamp((Age - TelegraphTicks) / (float)SweepTicks, 0f, 1f);
            Projectile.rotation = Projectile.ai[1] + SweepArc * Projectile.ai[2] * sweep;
            Projectile.localAI[1] = BeamLength();

            if (Age == TelegraphTicks)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Pitch = -0.3f }, Projectile.Center);
            }

            if (Sweeping)
            {
                Vector2 dir = Projectile.rotation.ToRotationVector2();
                Vector2 impact = Projectile.Center + dir * Projectile.localAI[1];
                Dust spark = Dust.NewDustPerfect(impact, DustID.PurpleTorch, (-dir).RotatedByRandom(1.1f) * Main.rand.NextFloat(2f, 6f), 100, default, 1.4f);
                spark.noGravity = true;
                for (float d = 0f; d < Projectile.localAI[1]; d += 64f)
                {
                    Lighting.AddLight(Projectile.Center + dir * d, 0.5f, 0.3f, 0.9f);
                }
            }
        }

        // Stops at the first solid block.
        private float BeamLength()
        {
            Vector2 dir = Projectile.rotation.ToRotationVector2();
            for (float d = 0f; d < MaxLength; d += 8f)
            {
                Point tile = (Projectile.Center + dir * d).ToTileCoordinates();
                if (WorldGen.SolidTile(tile.X, tile.Y))
                {
                    return d;
                }
            }
            return MaxLength;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Sweeping)
            {
                return false;
            }
            Vector2 end = Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.localAI[1];
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, Width, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float length = Projectile.localAI[1];
            if (Age < TelegraphTicks)
            {
                BeamRenderer.DrawTelegraph(Projectile.Center, Projectile.rotation, length, TelegraphWidth, CoreColor, Age / (float)TelegraphTicks);
                return false;
            }

            float fired = Age - TelegraphTicks;
            float reach = MathHelper.Clamp((fired + 1f) / ExtendTicks, 0f, 1f);
            float widthMult = BeamRenderer.FireEnvelope(fired);
            float opacity = 1f;
            if (!Sweeping)
            {
                float t = (fired - SweepTicks) / (float)FadeTicks;
                widthMult *= BeamRenderer.CloseEnvelope(t);
                opacity = 1f - t * 0.5f;
            }
            BeamRenderer.DrawBeam(Projectile.Center, Projectile.rotation, length * reach, Width * widthMult, OuterColor, CoreColor, opacity);
            return false;
        }
    }
}
