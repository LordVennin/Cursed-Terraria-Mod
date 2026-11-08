using System.Collections.Generic;
using BagofChaos.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Weapons
{
	// Chaotic weapon boxes: hold the attack button with one and it hands you a random weapon of its class from your hotbar,
	// then swaps to another one every half second (between attacks, never mid-swing). The weapon in hand works exactly as
	// normal, with +10 base damage. Let go and you're back on the box. Never consumed.
	// (Sold by the Chaotic Beholder after the Chaotic Slime is beaten.)
	public abstract class ChaoticWeaponBox : ModItem
	{
		protected abstract DamageClass Class { get; }
		public virtual bool Matches(Item item) => item.damage > 0 && item.CountsAsClass(Class) && !item.accessory;

		public static List<int> Candidates(Player player, ChaoticWeaponBox box) {
			var candidates = new List<int>();
			for (int i = 0; i < 10; i++) {
				Item item = player.inventory[i];
				if (!item.IsAir && item.ModItem is not ChaoticWeaponBox && box.Matches(item))
					candidates.Add(i);
			}
			return candidates;
		}

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
			List<int> candidates = Candidates(player, this);
			if (candidates.Count == 0)
				return false;
			player.GetModPlayer<ChaosPlayer>().StartBoxSession(this, candidates[Main.rand.Next(candidates.Count)]);
			return false; // the weapon in hand does the attacking
		}
	}

	public class ChaoticSwordBox : ChaoticWeaponBox
	{
		protected override DamageClass Class => DamageClass.Melee;
		public override bool Matches(Item item) => base.Matches(item) && item.useStyle == ItemUseStyleID.Swing && item.pick == 0 && item.axe == 0 && item.hammer == 0;
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
		public override bool Matches(Item item) => item.damage > 0 && item.shoot > 0 && ProjectileID.Sets.IsAWhip[item.shoot];
	}
}
