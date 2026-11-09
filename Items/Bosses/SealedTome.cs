using Microsoft.Xna.Framework;
using Reign_of_the_Ancients.Biomes;
using Reign_of_the_Ancients.Items.Materials;
using Reign_of_the_Ancients.NPCs.Bosses;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Bosses
{
    // Summons Volodar. Only works inside the Mystical Library's throne room.
    public class SealedTome : ModItem
    {
        public const int EssenceCost = 12;
        public const int FallenStarCost = 3;

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 3;
            ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 20;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(silver: 50);
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
        }

        public override bool CanUseItem(Player player)
        {
            Point tile = player.Center.ToTileCoordinates();
            return MysticalLibrary.FloorAt(tile.X, tile.Y) == MysticalLibrary.ThroneRoom
                && !NPC.AnyNPCs(ModContent.NPCType<Volodar>());
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.Roar, player.position);
                int type = ModContent.NPCType<Volodar>();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.SpawnOnPlayer(player.whoAmI, type); // Volodar moves himself to the throne on his first tick
                }
                else
                {
                    NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
                }
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenEssence>(EssenceCost)
                .AddIngredient(ItemID.Book)
                .AddIngredient(ItemID.FallenStar, FallenStarCost)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}
