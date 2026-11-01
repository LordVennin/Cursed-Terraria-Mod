using BagofChaos.Common.Players;
using Terraria;
using Terraria.ModLoader;

namespace BagofChaos.Common.GlobalItems
{
	// Weapon Attack Speed for the classes vanilla attack speed doesn't cover: ranged and magic use faster.
	public class ChaosGlobalItem : GlobalItem
	{
		public override float UseTimeMultiplier(Item item, Player player) {
			float speed = player.GetModPlayer<ChaosPlayer>().WeaponAttackSpeed;
			if (speed <= 0f || item.damage <= 0)
				return 1f;
			if (item.DamageType.CountsAsClass(DamageClass.Ranged) || item.DamageType.CountsAsClass(DamageClass.Magic))
				return 1f / (1f + speed);
			return 1f;
		}

		public override float UseAnimationMultiplier(Item item, Player player) => UseTimeMultiplier(item, player);

		// Chaotic Quiver / Ammo Box / Roulette: only the randomly chosen ammo type is allowed for this shot.
		public override bool? CanChooseAmmo(Item weapon, Item ammo, Player player) => player.GetModPlayer<ChaosPlayer>().ChooseAmmoFilter(weapon, ammo);
	}
}
