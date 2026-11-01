using System;
using System.Collections.Generic;
using BagofChaos.Common.Players;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Consumables
{
	public abstract class PermanentBuffItem : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 20;
			Item.useStyle = ItemUseStyleID.EatFood;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 1);
			Item.UseSound = SoundID.Item2;
		}
	}

	// Ball of Spiders: eat it, never get stuck in cobwebs again. Shimmer it into wings.
	public class BallOfSpiders : PermanentBuffItem
	{
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<Accessories.BallsOfSpiders>();
		}

		public override bool CanUseItem(Player player) => !player.GetModPlayer<ChaosPlayer>().ImmuneToCobwebs;

		public override bool? UseItem(Player player) {
			player.GetModPlayer<ChaosPlayer>().ImmuneToCobwebs = true;
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SpiderFang, 15).AddIngredient(ItemID.Cobweb, 50).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Ball of Confetti: permanent buff, you explode confetti when hurt. Shimmer it into Balls of Confetti.
	public class BallOfConfetti : PermanentBuffItem
	{
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			ItemID.Sets.ShimmerTransformToItem[Type] = ModContent.ItemType<Weapons.BallsOfConfetti>();
		}

		public override bool CanUseItem(Player player) => !player.GetModPlayer<ChaosPlayer>().ConfettiOnHurt;

		public override bool? UseItem(Player player) {
			player.GetModPlayer<ChaosPlayer>().ConfettiOnHurt = true;
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Confetti, 10).AddIngredient(ItemID.PinkGel, 5).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Ball of Glue: removes the confetti buff. Eat another Ball of Confetti to get it back.
	public class BallOfGlue : PermanentBuffItem
	{
		public override bool CanUseItem(Player player) => player.GetModPlayer<ChaosPlayer>().ConfettiOnHurt;

		public override bool? UseItem(Player player) {
			player.GetModPlayer<ChaosPlayer>().ConfettiOnHurt = false;
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Gel, 30).AddIngredient(ItemID.Bottle).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Pull the Lever, Kronk!: an instant hellevator.
	public class PullTheLeverKronk : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 28;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 40;
			Item.useTime = 40;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 1);
			Item.UseSound = SoundID.Unlock;
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI != Main.myPlayer)
				return true;
			int centerX = (int)(player.Center.X / 16f);
			int startY = (int)(player.Bottom.Y / 16f);
			int endY = Main.maxTilesY - 180; // down into the underworld
			for (int y = startY; y < endY; y++) {
				for (int x = centerX - 1; x <= centerX + 1; x++) {
					if (!WorldGen.InWorld(x, y, 10))
						continue;
					Tile tile = Main.tile[x, y];
					if (tile.HasTile && !TileID.Sets.BasicChest[tile.TileType] && tile.TileType != TileID.LihzahrdBrick)
						WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: true);
					if (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava)
						tile.LiquidAmount = 0;
				}
			}
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				for (int y = startY; y < endY; y += 30)
					NetMessage.SendTileSquare(-1, centerX - 2, y, 5, Math.Min(30, endY - y));
			}
			Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.Items.PullTheLeverKronk.Used"), Color.Orange);
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Lever).AddIngredient(ItemID.Dynamite, 5).AddIngredient(ItemID.Rope, 50).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Crushed Glass Potion: glass and sand blocks become invisible to you for 15 minutes.
	public class CrushedGlassPotion : Drinkable
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.rare = ItemRarityID.Blue;
			Item.buffType = ModContent.BuffType<CrushedGlass>();
			Item.buffTime = 60 * 60 * 15;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.Glass, 5).AddIngredient(ItemID.SandBlock, 5).AddTile(TileID.Bottles).Register();
		}
	}

	// Crushed Dirt Potion: dirt glows unreasonably bright for 5 minutes.
	public class CrushedDirtPotion : Drinkable
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.rare = ItemRarityID.Blue;
			Item.buffType = ModContent.BuffType<CrushedDirt>();
			Item.buffTime = 60 * 60 * 5;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.DirtBlock, 10).AddIngredient(ItemID.Moonglow).AddIngredient(ItemID.MudBlock, 5).AddTile(TileID.Bottles).Register();
		}
	}

	// Compressed crystals: max out the stat, then act as an infinite potion.
	public abstract class CompressedStat : ModItem
	{
		protected abstract bool IsMaxed(Player player);
		protected abstract void MaxOut(Player player);
		protected abstract int HealLife { get; }
		protected abstract int HealMana { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 24;
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item3;
			Item.maxStack = 1;
			Item.consumable = true;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(gold: 10);
			Item.potion = HealLife > 0;
			Item.healLife = HealLife;
			Item.healMana = HealMana;
		}

		public override bool CanUseItem(Player player) {
			if (IsMaxed(player))
				return !(HealLife > 0 && player.HasBuff(BuffID.PotionSickness));
			return true;
		}

		public override bool? UseItem(Player player) {
			if (!IsMaxed(player)) {
				MaxOut(player);
				SoundEngine.PlaySound(SoundID.Item4, player.Center);
			}
			return true;
		}

		public override bool ConsumeItem(Player player) => !IsMaxed(player); // once maxed it is an infinite potion
	}

	public class CompressedLifeCrystal : CompressedStat
	{
		protected override bool IsMaxed(Player player) => player.ConsumedLifeCrystals >= Player.LifeCrystalMax;
		protected override void MaxOut(Player player) {
			while (player.ConsumedLifeCrystals < Player.LifeCrystalMax)
				player.UseHealthMaxIncreasingItem(20);
		}
		protected override int HealLife => 50; // Lesser Healing Potion
		protected override int HealMana => 0;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.LifeCrystal, 15).AddTile(TileID.Anvils).Register();
		}
	}

	public class CompressedManaCrystal : CompressedStat
	{
		protected override bool IsMaxed(Player player) => player.ConsumedManaCrystals >= Player.ManaCrystalMax;
		protected override void MaxOut(Player player) {
			while (player.ConsumedManaCrystals < Player.ManaCrystalMax)
				player.UseManaMaxIncreasingItem(20);
		}
		protected override int HealLife => 0;
		protected override int HealMana => 50; // Lesser Mana Potion

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.ManaCrystal, 9).AddTile(TileID.Anvils).Register();
		}
	}

	public class CompressedLifeFruit : CompressedStat
	{
		protected override bool IsMaxed(Player player) => player.ConsumedLifeCrystals >= Player.LifeCrystalMax && player.ConsumedLifeFruit >= Player.LifeFruitMax;
		protected override void MaxOut(Player player) {
			while (player.ConsumedLifeCrystals < Player.LifeCrystalMax)
				player.UseHealthMaxIncreasingItem(20);
			while (player.ConsumedLifeFruit < Player.LifeFruitMax)
				player.UseHealthMaxIncreasingItem(5);
		}
		protected override int HealLife => 150; // Greater Healing Potion
		protected override int HealMana => 0;

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<CompressedLifeCrystal>().AddIngredient(ItemID.LifeFruit, 20).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Rat Package Co.: unlocks an extra inventory row (ten slots under the inventory).
	public class RatPackageCo : PermanentBuffItem
	{
		public override bool CanUseItem(Player player) => !player.GetModPlayer<ChaosPlayer>().ExtraRowUnlocked;

		public override bool? UseItem(Player player) {
			player.GetModPlayer<ChaosPlayer>().ExtraRowUnlocked = true;
			return true;
		}
	}

	// Bottled enemies: thrown, spawn ten random enemies on impact. 0.1% chance of a boss you've already beaten.
	public abstract class BottledEnemies : ModItem
	{
		protected abstract bool Hardmode { get; }
		protected abstract bool Biomed { get; }

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 5;
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 28;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 20;
			Item.useTime = 20;
			Item.noUseGraphic = true;
			Item.noMelee = true;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.rare = Hardmode ? ItemRarityID.LightRed : ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 1);
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<Projectiles.BottledEnemiesProjectile>();
			Item.shootSpeed = 9f;
		}

		public override bool CanUseItem(Player player) => !Hardmode || Main.hardMode;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, position, velocity, type, 0, 0f, player.whoAmI, Hardmode ? 1f : 0f, Biomed ? 1f : 0f);
			return false;
		}
	}

	public class BottledPrehardmode : BottledEnemies
	{
		protected override bool Hardmode => false;
		protected override bool Biomed => false;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.Lens, 3).AddIngredient(ItemID.RottenChunk, 5).AddTile(TileID.Bottles).Register();
	}

	public class BottledHardmode : BottledEnemies
	{
		protected override bool Hardmode => true;
		protected override bool Biomed => false;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.SoulofNight, 3).AddIngredient(ItemID.SoulofLight, 3).AddTile(TileID.Bottles).Register();
	}

	public class BiomedBottledPrehardmode : BottledEnemies
	{
		protected override bool Hardmode => false;
		protected override bool Biomed => true;
		public override void AddRecipes() => CreateRecipe().AddIngredient<BottledPrehardmode>().AddIngredient(ItemID.Daybloom).AddIngredient(ItemID.Moonglow).AddTile(TileID.Bottles).Register();
	}

	public class BiomedBottledHardmode : BottledEnemies
	{
		protected override bool Hardmode => true;
		protected override bool Biomed => true;
		public override void AddRecipes() => CreateRecipe().AddIngredient<BottledHardmode>().AddIngredient(ItemID.Daybloom).AddIngredient(ItemID.Moonglow).AddTile(TileID.Bottles).Register();
	}
}
