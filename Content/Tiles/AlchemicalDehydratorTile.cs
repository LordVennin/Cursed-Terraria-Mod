using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BagofChaos.Content.Tiles
{
	// Crafting station. Feed it living things, get a pouch of mystery powder. 3x2.
	public class AlchemicalDehydratorTile : ModTile
	{
		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = true;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.CoordinateHeights = [16, 18];
			TileObjectData.addTile(Type);

			AdjTiles = [TileID.Bottles]; // also counts as an alchemy station, because why not
			DustType = DustID.Glass;
			AddMapEntry(new Color(150, 90, 200), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.5f;
			g = 0.2f;
			b = 0.7f;
		}

		public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
	}
}
