using System.Collections.Generic;
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
	// Placeable global weather control. Beacons can be toggled on and off by right clicking; the most recently switched-on
	// beacon wins. When no beacon is on, control returns to Terraria's ordinary weather.
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

		public struct Beacon
		{
			public Point16 Position;
			public int Style;
		}

		// Ordered by activation time: the last entry is the active one.
		private static readonly List<Beacon> onBeacons = new();

		public static int ActiveStyle => onBeacons.Count > 0 ? onBeacons[^1].Style : -1;
		public static Point16 ActivePosition => onBeacons.Count > 0 ? onBeacons[^1].Position : Point16.NegativeOne;

		private static int syncTimer;

		public override void ClearWorld() {
			onBeacons.Clear();
		}

		public override void SaveWorldData(TagCompound tag) {
			var list = new List<TagCompound>();
			foreach (Beacon beacon in onBeacons)
				list.Add(new TagCompound { ["x"] = (int)beacon.Position.X, ["y"] = (int)beacon.Position.Y, ["style"] = beacon.Style });
			tag["beacons"] = list;
		}

		public override void LoadWorldData(TagCompound tag) {
			onBeacons.Clear();
			foreach (TagCompound entry in tag.GetList<TagCompound>("beacons"))
				onBeacons.Add(new Beacon { Position = new Point16(entry.GetInt("x"), entry.GetInt("y")), Style = entry.GetInt("style") });
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write((byte)onBeacons.Count);
			foreach (Beacon beacon in onBeacons) {
				writer.Write(beacon.Position.X);
				writer.Write(beacon.Position.Y);
				writer.Write((byte)beacon.Style);
			}
		}

		public override void NetReceive(BinaryReader reader) {
			onBeacons.Clear();
			int count = reader.ReadByte();
			for (int i = 0; i < count; i++)
				onBeacons.Add(new Beacon { Position = new Point16(reader.ReadInt16(), reader.ReadInt16()), Style = reader.ReadByte() });
		}

		private static int IndexOf(int x, int y) => onBeacons.FindIndex(b => b.Position.X == x && b.Position.Y == y);

		/// <summary>Beacon switched on (or freshly placed): it becomes the active one.</summary>
		public static void BeaconPlacedDirect(int x, int y, int style) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int index = IndexOf(x, y);
			if (index >= 0)
				onBeacons.RemoveAt(index);
			onBeacons.Add(new Beacon { Position = new Point16(x, y), Style = style });
			ApplyWeather(force: true);
			ChaosEvents.Announce("Beacon" + style, new Color(120, 200, 255));
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);
		}

		/// <summary>Beacon switched off or broken.</summary>
		public static void BeaconRemovedDirect(int x, int y) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int index = IndexOf(x, y);
			if (index < 0)
				return;
			bool wasActive = index == onBeacons.Count - 1;
			int oldStyle = onBeacons[index].Style;
			onBeacons.RemoveAt(index);
			if (wasActive) {
				ReleaseWeather(oldStyle);
				if (onBeacons.Count > 0) {
					ApplyWeather(force: true);
					ChaosEvents.Announce("Beacon" + ActiveStyle, new Color(120, 200, 255));
				}
				else {
					ChaosEvents.Announce("BeaconRemoved", new Color(120, 200, 255));
				}
			}
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);
		}

		private static void ReleaseWeather(int oldStyle) {
			// Control returns to Terraria's ordinary weather system. We only clean up what we forced.
			if (oldStyle == StyleRain || oldStyle == StyleStorm || oldStyle == StyleBlizzard || oldStyle == StyleJellyStorm || oldStyle == StyleFrogRain) {
				Main.StopRain();
				Main.windSpeedTarget = Main.rand.NextFloat(-0.2f, 0.2f);
			}
			else if (oldStyle == StyleSandstorm) {
				Sandstorm.StopSandstorm();
			}
		}

		public override void PostUpdateWorld() {
			if (onBeacons.Count == 0)
				return;

			// Sanity: beacons whose tile vanished without KillMultiTile running are dropped.
			for (int i = onBeacons.Count - 1; i >= 0; i--) {
				Point16 pos = onBeacons[i].Position;
				Tile tile = Framing.GetTileSafely(pos.X, pos.Y);
				if (!tile.HasTile || tile.TileType != ModContent.TileType<Content.Tiles.WeatherBeaconTile>() || tile.TileFrameY >= 36)
					BeaconRemovedDirect(pos.X, pos.Y);
			}
			if (onBeacons.Count == 0)
				return;

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
