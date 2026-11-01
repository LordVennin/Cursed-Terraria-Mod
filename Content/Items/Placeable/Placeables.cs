using BagofChaos.Common.Systems;
using BagofChaos.Content.Tiles;
using Microsoft.Xna.Framework;
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

	public abstract class WeatherBottle : ModItem
	{
		protected abstract int Style { get; }
		protected abstract int WeatherBlock { get; }
		protected virtual int WeatherBlockCount => 10;

		public override string Texture => "Terraria/Images/Item_" + ItemID.CrystalBall;

		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<WeatherBeaconTile>(), Style);
			Item.width = 22;
			Item.height = 22;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 1);
		}

		private Color Tint => WeatherBeaconTile.StyleColors[Style];

		public override bool PreDrawInInventory(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Vector2 position, Microsoft.Xna.Framework.Rectangle frame, Microsoft.Xna.Framework.Color drawColor, Microsoft.Xna.Framework.Color itemColor, Microsoft.Xna.Framework.Vector2 origin, float scale) {
			spriteBatch.Draw(Terraria.GameContent.TextureAssets.Item[Type].Value, position, frame, drawColor.MultiplyRGB(Tint), 0f, origin, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
			return false;
		}

		public override bool PreDrawInWorld(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Microsoft.Xna.Framework.Color lightColor, Microsoft.Xna.Framework.Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
			var texture = Terraria.GameContent.TextureAssets.Item[Type].Value;
			spriteBatch.Draw(texture, Item.Center - Main.screenPosition, null, lightColor.MultiplyRGB(Tint), rotation, texture.Size() * 0.5f, scale, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
			return false;
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.Glass, 10).AddIngredient(ItemID.LifeCrystal).AddIngredient(ItemID.Cloud, 10).AddIngredient(WeatherBlock, WeatherBlockCount).AddTile(TileID.WorkBenches).Register();
		}
	}

	public class RainInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleRain;
		protected override int WeatherBlock => ItemID.RainCloud;
	}

	public class StormInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleStorm;
		protected override int WeatherBlock => ItemID.RainCloud;
		protected override int WeatherBlockCount => 25;
	}

	public class BlizzardInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleBlizzard;
		protected override int WeatherBlock => ItemID.SnowCloudBlock;
	}

	public class SunshineInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleSunshine;
		protected override int WeatherBlock => ItemID.Hay;
	}

	public class SandstormInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleSandstorm;
		protected override int WeatherBlock => ItemID.SandBlock;
		protected override int WeatherBlockCount => 30;
	}

	public class JellyStormInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleJellyStorm;
		protected override int WeatherBlock => ItemID.SlimeBlock;
	}

	public class FrogRainInABottle : WeatherBottle
	{
		protected override int Style => WeatherBeaconSystem.StyleFrogRain;
		protected override int WeatherBlock => ItemID.IceBlock;
	}
}
