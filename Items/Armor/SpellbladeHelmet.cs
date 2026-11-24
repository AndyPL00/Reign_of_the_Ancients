using Reign_of_the_Ancients.Buffs;
using Reign_of_the_Ancients.Common;
using Reign_of_the_Ancients.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Armor
{
    // Spellblade set (placeholder name), melee + magic. Melee and magic bonuses also apply to MeleeMagicDamageClass.
    // The helmet owns the set check and the Spellweave set bonus (logic in Common/SpellweavePlayer).
    [AutoloadEquip(EquipType.Head)]
    public class SpellbladeHelmet : ModItem
    {
        public const int ArmorDefense = 8;
        public const int DamageBonusPercent = 8;  // melee and magic
        public const int MaxManaBonus = 40;
        public const int BarCost = 10;
        public const int EssenceCost = 3;

        public static LocalizedText SetBonusText { get; private set; }

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageBonusPercent, MaxManaBonus);

        public override void SetStaticDefaults()
        {
            SetBonusText = this.GetLocalization("SetBonus").WithFormatArgs(
                SpellweavePlayer.HitsToCharge,
                (int)(SpellweavePlayer.EmpoweredMagicDamage * 100),
                (int)(SpellweaveHasteBuff.MeleeSpeedBonus * 100),
                SpellweavePlayer.HasteTicks / 60);
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
            player.GetDamage(DamageClass.Melee) += DamageBonusPercent / 100f;
            player.GetDamage(DamageClass.Magic) += DamageBonusPercent / 100f;
            player.statManaMax2 += MaxManaBonus;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<SpellbladeBreastplate>() && legs.type == ModContent.ItemType<SpellbladeGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = SetBonusText.Value;
            player.GetModPlayer<SpellweavePlayer>().setActive = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenBar>(BarCost)
                .AddIngredient<ElvenEssence>(EssenceCost)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
