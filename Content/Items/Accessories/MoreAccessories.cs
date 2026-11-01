using System;
using BagofChaos.Common.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Accessories
{
	// ---------------------------------------------------------------------------------------------------------------
	// Triforce pieces: convert damage types. Hardmode, pre-mech.
	// ---------------------------------------------------------------------------------------------------------------
	public abstract class TriforcePiece : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.LightRed;
		protected override int Value => Item.buyPrice(gold: 5);
	}

	public class TriforceOfWisdom : TriforcePiece
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TriforceMode = DamageConversion.Magic;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SoulofLight, 10).AddIngredient(ItemID.Sapphire, 3).AddIngredient(ItemID.CrystalShard, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public class TriforceOfPower : TriforcePiece
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TriforceMode = DamageConversion.Melee;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SoulofNight, 10).AddIngredient(ItemID.Ruby, 3).AddIngredient(ItemID.CrystalShard, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public class TriforceOfCourage : TriforcePiece
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TriforceMode = DamageConversion.Ranged;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SoulofFlight, 10).AddIngredient(ItemID.Emerald, 3).AddIngredient(ItemID.CrystalShard, 10).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// All vanilla damage bonuses apply to every class; right click the item to pick which class everything becomes.
	public class CompleteTriforceOfHyrule : TriforcePiece
	{
		protected override int Rarity => ItemRarityID.Yellow;

		public override void UpdateAccessory(Player player, bool hideVisual) {
			ChaosPlayer chaos = Chaos(player);
			chaos.TriforceMode = chaos.HyruleMode;
			chaos.CompleteTriforce = true;
		}

		public override bool CanRightClick() => true;

		public override bool ConsumeItem(Player player) => false;

		public override void RightClick(Player player) {
			ChaosPlayer chaos = Chaos(player);
			chaos.HyruleMode = chaos.HyruleMode switch {
				DamageConversion.Magic => DamageConversion.Melee,
				DamageConversion.Melee => DamageConversion.Ranged,
				DamageConversion.Ranged => DamageConversion.Summon,
				_ => DamageConversion.Magic,
			};
			Color color = chaos.HyruleMode switch { DamageConversion.Magic => Color.DeepSkyBlue, DamageConversion.Melee => Color.OrangeRed, DamageConversion.Ranged => Color.LimeGreen, _ => Color.Gold };
			Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.Items.CompleteTriforceOfHyrule.Mode" + chaos.HyruleMode), color);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<TriforceOfWisdom>().AddIngredient<TriforceOfPower>().AddIngredient<TriforceOfCourage>().AddTile(TileID.MythrilAnvil).Register();
		}
	}

	public enum DamageConversion
	{
		None,
		Magic,
		Melee,
		Ranged,
		Summon,
	}

	// ---------------------------------------------------------------------------------------------------------------
	// Stalemate: nobody hurts anybody. Debuffs, traps and the environment still count.
	// ---------------------------------------------------------------------------------------------------------------
	public class Stalemate : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Pink;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).Stalemate = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.HallowedBar, 10).AddIngredient(ItemID.SoulofSight, 5).AddIngredient(ItemID.SoulofMight, 5).AddIngredient(ItemID.SoulofFright, 5).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Loot Scanner: enemies tagged by whips drop double loot.
	public class LootScanner : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.LightRed;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).LootScanner = true;

		public override void AddRecipes() {
			CreateRecipe().AddRecipeGroup("BagofChaos:TitaniumBar", 10).AddIngredient(ItemID.Wire, 20).AddIngredient(ItemID.Lens, 2).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Touch Grass: +5 life regen while standing on grass. Found in surface chests.
	public class TouchGrass : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).TouchGrass = true;
	}

	// Slippery Frog Shoes: your Y position never changes. Walk on air. Never go up or down again.
	public class SlipperyFrogShoes : ChaosAccessory
	{
		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).FrogShoes = true;
	}

	// N-Terraria: inverts every colour on screen. Cursed. Cosmetic.
	public class NTerraria : ChaosAccessory
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.vanity = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).NTerraria = true;

		public override void UpdateVanity(Player player) => Chaos(player).NTerraria = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.NegativeDye, 2).AddIngredient(ItemID.Glass, 5).AddTile(TileID.DyeVat).Register();
		}
	}

	// Unstable Concoction: potions don't heal; while sick you can't die and get random power; when it wears off you explode. And die.
	public class UnstableConcoction : ChaosAccessory
	{
		protected override int Rarity => ItemRarityID.Pink;

		public override void UpdateAccessory(Player player, bool hideVisual) => Chaos(player).UnstableConcoction = true;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.GreaterHealingPotion, 5).AddIngredient(ItemID.Bottle).AddIngredient(ItemID.Dynamite, 3).AddIngredient(ItemID.SoulofNight, 5).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Team Tether: a grappling hook you can't aim. It hooks low-health allies and heals them.
	public class TeamTether : ModItem
	{
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.GrapplingHook);
			Item.shoot = ProjectileID.Hook;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 5);
		}

		public override bool CanUseItem(Player player) => false; // automatic only

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.GrapplingHook).AddIngredient(ItemID.HealingPotion, 10).AddIngredient(ItemID.Chain, 10).AddTile(TileID.Anvils).Register();
		}
	}

	// Balls of Spiders: wings. Constantly drop spiders while flying; cling to walls.
	[AutoloadEquip(EquipType.Wings)]
	public class BallsOfSpiders : ModItem
	{
		public override void SetStaticDefaults() {
			ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(100, 6.25f, 1f); // same as Leaf Wings
			ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<Pets.SpiderOnABallItem>();
		}

		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 20;
			Item.value = Item.buyPrice(gold: 10);
			Item.rare = ItemRarityID.LightRed;
			Item.accessory = true;
		}

		public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend) {
			ascentWhenFalling = 0.85f;
			ascentWhenRising = 0.15f;
			maxCanAscendMultiplier = 1f;
			maxAscentMultiplier = 2.5f;
			constantAscend = 0.125f;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			ChaosPlayer chaos = player.GetModPlayer<ChaosPlayer>();
			chaos.SpiderWings = true;
		}
	}
}
