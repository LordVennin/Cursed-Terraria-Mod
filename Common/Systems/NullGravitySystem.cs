using System.Collections.Generic;
using BagofChaos.Content.TileEntities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace BagofChaos.Common.Systems
{
	// Keeps a cheap list of Null Gravity Beacon positions so entities can ask "am I in a field?" every frame.
	public class NullGravitySystem : ModSystem
	{
		public const float RadiusTiles = 18f;
		public const float RadiusPixels = RadiusTiles * 16f;

		private static readonly List<Vector2> fieldCenters = new();
		private static int refreshTimer;

		public override void ClearWorld() {
			fieldCenters.Clear();
		}

		public override void PreUpdateEntities() {
			// Refresh a few times a second; tile entities don't change that often.
			if (++refreshTimer < 10 && fieldCenters.Count > 0)
				return;
			refreshTimer = 0;
			fieldCenters.Clear();
			foreach (TileEntity te in TileEntity.ByID.Values) {
				if (te is NullGravityBeaconTE beacon) {
					fieldCenters.Add(new Vector2(beacon.Position.X * 16f + 16f, beacon.Position.Y * 16f + 24f));
				}
			}
		}

		public static bool InField(Vector2 worldPosition) {
			for (int i = 0; i < fieldCenters.Count; i++) {
				if (Vector2.DistanceSquared(fieldCenters[i], worldPosition) < RadiusPixels * RadiusPixels)
					return true;
			}
			return false;
		}

		public static IReadOnlyList<Vector2> FieldCenters => fieldCenters;
	}
}
