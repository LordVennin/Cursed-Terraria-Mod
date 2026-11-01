using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common.GlobalTiles
{
	public class ChaosGlobalTile : GlobalTile
	{
		private static bool IsGlassy(int type) => type == TileID.Glass || type == TileID.Sand || type == TileID.HardenedSand || type == TileID.Sandstone
			|| type == TileID.Ebonsand || type == TileID.Crimsand || type == TileID.Pearlsand || type == TileID.CorruptHardenedSand || type == TileID.CrimsonHardenedSand
			|| type == TileID.HallowHardenedSand || type == TileID.CorruptSandstone || type == TileID.CrimsonSandstone || type == TileID.HallowSandstone
			|| TileID.Sets.Conversion.Sand[type] || TileID.Sets.Conversion.Sandstone[type] || TileID.Sets.Conversion.HardenedSand[type];

		// Crushed Glass Potion: glass and sand related blocks are invisible to the drinker.
		public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch) {
			if (IsGlassy(type) && !Main.gameMenu && Main.LocalPlayer.HasBuff<CrushedGlass>())
				return false;
			return true;
		}

		// Crushed Dirt Potion: dirt glows brown, as bright as a torch.
		public override void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b) {
			if (type == TileID.Dirt && !Main.gameMenu && Main.LocalPlayer.HasBuff<CrushedDirt>()) {
				r = 1f;
				g = 0.6f;
				b = 0.25f;
			}
		}
	}
}
