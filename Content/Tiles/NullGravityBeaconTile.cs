using BagofChaos.Common.Systems;
using BagofChaos.Content.TileEntities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BagofChaos.Content.Tiles
{
	// 2x3 statue-ish thing. Everything in range forgets what gravity is. Liquids are explicitly exempt; they were too complicated.
	public class NullGravityBeaconTile : ModTile
	{
		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
			TileObjectData.newTile.Height = 3;
			TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
			TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent.GetInstance<NullGravityBeaconTE>().Generic_HookPostPlaceMyPlayer;
			TileObjectData.addTile(Type);

			DustType = DustID.Electric;
			AddMapEntry(new Color(90, 230, 230), CreateMapEntryName());
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY) {
			ModContent.GetInstance<NullGravityBeaconTE>().Kill(i, j);
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.2f;
			g = 0.7f;
			b = 0.8f;
		}

		public override void NearbyEffects(int i, int j, bool closer) {
			// A faint ring of sparkles so you can tell where the field ends.
			if (!closer || !Main.rand.NextBool(6) || !TileObjectData.IsTopLeft(i, j))
				return;
			Vector2 center = new Vector2(i * 16f + 16f, j * 16f + 24f);
			Vector2 pos = center + Main.rand.NextVector2CircularEdge(NullGravitySystem.RadiusPixels, NullGravitySystem.RadiusPixels);
			Dust dust = Dust.NewDustPerfect(pos, DustID.Electric, Vector2.Zero, 0, default, 0.7f);
			dust.noGravity = true;
		}
	}
}
