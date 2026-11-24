using Reign_of_the_Ancients.Projectiles.Pets;
using Terraria;
using Terraria.ModLoader;

namespace Reign_of_the_Ancients.Buffs
{
    public class TinySealedTomeBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.vanityPet[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            bool unused = false;
            player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, ModContent.ProjectileType<TinySealedTomePet>());
        }
    }
}
