using System.Collections.Generic;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common.GlobalTiles
{
	public class ChaosGlobalTile : GlobalTile
	{
		// Crushed Glass Potion: glass- and sand-related tiles are not drawn for the drinker. Rendering only:
		// collision, wiring, interaction and lighting are untouched.
		public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch) {
			if (CrushedPotionSystem.LocalCrushedGlass && CrushedPotionSystem.InvisibleTile(type))
				return false;
			return true;
		}

		// Crushed Dirt Potion: dirt glows brown, as bright as a torch (CrushedPotionSystem makes vanilla ask us).
		public override void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b) {
			if (type == TileID.Dirt && CrushedPotionSystem.LocalCrushedDirt) {
				r = 1f;
				g = 0.7f;
				b = 0.35f;
			}
		}
	}

	public class ChaosGlobalWall : GlobalWall
	{
		public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch) {
			if (CrushedPotionSystem.LocalCrushedGlass && CrushedPotionSystem.InvisibleWall(type))
				return false;
			return true;
		}
	}

	public class CrushedPotionSystem : ModSystem
	{
		public static bool LocalCrushedGlass, LocalCrushedDirt;
		private static bool[] invisibleTiles, invisibleWalls;

		public static bool InvisibleTile(int type) => invisibleTiles != null && type < invisibleTiles.Length && invisibleTiles[type];
		public static bool InvisibleWall(int type) => invisibleWalls != null && type < invisibleWalls.Length && invisibleWalls[type];

		private static readonly int[] ExplicitTiles = {
			TileID.Glass, TileID.Sand, TileID.Ebonsand, TileID.Crimsand, TileID.Pearlsand, TileID.HardenedSand, TileID.CorruptHardenedSand,
			TileID.CrimsonHardenedSand, TileID.HallowHardenedSand, TileID.Sandstone, TileID.CorruptSandstone, TileID.CrimsonSandstone,
			TileID.HallowSandstone, TileID.SandstoneBrick, TileID.SandStoneSlab, TileID.Bottles, TileID.GlassKiln,
			TileID.AmethystGemspark, TileID.TopazGemspark, TileID.SapphireGemspark, TileID.EmeraldGemspark, TileID.RubyGemspark,
			TileID.DiamondGemspark, TileID.AmberGemspark, TileID.AmethystGemsparkOff, TileID.TopazGemsparkOff, TileID.SapphireGemsparkOff,
			TileID.EmeraldGemsparkOff, TileID.RubyGemsparkOff, TileID.DiamondGemsparkOff, TileID.AmberGemsparkOff,
		};

		private static readonly int[] ExplicitWalls = {
			WallID.Glass, WallID.Sandstone, WallID.HardenedSand, WallID.SandstoneBrick, WallID.SmoothSandstone,
			WallID.CorruptSandstone, WallID.CrimsonSandstone, WallID.HallowSandstone, WallID.CorruptHardenedSand, WallID.CrimsonHardenedSand,
			WallID.HallowHardenedSand,
		};

		private static readonly int[] SeedItems = {
			ItemID.Glass, ItemID.SandBlock, ItemID.EbonsandBlock, ItemID.CrimsandBlock, ItemID.PearlsandBlock, ItemID.HardenedSand,
			ItemID.CorruptHardenedSand, ItemID.CrimsonHardenedSand, ItemID.HallowHardenedSand, ItemID.Sandstone, ItemID.CorruptSandstone,
			ItemID.CrimsonSandstone, ItemID.HallowSandstone,
		};

		// Built once after recipes exist. Deliberately overinclusive: anything placeable whose recipe uses glass or sand,
		// or uses something that was itself made from glass or sand, goes invisible too. Lamps included.
		public override void PostAddRecipes() {
			invisibleTiles = new bool[TileLoader.TileCount];
			invisibleWalls = new bool[WallLoader.WallCount];
			foreach (int t in ExplicitTiles) invisibleTiles[t] = true;
			foreach (int w in ExplicitWalls) invisibleWalls[w] = true;
			for (int t = 0; t < TileLoader.TileCount; t++) {
				if (TileID.Sets.Conversion.Sand[t] || TileID.Sets.Conversion.Sandstone[t] || TileID.Sets.Conversion.HardenedSand[t])
					invisibleTiles[t] = true;
			}

			var related = new HashSet<int>(SeedItems);
			for (int depth = 0; depth < 2; depth++) {
				var found = new List<int>();
				for (int r = 0; r < Recipe.numRecipes; r++) {
					Recipe recipe = Main.recipe[r];
					if (recipe?.createItem == null || recipe.createItem.IsAir || related.Contains(recipe.createItem.type))
						continue;
					if (UsesRelated(recipe, related))
						found.Add(recipe.createItem.type);
				}
				related.UnionWith(found);
			}
			foreach (int itemType in related) {
				if (!ContentSamples.ItemsByType.TryGetValue(itemType, out Item item))
					continue;
				if (item.createTile >= 0 && item.createTile < invisibleTiles.Length)
					invisibleTiles[item.createTile] = true;
				if (item.createWall > 0 && item.createWall < invisibleWalls.Length)
					invisibleWalls[item.createWall] = true;
			}
		}

		private static bool UsesRelated(Recipe recipe, HashSet<int> related) {
			foreach (Item ingredient in recipe.requiredItem) {
				if (related.Contains(ingredient.type))
					return true;
			}
			foreach (int group in recipe.acceptedGroups) {
				if (RecipeGroup.recipeGroups.TryGetValue(group, out RecipeGroup g)) {
					foreach (int valid in g.ValidItems) {
						if (related.Contains(valid))
							return true;
					}
				}
			}
			return false;
		}

		public override void PostUpdatePlayers() {
			if (Main.dedServ)
				return;
			LocalCrushedGlass = !Main.gameMenu && Main.LocalPlayer.HasBuff<CrushedGlass>();
			LocalCrushedDirt = !Main.gameMenu && Main.LocalPlayer.HasBuff<CrushedDirt>();
			// Vanilla only asks tiles marked as light sources for light; dirt is one only while the buff lasts.
			Main.tileLighted[TileID.Dirt] = LocalCrushedDirt;
		}

		public override void OnWorldUnload() {
			LocalCrushedGlass = LocalCrushedDirt = false;
			Main.tileLighted[TileID.Dirt] = false;
		}
	}
}
