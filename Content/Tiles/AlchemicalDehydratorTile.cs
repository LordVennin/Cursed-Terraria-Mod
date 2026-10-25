using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BagofChaos.Content.Tiles
{
	// Works like an Extractinator: hold an eligible living thing and right click the machine to dehydrate one of them
	// into a random potion ingredient. Gold critters, dye plants and quest fish are rare and yield 6-31 ingredients. 3x2.
	public class AlchemicalDehydratorTile : ModTile
	{
		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = true;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;
			TileID.Sets.HasOutlines[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.CoordinateHeights = [16, 18];
			TileObjectData.addTile(Type);

			DustType = DustID.Glass;
			AddMapEntry(new Color(150, 90, 200), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.5f;
			g = 0.2f;
			b = 0.7f;
		}

		public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

		public override bool HasSmartInteract(int i, int j, Terraria.GameContent.ObjectInteractions.SmartInteractScanSettings settings) => true;

		public override void MouseOver(int i, int j) {
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			if (DehydratorSystem.IsEligible(player.HeldItem.type)) {
				player.cursorItemIconID = player.HeldItem.type;
			}
			else {
				player.cursorItemIconID = ModContent.ItemType<Items.Placeable.AlchemicalDehydrator>();
			}
		}

		public override bool RightClick(int i, int j) {
			Player player = Main.LocalPlayer;
			Item held = player.HeldItem;
			if (held.IsAir || !DehydratorSystem.IsEligible(held.type)) {
				Main.NewText(Language.GetTextValue("Mods.BagofChaos.Dehydrator.NotEligible"), new Color(200, 160, 255));
				return true;
			}
			int amount = DehydratorSystem.IsRare(held.type) ? 6 + Main.rand.Next(0, 26) : 1;
			int result = DehydratorSystem.RandomIngredient();
			held.stack--;
			if (held.stack <= 0)
				held.TurnToAir();
			player.QuickSpawnItem(player.GetSource_TileInteraction(i, j), result, amount);
			SoundEngine.PlaySound(SoundID.Item3, new Vector2(i * 16f, j * 16f));
			for (int d = 0; d < 10; d++) {
				Dust dust = Dust.NewDustDirect(new Vector2(i * 16f - 16f, j * 16f - 8f), 48, 24, DustID.PurpleTorch, 0f, -1f);
				dust.noGravity = true;
			}
			return true;
		}
	}

	// Which items the dehydrator accepts and what it spits out.
	public class DehydratorSystem : ModSystem
	{
		private static readonly HashSet<int> eligible = new();
		private static readonly HashSet<int> rare = new();

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
			ItemID.Sunflower, ItemID.Pumpkin, ItemID.Vine, ItemID.Hay, ItemID.Acorn,
		};

		// Already potion ingredients: not accepted as input, but they're what comes out.
		private static readonly int[] Ingredients = {
			ItemID.Daybloom, ItemID.Moonglow, ItemID.Blinkroot, ItemID.Deathweed, ItemID.Waterleaf, ItemID.Fireblossom, ItemID.Shiverthorn,
			ItemID.Mushroom, ItemID.GlowingMushroom, ItemID.VileMushroom, ItemID.ViciousMushroom, ItemID.Lens, ItemID.SharkFin, ItemID.Feather,
			ItemID.AntlionMandible, ItemID.Cactus, ItemID.WormTooth, ItemID.RottenChunk, ItemID.Vertebrae, ItemID.Bone, ItemID.Coral, ItemID.Gel,
			ItemID.PinkGel, ItemID.Stinger, ItemID.JungleSpores, ItemID.Obsidian, ItemID.BottledWater, ItemID.Cloud, ItemID.RainCloud,
			ItemID.FallenStar, ItemID.Hellstone,
		};

		public override void PostSetupContent() {
			eligible.Clear();
			rare.Clear();
			var excluded = new HashSet<int>(Ingredients);
			for (int type = 1; type < ItemLoader.ItemCount; type++) {
				Item item = ContentSamples.ItemsByType.TryGetValue(type, out Item sample) ? sample : new Item(type);
				if (item.makeNPC > 0 && !excluded.Contains(type)) {
					eligible.Add(type);
					if (item.Name.Contains("Gold", StringComparison.OrdinalIgnoreCase))
						rare.Add(type);
				}
				else if (item.questItem && item.uniqueStack) {
					eligible.Add(type);
					rare.Add(type);
				}
			}
			foreach (int type in Fish) eligible.Add(type);
			foreach (int type in Plants) if (!excluded.Contains(type)) eligible.Add(type);
			foreach (int type in DyePlants) { eligible.Add(type); rare.Add(type); }
		}

		public static bool IsEligible(int type) => eligible.Contains(type);
		public static bool IsRare(int type) => rare.Contains(type);
		public static int RandomIngredient() => Ingredients[Main.rand.Next(Ingredients.Length)];
	}
}
