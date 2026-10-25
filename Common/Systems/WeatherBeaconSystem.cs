using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BagofChaos.Common.Systems
{
	// Placeable global weather control. Last beacon placed wins; breaking the active beacon hands control back to vanilla.
	public class WeatherBeaconSystem : ModSystem
	{
		public const int StyleRain = 0;
		public const int StyleStorm = 1;
		public const int StyleBlizzard = 2;
		public const int StyleSunshine = 3;
		public const int StyleSandstorm = 4;
		public const int StyleJellyStorm = 5;
		public const int StyleFrogRain = 6;
		public const int StyleCount = 7;

		public static int ActiveStyle = -1;
		public static Point16 ActivePosition = Point16.NegativeOne;

		private static int syncTimer;

		public override void ClearWorld() {
			ActiveStyle = -1;
			ActivePosition = Point16.NegativeOne;
		}

		public override void SaveWorldData(TagCompound tag) {
			if (ActiveStyle >= 0) {
				tag["beaconStyle"] = ActiveStyle;
				tag["beaconX"] = (int)ActivePosition.X;
				tag["beaconY"] = (int)ActivePosition.Y;
			}
		}

		public override void LoadWorldData(TagCompound tag) {
			ActiveStyle = tag.ContainsKey("beaconStyle") ? tag.GetInt("beaconStyle") : -1;
			ActivePosition = new Point16(tag.GetInt("beaconX"), tag.GetInt("beaconY"));
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write((sbyte)ActiveStyle);
			writer.Write(ActivePosition.X);
			writer.Write(ActivePosition.Y);
		}

		public override void NetReceive(BinaryReader reader) {
			ActiveStyle = reader.ReadSByte();
			ActivePosition = new Point16(reader.ReadInt16(), reader.ReadInt16());
		}

		public static void BeaconPlacedDirect(int x, int y, int style) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			ActiveStyle = style;
			ActivePosition = new Point16(x, y);
			ApplyWeather(force: true);
			ChaosEvents.Announce("Beacon" + style, new Color(120, 200, 255));
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);
		}

		public static void BeaconRemovedDirect(int x, int y) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (ActivePosition.X != x || ActivePosition.Y != y)
				return; // not the active one, nothing changes
			int oldStyle = ActiveStyle;
			ActiveStyle = -1;
			ActivePosition = Point16.NegativeOne;
			// Control returns to Terraria's ordinary weather system. We only clean up what we forced.
			if (oldStyle == StyleRain || oldStyle == StyleStorm || oldStyle == StyleBlizzard || oldStyle == StyleJellyStorm || oldStyle == StyleFrogRain) {
				Main.StopRain();
				Main.windSpeedTarget = Main.rand.NextFloat(-0.2f, 0.2f);
			}
			else if (oldStyle == StyleSandstorm) {
				Sandstorm.StopSandstorm();
			}
			ChaosEvents.Announce("BeaconRemoved", new Color(120, 200, 255));
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);
		}

		public override void PostUpdateWorld() {
			if (ActiveStyle < 0)
				return;

			// Sanity: if the beacon tile vanished without KillMultiTile running (world edits, explosions in odd states), release the override.
			Tile tile = Framing.GetTileSafely(ActivePosition.X, ActivePosition.Y);
			if (!tile.HasTile || tile.TileType != ModContent.TileType<Content.Tiles.WeatherBeaconTile>()) {
				BeaconRemovedDirect(ActivePosition.X, ActivePosition.Y);
				return;
			}

			ApplyWeather(force: false);
			if (Main.netMode == NetmodeID.Server && ++syncTimer >= 300) {
				syncTimer = 0;
				NetMessage.SendData(MessageID.WorldData);
			}
		}

		private static void ApplyWeather(bool force) {
			switch (ActiveStyle) {
				case StyleRain:
					if (!Main.raining || force)
						Main.StartRain();
					Main.rainTime = 36000;
					Main.maxRaining = 0.5f;
					Main.windSpeedTarget = 0.15f;
					if (Sandstorm.Happening)
						Sandstorm.StopSandstorm();
					break;
				case StyleStorm:
					if (!Main.raining || force)
						Main.StartRain();
					Main.rainTime = 36000;
					Main.maxRaining = 0.95f; // heavy rain + strong wind = IsItStorming
					Main.windSpeedTarget = 0.95f;
					Main.windSpeedCurrent = 0.95f;
					if (Sandstorm.Happening)
						Sandstorm.StopSandstorm();
					break;
				case StyleBlizzard:
					// Blizzards are "rain while in snow with strong wind"; we set the world up and let the biome do the rest.
					if (!Main.raining || force)
						Main.StartRain();
					Main.rainTime = 36000;
					Main.maxRaining = 0.8f;
					Main.windSpeedTarget = -0.9f;
					Main.windSpeedCurrent = -0.9f;
					break;
				case StyleSunshine:
					if (Main.raining)
						Main.StopRain();
					Main.rainTime = 0;
					Main.maxRaining = 0f;
					Main.windSpeedTarget = 0f;
					if (Sandstorm.Happening)
						Sandstorm.StopSandstorm();
					break;
				case StyleSandstorm:
					if (!Sandstorm.Happening || force)
						Sandstorm.StartSandstorm();
					Sandstorm.TimeLeft = 36000;
					Sandstorm.IntendedSeverity = 1f;
					break;
				case StyleJellyStorm:
				case StyleFrogRain:
					// Custom effects handled elsewhere, but it still looks (and sounds) like rain.
					if (!Main.raining || force)
						Main.StartRain();
					Main.rainTime = 36000;
					Main.maxRaining = 0.7f;
					Main.windSpeedTarget = 0.3f;
					if (Sandstorm.Happening)
						Sandstorm.StopSandstorm();
					break;
			}
		}
	}
}
