using Terraria;
using Terraria.ModLoader;

namespace BagofChaos.Content.TileEntities
{
	// Exists purely so the Null Gravity System can find every beacon cheaply. No data, no updates.
	public class NullGravityBeaconTE : ModTileEntity
	{
		public override bool IsTileValidForEntity(int x, int y) {
			Tile tile = Main.tile[x, y];
			return tile.HasTile && tile.TileType == ModContent.TileType<Tiles.NullGravityBeaconTile>();
		}

		public override int Hook_AfterPlacement(int i, int j, int type, int style, int direction, int alternate) {
			if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient) {
				NetMessage.SendTileSquare(Main.myPlayer, i, j, 2, 3);
				NetMessage.SendData(Terraria.ID.MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);
				return -1;
			}
			return Place(i, j);
		}
	}
}
