using Reign_of_the_Ancients.Common;
using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Armor
{
    // Elven Ranger set (placeholder name), ranged. Built for Volodar's Bow.
    // The hood owns the set check and the Arcane Volley set bonus (logic in Common/ElvenRangerPlayer).
    [AutoloadEquip(EquipType.Head)]
    public class ElvenRangerHood : ModItem
    {
        public const int ArmorDefense = 6;
        public const int RangedDamageBonusPercent = 8;
        public const int BarCost = 10;
        public const int CrystalCost = 4;
        public const int EssenceCost = 3;

        public static LocalizedText SetBonusText { get; private set; }

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(RangedDamageBonusPercent);

        public override void SetStaticDefaults()
        {
            SetBonusText = this.GetLocalization("SetBonus").WithFormatArgs(
                ElvenRangerPlayer.ShotsPerVolley,
                ElvenRangerPlayer.VolleyArrowCount,
                (int)(ElvenRangerPlayer.VolleyDamageRatio * 100));
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 20;
            Item.defense = ArmorDefense;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1, silver: 50);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += RangedDamageBonusPercent / 100f;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ElvenRangerTunic>() && legs.type == ModContent.ItemType<ElvenRangerLeggings>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = SetBonusText.Value;
            player.GetModPlayer<ElvenRangerPlayer>().setActive = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenBar>(BarCost)
                .AddIngredient<ArcaneCrystal>(CrystalCost)
                .AddIngredient<ElvenEssence>(EssenceCost)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
