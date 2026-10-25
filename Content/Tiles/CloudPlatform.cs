using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BagofChaos.Content.Tiles
{
	// Sky in a Bottle's temporary cloud. A real one-way platform: jump through it, hold Down to drop through it.
	public class CloudPlatform : ModTile
	{
		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileSolidTop[Type] = true;
			Main.tileSolid[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileTable[Type] = true;
			TileID.Sets.Platforms[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;
			TileID.Sets.DisableSmartInteract[Type] = true;
			TileID.Sets.CanBeClearedDuringGeneration[Type] = true;

			AddMapEntry(new Color(235, 235, 245));

			DustType = DustID.Cloud;
			AdjTiles = [TileID.Platforms];
			VanillaFallbackOnModDeletion = TileID.Platforms;

			TileObjectData.newTile.CoordinateHeights = [16];
			TileObjectData.newTile.CoordinateWidth = 16;
			TileObjectData.newTile.CoordinatePadding = 2;
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleMultiplier = 27;
			TileObjectData.newTile.StyleWrapLimit = 27;
			TileObjectData.newTile.UsesCustomCanPlace = false;
			TileObjectData.addTile(Type);
		}

		public override void PostSetDefaults() => Main.tileNoSunLight[Type] = false;

		public override bool CanDrop(int i, int j) => false;

		public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 4;

		public override bool CanExplode(int i, int j) => true;
	}
}
