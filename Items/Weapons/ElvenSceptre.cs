using Reign_of_the_Ancients.Items.Materials;
using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Weapons
{
    // Elven Sceptre (placeholder name). A smaller take on Volodar's beam:
    // aiming line, then a burst beam that can be held for a short time (see ElvenSceptreBeam).
    public class ElvenSceptre : ModItem
    {
        public const int BaseDamage = 26;
        public const int ManaCost = 12;
        public const int UseTimeTicks = 20;
        public const int BarCost = 12;
        public const int EssenceCost = 3;
        public const int CrystalCost = 3;
        public const int CooldownTicks = 90;   // after the beam ends, wait this long before the next cast

        // Game tick until which this sceptre can't be used again (set by ElvenSceptreBeam when it ends).
        public uint cooldownUntil;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            ElvenSceptreBeam.MaxActiveTicks / 60f,
            ElvenSceptreBeam.DrainAmount * 60 / ElvenSceptreBeam.DrainIntervalTicks,
            CooldownTicks / 60f);

        public override void SetStaticDefaults()
        {
            Item.staff[Type] = true; // held pointing at the cursor, like vanilla staves
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 40;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.knockBack = 2f;
            Item.useTime = UseTimeTicks;
            Item.useAnimation = UseTimeTicks;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.channel = true;     // the beam checks player.channel to know if the button is still held
            Item.autoReuse = false;  // after the forced stop you have to click again
            Item.UseSound = SoundID.Item15 with { Pitch = 0.3f, Volume = 0.6f };
            Item.shoot = ModContent.ProjectileType<ElvenSceptreBeam>();
            Item.shootSpeed = 1f;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 1, silver: 50);
        }

        // Only one beam at a time, and not during the cooldown after a beam.
        public override bool CanUseItem(Player player) =>
            player.ownedProjectileCounts[Item.shoot] < 1 && Main.GameUpdateCount >= cooldownUntil;

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<ElvenBar>(BarCost)
                .AddIngredient<ElvenEssence>(EssenceCost)
                .AddIngredient<ArcaneCrystal>(CrystalCost)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
