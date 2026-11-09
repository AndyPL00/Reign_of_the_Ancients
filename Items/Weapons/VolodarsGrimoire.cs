using Reign_of_the_Ancients.Projectiles.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Items.Weapons
{
    // Magic tome dropped by Volodar: casts a slow arcane orb that explodes and scatters homing shards.
    public class VolodarsGrimoire : ModItem
    {
        public const int BaseDamage = 34;
        public const int ManaCost = 14;
        public const int UseTimeTicks = 30;
        public const float OrbSpeed = 7f;

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Magic;
            Item.mana = ManaCost;
            Item.knockBack = 4f;
            Item.useTime = UseTimeTicks;
            Item.useAnimation = UseTimeTicks;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item117;
            Item.shoot = ModContent.ProjectileType<ArcaneOrb>();
            Item.shootSpeed = OrbSpeed;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 3);
        }
    }
}
