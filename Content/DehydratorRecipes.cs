using System;
using System.Collections.Generic;
using BagofChaos.Content.Items.Consumables;
using BagofChaos.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content
{
	// The Alchemical Dehydrator accepts living plants and creatures that contain water and aren't already potion ingredients.
	// Output is a pouch of Dehydrated Mystery Powder (which turns into a random ingredient when opened).
	// Gold critters, dye plants and quest fish are rare and give 5-30 bonus pouches.
	public class DehydratorRecipes : ModSystem
	{
		private static readonly int[] Fish = {
			ItemID.Bass, ItemID.Trout, ItemID.Salmon, ItemID.AtlanticCod, ItemID.Tuna, ItemID.RedSnapper, ItemID.NeonTetra, ItemID.ArmoredCavefish,
			ItemID.Damselfish, ItemID.CrimsonTigerfish, ItemID.FrostMinnow, ItemID.PrincessFish, ItemID.GoldenCarp, ItemID.SpecularFish, ItemID.Prismite,
			ItemID.VariegatedLardfish, ItemID.FlarefinKoi, ItemID.DoubleCod, ItemID.Honeyfin, ItemID.Obsidifish, ItemID.Shrimp, ItemID.ChaosFish,
			ItemID.Ebonkoi, ItemID.Hemopiranha, ItemID.Rockfish, ItemID.Stinkfish, ItemID.Flounder, ItemID.RockLobster,
		};

		private static readonly int[] DyePlants = {
			ItemID.TealMushroom, ItemID.GreenMushroom, ItemID.SkyBlueFlower, ItemID.YellowMarigold, ItemID.BlueBerries, ItemID.LimeKelp,
			ItemID.PinkPricklyPear, ItemID.OrangeBloodroot, ItemID.RedHusk, ItemID.CyanHusk, ItemID.VioletHusk, ItemID.PurpleMucos, ItemID.BlackInk,
			ItemID.StrangePlant1, ItemID.StrangePlant2, ItemID.StrangePlant3, ItemID.StrangePlant4,
		};

		private static readonly int[] Plants = {
			ItemID.Sunflower, ItemID.Cactus, ItemID.Pumpkin, ItemID.Vine, ItemID.JungleSpores, ItemID.Hay, ItemID.Acorn, ItemID.Coral,
		};

		// Already potion ingredients: not accepted.
		private static readonly HashSet<int> Excluded = new() {
			ItemID.Daybloom, ItemID.Moonglow, ItemID.Blinkroot, ItemID.Deathweed, ItemID.Waterleaf, ItemID.Fireblossom, ItemID.Shiverthorn,
			ItemID.Mushroom, ItemID.GlowingMushroom, ItemID.VileMushroom, ItemID.ViciousMushroom, ItemID.Cactus, ItemID.Coral, ItemID.JungleSpores,
		};

		public override void AddRecipes() {
			int powder = ModContent.ItemType<DehydratedMysteryPowder>();
			int station = ModContent.TileType<AlchemicalDehydratorTile>();
			var added = new HashSet<int>();

			void Add(int inputType, bool rare) {
				if (inputType <= 0 || Excluded.Contains(inputType) || !added.Add(inputType))
					return;
				// Deterministic "random" bonus so every client and the server agree on the recipe list.
				int amount = rare ? 1 + 5 + new Random(inputType * 7919).Next(0, 26) : 1;
				Recipe.Create(powder, amount).AddIngredient(inputType).AddTile(station).Register();
			}

			// Every critter item (anything that spawns an NPC when used), gold ones are rare. Quest fish are rare too.
			for (int type = 1; type < ItemLoader.ItemCount; type++) {
				Item item = ContentSamples.ItemsByType.TryGetValue(type, out Item sample) ? sample : new Item(type);
				if (item.makeNPC > 0)
					Add(type, item.Name.Contains("Gold", StringComparison.OrdinalIgnoreCase));
				else if (item.questItem && item.uniqueStack)
					Add(type, rare: true);
			}
			foreach (int type in Fish) Add(type, false);
			foreach (int type in DyePlants) Add(type, true);
			foreach (int type in Plants) Add(type, false);
		}
	}
}
