using BagofChaos.Common.Systems;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Consumables
{
	public abstract class Drinkable : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 20;
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 28;
			Item.useStyle = ItemUseStyleID.DrinkLiquid;
			Item.useAnimation = 17;
			Item.useTime = 17;
			Item.useTurn = true;
			Item.UseSound = SoundID.Item3;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.rare = ItemRarityID.White;
			Item.value = Item.buyPrice(silver: 5);
		}
	}

	// It's air. You drink it. What did you expect? Refills your breath underwater. 10 second cooldown. You might drown waiting.
	public class DrinkableAir : Drinkable
	{
		public override bool CanUseItem(Player player) => !player.HasBuff<Winded>(); // drink it whenever. Even on land. Especially on land.

		public override bool? UseItem(Player player) {
			player.breath = player.breathMax;
			player.AddBuff(ModContent.BuffType<Winded>(), 60 * 10);
			for (int i = 0; i < 10; i++)
				Dust.NewDust(player.position, player.width, player.height, DustID.Cloud, 0f, -2f, 150, Color.White, 1.2f);
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Bottle).Register(); // just... bottle it.
		}
	}

	// Kills you instantly. The entire joke.
	public class DrinkableLava : Drinkable
	{
		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			ItemID.Sets.DrinkParticleColors[Type] = [new Color(255, 120, 20), new Color(255, 200, 60), new Color(200, 40, 10)];
		}

		public override void SetDefaults() {
			base.SetDefaults();
			Item.rare = ItemRarityID.Orange;
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				for (int i = 0; i < 30; i++)
					Dust.NewDust(player.position, player.width, player.height, DustID.Torch, 0f, -3f, 0, default, 2f);
				player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.BagofChaos.DeathMessages.DrinkableLava", player.name)), 9999, 0);
			}
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.LavaBucket).Register();
		}
	}

	// Solid Snake: 10 seconds of phasing through blocks. Does not prevent fall damage.
	public class SolidSnake : Drinkable
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.rare = ItemRarityID.Orange;
			Item.buffType = ModContent.BuffType<CardboardBox>();
			Item.buffTime = 60 * 10;
			Item.value = Item.buyPrice(silver: 50);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.BottledWater).AddIngredient(ItemID.Blinkroot).AddRecipeGroup(RecipeGroupID.Wood, 10).AddTile(TileID.Bottles).Register();
		}
	}

	// Admit one. 60 seconds of 3D.
	public class ThreeDMovieTicket : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 5;
		}

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 16;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 20;
			Item.useTime = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.buyPrice(silver: 25);
			Item.buffType = ModContent.BuffType<ThreeDDebuff>();
			Item.buffTime = 60 * 60;
			Item.UseSound = SoundID.Item4;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.SilverCoin, 10).AddIngredient(ItemID.Gel, 3).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Every NPC, player and enemy gets a faded upside-down twin reflected across the line under your feet. For a minute.
	public class WeedyBeer : Drinkable
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.rare = ItemRarityID.Green;
			Item.buffType = ModContent.BuffType<Weedy>();
			Item.buffTime = 60 * 60;
			Item.value = Item.buyPrice(silver: 30);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Ale).AddIngredient(ItemID.Vine).AddIngredient(ItemID.JungleSpores, 2).AddTile(TileID.Bottles).Register();
		}
	}

	// Summons the Delirium event: silhouettes of enemies attack spawn and occasionally swap places with you.
	public class DeliriumPills : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 22;
			Item.useStyle = ItemUseStyleID.EatFood;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.maxStack = 20;
			Item.consumable = true;
			Item.rare = ItemRarityID.LightPurple;
			Item.value = Item.buyPrice(gold: 1);
			Item.UseSound = SoundID.Item2;
		}

		public override bool CanUseItem(Player player) => !ChaosEvents.DeliriumActive;

		public override bool? UseItem(Player player) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChaosEvents.StartDelirium();
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.Deathweed, 3).AddIngredient(ItemID.SoulofNight, 5).AddTile(TileID.Bottles).Register();
		}
	}

	// Summons the Corporate Zombie Invasion. Everyone is let go. Including you.
	public class PinkSlip : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
		}

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 20;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 45;
			Item.useTime = 45;
			Item.maxStack = 20;
			Item.consumable = true;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 2);
			Item.UseSound = SoundID.Roar;
		}

		public override bool CanUseItem(Player player) => !ChaosEvents.InvasionActive && !Main.dayTime; // the zombies only take the night shift

		public override bool? UseItem(Player player) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChaosEvents.StartInvasion();
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Book).AddIngredient(ItemID.PinkGel, 5).AddIngredient(ItemID.ZombieArm).AddTile(TileID.WorkBenches).Register();
		}
	}

	// Starts the Night of Moon Shoes right now (if it's night). Half gravity for every NPC until dawn.
	public class MoonShoes : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
		}

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 18;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.maxStack = 20;
			Item.consumable = true;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.buyPrice(silver: 50);
			Item.UseSound = SoundID.Item4;
		}

		public override bool CanUseItem(Player player) => !Main.dayTime && !ChaosEvents.MoonShoesActive;

		public override bool? UseItem(Player player) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChaosEvents.StartMoonShoes();
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Silk, 10).AddIngredient(ItemID.FallenStar, 3).AddIngredient(ItemID.Cloud, 5).AddTile(TileID.Loom).Register();
		}
	}

	public class SlipperyFrogItem : ModItem
	{
		public override void SetDefaults() {
			Item.CloneDefaults(ItemID.Frog);
			Item.makeNPC = ModContent.NPCType<NPCs.SlipperyFrog>();
			Item.rare = ItemRarityID.Blue;
		}
	}
}
