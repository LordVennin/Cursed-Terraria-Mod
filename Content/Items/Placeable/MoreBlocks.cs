using BagofChaos.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Items.Placeable
{
	// Hollow Block: solid for everything except players. Places in mid-air.
	public class HollowBlock : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 100;
		}

		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<HollowBlockTile>());
			Item.width = 16;
			Item.height = 16;
			Item.rare = ItemRarityID.LightRed;
		}

		public override void AddRecipes() {
			CreateRecipe(500).AddIngredient(ItemID.AncientCloth).AddTile(TileID.MythrilAnvil).Register(); // 1 Forbidden Fragment
		}
	}

	// Blue Core Block: solid for everything except enemies.
	public class BlueCoreBlock : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 100;
		}

		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<BlueCoreBlockTile>());
			Item.width = 16;
			Item.height = 16;
			Item.rare = ItemRarityID.LightRed;
		}

		public override void AddRecipes() {
			CreateRecipe(500).AddIngredient(ItemID.FrostCore).AddIngredient(ItemID.Glass, 50).AddTile(TileID.MythrilAnvil).Register();
		}
	}

	// Model Demon Altar: crafts like a demon altar, glows blue, mines like a normal tile, spawns no ores.
	public class ModelDemonAltar : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<ModelDemonAltarTile>());
			Item.width = 28;
			Item.height = 20;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(gold: 1);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient(ItemID.StoneBlock, 20).AddIngredient(ItemID.Lens, 3).AddIngredient(ItemID.Torch, 5).AddTile(TileID.DemonAltar).Register();
		}
	}

	// Chaotic Dehydrator: post-Moon Lord. Any item in, a random item out.
	public class ChaoticDehydrator : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<ChaoticDehydratorTile>());
			Item.width = 32;
			Item.height = 26;
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(gold: 10);
		}

		public override void AddRecipes() {
			CreateRecipe().AddIngredient<AlchemicalDehydrator>().AddIngredient(ItemID.LunarBar, 10).AddIngredient(ItemID.FragmentNebula, 5).AddTile(TileID.LunarCraftingStation).Register();
		}
	}
}
