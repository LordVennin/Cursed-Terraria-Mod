using BagofChaos.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Weapons
{
	// Chaotic weapon boxes: using one picks a random weapon of its class from your hotbar and swings/fires it with +10 base damage.
	// (Sold by the Chaotic Beholder after the Chaotic Slime is beaten.)
	public abstract class ChaoticWeaponBox : ModItem
	{
		protected abstract DamageClass Class { get; }
		protected virtual bool Matches(Item item) => item.damage > 0 && item.CountsAsClass(Class) && !item.accessory;

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 26;
			Item.damage = 10;
			Item.DamageType = Class;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 12;
			Item.useAnimation = 12;
			Item.noMelee = true;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 8);
		}

		public override bool CanUseItem(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return false;
			// Pick a random matching weapon from the hotbar (excluding this box) and switch to it for one attack.
			var candidates = new System.Collections.Generic.List<int>();
			for (int i = 0; i < 10; i++) {
				Item item = player.inventory[i];
				if (!item.IsAir && item.type != Type && item.ModItem is not ChaoticWeaponBox && Matches(item))
					candidates.Add(i);
			}
			if (candidates.Count == 0)
				return false;
			ChaosPlayer chaos = player.GetModPlayer<ChaosPlayer>();
			chaos.BoxReturnSlot = player.selectedItem;
			chaos.BoxBorrowTimer = 2;
			chaos.BoxBonusActive = true;
			player.selectedItem = candidates[Main.rand.Next(candidates.Count)];
			return false; // the borrowed weapon is used instead
		}
	}

	public class ChaoticSwordBox : ChaoticWeaponBox
	{
		protected override DamageClass Class => DamageClass.Melee;
		protected override bool Matches(Item item) => base.Matches(item) && item.useStyle == ItemUseStyleID.Swing && item.pick == 0 && item.axe == 0 && item.hammer == 0;
	}

	public class ChaoticRangerBox : ChaoticWeaponBox
	{
		protected override DamageClass Class => DamageClass.Ranged;
	}

	public class ChaoticMagicBox : ChaoticWeaponBox
	{
		protected override DamageClass Class => DamageClass.Magic;
	}

	public class ChaoticWhipBox : ChaoticWeaponBox
	{
		protected override DamageClass Class => DamageClass.SummonMeleeSpeed;
		protected override bool Matches(Item item) => item.damage > 0 && item.shoot > 0 && ProjectileID.Sets.IsAWhip[item.shoot];
	}
}
