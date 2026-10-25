using System.Collections.Generic;
using BagofChaos.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common.Systems
{
	// Sky in a Bottle: temporary real platform tiles that remove themselves after a while.
	// Placement and cleanup run on the authority (server / singleplayer) so everyone sees the same clouds.
	public class CloudPlatformSystem : ModSystem
	{
		public const int CloudLifetime = 60 * 6; // 6 seconds, NOT refreshed by standing on it

		private struct Cloud
		{
			public int X;
			public int Y;
			public int Width;
			public int TimeLeft;
		}

		private static readonly List<Cloud> clouds = new();

		public override void ClearWorld() {
			clouds.Clear();
		}

		public static void PlaceCloudDirect(int x, int y, int width) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int type = ModContent.TileType<CloudPlatform>();
			bool placedAny = false;
			int startX = x - width / 2;
			for (int i = startX; i < startX + width; i++) {
				if (!WorldGen.InWorld(i, y, 10))
					continue;
				Tile tile = Main.tile[i, y];
				if (tile.HasTile)
					continue;
				tile.HasTile = true;
				tile.TileType = (ushort)type;
				tile.TileFrameX = 0;
				tile.TileFrameY = 0;
				tile.Slope = SlopeType.Solid;
				tile.IsHalfBlock = false;
				placedAny = true;
			}
			if (!placedAny)
				return;
			for (int i = startX - 1; i <= startX + width; i++)
				WorldGen.TileFrame(i, y, resetFrame: true);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendTileSquare(-1, startX - 1, y - 1, width + 2, 3);
			clouds.Add(new Cloud { X = startX, Y = y, Width = width, TimeLeft = CloudLifetime });
		}

		public override void PostUpdateWorld() {
			if (clouds.Count == 0)
				return;
			int type = ModContent.TileType<CloudPlatform>();
			for (int c = clouds.Count - 1; c >= 0; c--) {
				Cloud cloud = clouds[c];
				cloud.TimeLeft--;
				if (cloud.TimeLeft > 0) {
					clouds[c] = cloud;
					continue;
				}
				for (int i = cloud.X; i < cloud.X + cloud.Width; i++) {
					if (!WorldGen.InWorld(i, cloud.Y, 10))
						continue;
					Tile tile = Main.tile[i, cloud.Y];
					if (tile.HasTile && tile.TileType == type)
						WorldGen.KillTile(i, cloud.Y, fail: false, effectOnly: false, noItem: true);
				}
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendTileSquare(-1, cloud.X - 1, cloud.Y - 1, cloud.Width + 2, 3);
				clouds.RemoveAt(c);
			}
		}

		// Leftover clouds from a crash or an unloaded world shouldn't linger forever: evaporate stray ones occasionally.
		public override void PostUpdateEverything() {
			if (Main.netMode == NetmodeID.MultiplayerClient || Main.GameUpdateCount % 600 != 0)
				return;
			// Only scan around players to keep this cheap.
			int type = ModContent.TileType<CloudPlatform>();
			foreach (Player player in Main.ActivePlayers) {
				int px = (int)(player.Center.X / 16f);
				int py = (int)(player.Center.Y / 16f);
				for (int i = px - 80; i <= px + 80; i++) {
					for (int j = py - 50; j <= py + 50; j++) {
						if (!WorldGen.InWorld(i, j, 10))
							continue;
						Tile tile = Main.tile[i, j];
						if (tile.HasTile && tile.TileType == type && !IsTracked(i, j)) {
							WorldGen.KillTile(i, j, fail: false, effectOnly: false, noItem: true);
							if (Main.netMode == NetmodeID.Server)
								NetMessage.SendTileSquare(-1, i, j, 1, 1);
						}
					}
				}
			}
		}

		private static bool IsTracked(int x, int y) {
			foreach (Cloud cloud in clouds) {
				if (cloud.Y == y && x >= cloud.X && x < cloud.X + cloud.Width)
					return true;
			}
			return false;
		}
	}
}
