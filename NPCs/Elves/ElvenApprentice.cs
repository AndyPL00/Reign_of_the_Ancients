using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Reign_of_the_Ancients.Projectiles.Elves;

namespace Reign_of_the_Ancients.NPCs.Elves
{
    public class ElvenApprentice : ModNPC
    {
        private const int FireCooldownTicks = 90;
        private const float DetectRange = 700f;
        private const float PreferredDistance = 300f;
        private const float ProjectileSpeed = 4f;
        private const int FrameDelay = 8;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
        }

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 48;
            NPC.damage = 8;
            NPC.defense = 0;
            NPC.lifeMax = 20;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 60f;
            NPC.knockBackResist = 0.9f;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = false;
        }

        public override void AI()
        {
            if (NPC.target < 0 ||
                NPC.target == 255 ||
                !Main.player[NPC.target].active ||
                Main.player[NPC.target].dead ||
                Vector2.Distance(
                    NPC.Center,
                    Main.player[NPC.target].Center
                ) > DetectRange)
            {
                NPC.TargetClosest();
            }

            Player player = Main.player[NPC.target];

            if (!player.active || player.dead)
            {
                NPC.velocity.Y -= 0.03f;
                NPC.velocity *= 0.98f;
                return;
            }

            Vector2 toPlayer = player.Center - NPC.Center;
            float distance = toPlayer.Length();
            Vector2 dirToPlayer = toPlayer.SafeNormalize(Vector2.Zero);

            NPC.direction = player.Center.X > NPC.Center.X ? -1 : 1;
            NPC.spriteDirection = NPC.direction;

            if (distance > PreferredDistance)
            {
                NPC.velocity = Vector2.Lerp(
                    NPC.velocity,
                    dirToPlayer * 2.5f,
                    0.04f
                );
            }
            else if (distance < PreferredDistance - 60f)
            {
                NPC.velocity = Vector2.Lerp(
                    NPC.velocity,
                    -dirToPlayer * 1.5f,
                    0.04f
                );
            }
            else
            {
                NPC.velocity *= 0.96f;
            }

            NPC.ai[0]++;

            if (NPC.ai[0] >= FireCooldownTicks)
            {
                NPC.ai[0] = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 shootVelocity = dirToPlayer * ProjectileSpeed;

                    int type = ModContent.ProjectileType<ElvenFireball>();

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        shootVelocity,
                        type,
                        8,
                        1f,
                        Main.myPlayer
                    );
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            if (NPC.frameCounter >= FrameDelay)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;

                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return 0f;
        }
    }
}
