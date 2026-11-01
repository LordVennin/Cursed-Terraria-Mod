using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Common.Systems
{
	// Recipe groups and a little world generation (Touch Grass in surface chests).
	public class ChaosWorldGen : ModSystem
	{
		public override void AddRecipeGroups() {
			RecipeGroup.RegisterGroup("BagofChaos:TitaniumBar", new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.TitaniumBar)}", ItemID.TitaniumBar, ItemID.AdamantiteBar));
			RecipeGroup.RegisterGroup("BagofChaos:CobaltBar", new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.CobaltBar)}", ItemID.CobaltBar, ItemID.PalladiumBar));
			RecipeGroup.RegisterGroup("BagofChaos:MythrilBar", new RecipeGroup(() => $"{Language.GetTextValue("LegacyMisc.37")} {Lang.GetItemNameValue(ItemID.MythrilBar)}", ItemID.MythrilBar, ItemID.OrichalcumBar));
		}

		public override void PostWorldGen() {
			int touchGrass = ModContent.ItemType<Content.Items.Accessories.TouchGrass>();
			int placed = 0;
			for (int chestIndex = 0; chestIndex < Main.maxChests && placed < 6; chestIndex++) {
				Chest chest = Main.chest[chestIndex];
				if (chest == null)
					continue;
				Tile tile = Main.tile[chest.x, chest.y];
				// Plain wooden chests on the surface.
				if (tile.TileType != TileID.Containers || tile.TileFrameX != 0 || chest.y > Main.worldSurface)
					continue;
				if (!WorldGen.genRand.NextBool(3))
					continue;
				for (int slot = 0; slot < Chest.maxItems; slot++) {
					if (chest.item[slot].type == ItemID.None) {
						chest.item[slot].SetDefaults(touchGrass);
						placed++;
						break;
					}
				}
			}
		}
	}
}
