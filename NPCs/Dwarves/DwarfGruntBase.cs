using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Common;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Dwarves
{
    // Shared behaviour for the corrupted dwarf grunts of the Dwarven Hold: targeting, walking with step-up and
    // jumping over obstacles, facing (art faces RIGHT), line of sight, zone-based spawning, banners, bestiary,
    // hit/death effects and the sprite sheet: 4 walk frames plus an optional 5th attack pose.
    public abstract class DwarfGruntBase : ModNPC
    {
        protected const float JumpSpeed = 6.5f;
        protected const float WalkAcceleration = 0.12f;
        protected const int FrameDelay = 8;

        protected Player Target => Main.player[NPC.target];
        protected int DirToTarget => Target.Center.X > NPC.Center.X ? 1 : -1;
        protected float DistToTarget => Vector2.Distance(Target.Center, NPC.Center);
        protected bool Grounded => NPC.velocity.Y == 0f;
        protected bool CanSeeTarget => Collision.CanHitLine(NPC.position, NPC.width, NPC.height, Target.position, Target.width, Target.height);
        protected bool IsServer => Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient;

        // Relative spawn weight in each hold zone (0 = doesn't spawn there).
        protected abstract float SpawnWeight(int zone);

        protected abstract int BannerItemType { get; }

        // 4 walk frames, plus a 5th attack pose for grunts that have one.
        protected virtual bool HasAttackFrame => true;
        protected virtual bool ShowAttackFrame => false;

        // Dust used when hit (blood for the dwarves, sparks for the mech).
        protected virtual int HitDust => DustID.Blood;
        protected virtual int DeathDustAmount => 25;

        private const int WalkFrames = 4;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = HasAttackFrame ? WalkFrames + 1 : WalkFrames;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                ModContent.GetInstance<DwarvenHoldBiome>().ModBiomeBestiaryInfoElement,
                new FlavorTextBestiaryInfoElement("Mods.Reign_of_the_Ancients.Bestiary." + Name),
            });
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            int amount = NPC.life > 0 ? 4 : DeathDustAmount;
            for (int i = 0; i < amount; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, HitDust, hit.HitDirection * 2f, -1.5f);
                if (Main.rand.NextBool(3))
                {
                    Dust spark = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Iron, hit.HitDirection, -1f);
                    spark.noGravity = true;
                }
            }
            if (NPC.life <= 0)
            {
                OnDeathEffects();
            }
        }

        protected virtual void OnDeathEffects()
        {
        }

        // Telegraph lines etc., drawn behind the sprite.
        protected virtual void DrawTelegraphs()
        {
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            DrawTelegraphs();
            return true;
        }

        protected void CommonDefaults(int width, int height)
        {
            NPC.width = width;
            NPC.height = height;
            NPC.aiStyle = -1;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            SpawnModBiomes = new int[] { ModContent.GetInstance<DwarvenHoldBiome>().Type };
            Banner = Type;
            BannerItem = BannerItemType;
        }

        // Re-targets when needed; returns false (and slows down) if there's nobody to fight.
        protected bool UpdateTarget()
        {
            if (NPC.target < 0 || NPC.target == 255 || !Target.active || Target.dead)
            {
                NPC.TargetClosest();
            }
            if (!Target.active || Target.dead)
            {
                NPC.velocity.X *= 0.9f;
                return false;
            }
            return true;
        }

        protected void Face(int dir)
        {
            NPC.direction = dir;
            NPC.spriteDirection = -dir; // sprite art faces right
        }

        protected void Walk(int dir, float speed)
        {
            NPC.velocity.X = MoveTowards(NPC.velocity.X, dir * speed, WalkAcceleration);
            JumpIfBlocked();
        }

        protected void Brake(float factor = 0.85f)
        {
            NPC.velocity.X *= factor;
        }

        protected void JumpIfBlocked()
        {
            if (NPC.collideX && Grounded)
            {
                NPC.velocity.Y = -JumpSpeed;
            }
        }

        protected void StepUp()
        {
            Collision.StepUp(ref NPC.position, ref NPC.velocity, NPC.width, NPC.height, ref NPC.stepSpeed, ref NPC.gfxOffY);
        }

        protected static float MoveTowards(float current, float target, float step)
        {
            return Math.Abs(target - current) <= step ? target : current + Math.Sign(target - current) * step;
        }

        public override void FindFrame(int frameHeight)
        {
            if (HasAttackFrame && ShowAttackFrame)
            {
                NPC.frame.Y = WalkFrames * frameHeight;
                return;
            }
            if (Math.Abs(NPC.velocity.X) > 0.2f && Grounded)
            {
                NPC.frameCounter += Math.Abs(NPC.velocity.X) / 1.5f;
                if (NPC.frameCounter >= FrameDelay)
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y = (NPC.frame.Y / frameHeight + 1) % WalkFrames * frameHeight;
                }
                else if (NPC.frame.Y >= WalkFrames * frameHeight)
                {
                    NPC.frame.Y = 0;
                }
            }
            else
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return SpawnWeight(DwarvenHoldSpawns.ZoneAtSpawn(spawnInfo));
        }
    }
}
