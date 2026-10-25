using BagofChaos.Common.Systems;
using BagofChaos.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Placeable
{
	public class AlchemicalDehydrator : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<AlchemicalDehydratorTile>());
			Item.width = 32;
			Item.height = 26;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(gold: 1);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Glass, 10).AddIngredient(ItemID.Bottle, 3).AddRecipeGroup(RecipeGroupID.IronBar, 5).AddTile(TileID.WorkBenches).Register();
		}
	}

	public class NullGravityBeacon : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<NullGravityBeaconTile>());
			Item.width = 20;
			Item.height = 30;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(gold: 3);
		}

		public override void AddRecipes() {
			CreateRecipe().AddRecipeGroup(RecipeGroupID.IronBar, 10).AddIngredient(ItemID.FallenStar, 5).AddIngredient(ItemID.GravitationPotion).AddTile(TileID.Anvils).Register();
		}
	}

	public abstract class WeatherBottle : ModItem
	{
		protected abstract int Style { get; }

		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<WeatherBeaconTile>(), Style);
			Item.width = 20;
			Item.height = 28;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 1);
		}
	}

	public class RainInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleRain;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.RainCloud, 10).AddTile(TileID.WorkBenches).Register();
	}

	public class StormInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleStorm;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.RainCloud, 20).AddIngredient(ItemID.Wire, 5).AddTile(TileID.WorkBenches).Register();
	}

	public class BlizzardInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleBlizzard;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.SnowCloudBlock, 10).AddIngredient(ItemID.IceBlock, 10).AddTile(TileID.WorkBenches).Register();
	}

	public class SunshineInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleSunshine;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.Daybloom, 5).AddIngredient(ItemID.Sunflower).AddTile(TileID.WorkBenches).Register();
	}

	public class SandstormInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleSandstorm;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.SandBlock, 30).AddIngredient(ItemID.Cactus, 5).AddTile(TileID.WorkBenches).Register();
	}

	public class JellyStormInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleJellyStorm;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.Gel, 30).AddIngredient(ItemID.PinkGel, 5).AddTile(TileID.WorkBenches).Register();
	}

	public class FrogRainInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleFrogRain;
		public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle).AddIngredient(ItemID.Frog).AddIngredient(ItemID.IceBlock, 10).AddTile(TileID.WorkBenches).Register();
	}
}
