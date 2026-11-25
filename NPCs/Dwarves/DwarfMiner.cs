using System;
using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Tiles.DwarvenHold;
using Reign_of_the_Ancients.Tiles.Library;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.NPCs.Dwarves
{
    // Corrupted Miner (placeholder name). Walks straight at you and digs through anything in the way:
    // blocks in front, under its feet if you're below, overhead if you're above. Then swings its pickaxe.
    // It never digs hold/library blocks, dungeon or temple bricks, furniture, or very hard blocks.
    public class DwarfMiner : DwarfGruntBase
    {
        private const float WalkSpeed = 1.6f;
        private const int DigInterval = 14;            // ticks per block dug
        private const float DigDownWhenBelow = 3 * 16f;
        private const float DigUpWhenAbove = 5 * 16f;
        private const float SwingRange = 44f;
        private const int SwingWindupTicks = 18;
        private const int SwingLungeTicks = 10;
        private const float SwingLungeSpeed = 3f;
        private const int SwingCooldownTicks = 40;

        private const float StateWalk = 0f;
        private const float StateWindup = 1f;
        private const float StateLunge = 2f;

        private ref float State => ref NPC.ai[0];
        private ref float Timer => ref NPC.ai[1];
        private ref float DigTimer => ref NPC.ai[2];
        private ref float SwingCooldown => ref NPC.ai[3];

        protected override float SpawnWeight(int zone) => zone == DwarvenHold.Mines ? 1.2f : 0f;

        public override void SetDefaults()
        {
            CommonDefaults(26, 40);
            NPC.damage = 30;
            NPC.defense = 12;
            NPC.lifeMax = 130;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 160f;
            NPC.knockBackResist = 0.35f;
        }

        public override void AI()
        {
            if (!UpdateTarget())
            {
                return;
            }
            int dir = DirToTarget;
            Vector2 toTarget = Target.Center - NPC.Center;
            Timer++;
            if (SwingCooldown > 0f)
            {
                SwingCooldown--;
            }

            switch (State)
            {
                case StateWindup:
                    Brake(0.7f);
                    if (Timer >= SwingWindupTicks)
                    {
                        State = StateLunge;
                        Timer = 0f;
                        NPC.velocity.X = NPC.direction * SwingLungeSpeed;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
                    }
                    break;

                case StateLunge:
                    if (Timer >= SwingLungeTicks)
                    {
                        State = StateWalk;
                        Timer = 0f;
                        SwingCooldown = SwingCooldownTicks;
                    }
                    break;

                default:
                    Face(dir);
                    NPC.velocity.X = MoveTowards(NPC.velocity.X, dir * WalkSpeed, WalkAcceleration);
                    if (Math.Abs(toTarget.X) < SwingRange && Math.Abs(toTarget.Y) < 40f && SwingCooldown <= 0f && IsServer)
                    {
                        State = StateWindup;
                        Timer = 0f;
                        NPC.netUpdate = true;
                    }
                    if (IsServer && ++DigTimer >= DigInterval)
                    {
                        DigTimer = 0f;
                        Dig(toTarget);
                    }
                    break;
            }
            StepUp();
        }

        // Digs one block per call, in the direction it needs to go. Jumps if it can't dig through.
        private void Dig(Vector2 toTarget)
        {
            int left = (int)(NPC.position.X / 16f);
            int right = (int)((NPC.position.X + NPC.width - 1) / 16f);
            int top = (int)(NPC.position.Y / 16f);
            int bottom = (int)((NPC.position.Y + NPC.height - 1) / 16f);

            if (Grounded && toTarget.Y > DigDownWhenBelow && Math.Abs(toTarget.X) < 6 * 16f)
            {
                for (int x = left; x <= right; x++)
                {
                    if (TryDig(x, bottom + 1))
                    {
                        return;
                    }
                }
            }
            if (Grounded && toTarget.Y < -DigUpWhenAbove && Math.Abs(toTarget.X) < 6 * 16f)
            {
                for (int x = left; x <= right; x++)
                {
                    if (TryDig(x, top - 1))
                    {
                        return;
                    }
                }
            }
            if (NPC.collideX || Math.Abs(NPC.velocity.X) < 0.1f)
            {
                int front = NPC.direction > 0 ? right + 1 : left - 1;
                for (int y = top; y <= bottom; y++)
                {
                    if (TryDig(front, y))
                    {
                        return;
                    }
                }
                JumpIfBlocked();
            }
        }

        private bool TryDig(int x, int y)
        {
            if (!CanDig(x, y))
            {
                return false;
            }
            WorldGen.KillTile(x, y);
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, x, y);
            }
            return true;
        }

        private static bool CanDig(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 5))
            {
                return false;
            }
            Tile tile = Main.tile[x, y];
            if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType] || Main.tileFrameImportant[tile.TileType]
                || Main.tileDungeon[tile.TileType] || IsTooHard(tile.TileType))
            {
                return false;
            }
            ModTile modTile = TileLoader.GetTile(tile.TileType);
            if (modTile is HoldBlockBase || modTile is LibraryBlockBase || (modTile != null && modTile.MinPick > 65))
            {
                return false;
            }
            return WorldGen.CanKillTile(x, y);
        }

        private static bool IsTooHard(int type) => type switch
        {
            TileID.Obsidian or TileID.Hellstone or TileID.LihzahrdBrick or TileID.Chlorophyte
                or TileID.Cobalt or TileID.Palladium or TileID.Mythril or TileID.Orichalcum or TileID.Adamantite or TileID.Titanium => true,
            _ => false,
        };
    }
}
