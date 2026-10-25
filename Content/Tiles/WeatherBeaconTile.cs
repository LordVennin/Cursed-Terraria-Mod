using BagofChaos.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BagofChaos.Content.Tiles
{
	// One tile, seven styles (one per bottle). Placing one sets the world's weather; breaking the active one gives control back to vanilla.
	public class WeatherBeaconTile : ModTile
	{
		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
			TileObjectData.newTile.Height = 3;
			TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = WeatherBeaconSystem.StyleCount;
			TileObjectData.addTile(Type);

			DustType = DustID.Glass;
			AddMapEntry(new Color(120, 200, 255), CreateMapEntryName());
		}

		public override void PlaceInWorld(int i, int j, Item item) {
			// Called on the placing client. (i, j) is the clicked tile; normalize to the top-left.
			var topLeft = TileObjectData.TopLeft(i, j);
			BagofChaos.SendWeatherBeacon(topLeft.X, topLeft.Y, item.placeStyle, placed: true);
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY) {
			BagofChaos.SendWeatherBeacon(i, j, frameX / 36, placed: false);
		}

		public override bool CanDrop(int i, int j) => true;

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.3f;
			g = 0.5f;
			b = 0.8f;
		}
	}
}
