using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BagofChaos.Content.Tiles
{
	// Solid blocks that let one kind of entity through. The solidity is flipped on and off around the relevant update loops
	// (players in ChaosPlayer, NPCs in PassableBlockSystem), which is as close as Terraria gets to "solid for some".
	public abstract class SelectivelySolidBlock : ModTile
	{
		protected abstract Color MapColor { get; }

		public override void SetStaticDefaults() {
			Main.tileSolid[Type] = true;
			Main.tileMergeDirt[Type] = false;
			Main.tileBlockLight[Type] = false;
			Main.tileLighted[Type] = true;
			TileID.Sets.DrawsWalls[Type] = true;
			DustType = DustID.Glass;
			AddMapEntry(MapColor);
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = MapColor.R / 255f * 0.25f;
			g = MapColor.G / 255f * 0.25f;
			b = MapColor.B / 255f * 0.25f;
		}
	}

	// Hollow Block: black glass with gold trim. Players always pass through (from every side, no need to hold down);
	// everything else treats it as solid. It can be placed in thin air: a 1x1 object with no anchors skips vanilla's
	// "needs a neighbouring block or wall" rule, while the normal occupancy checks still apply. So floating bridges work.
	public class HollowBlockTile : SelectivelySolidBlock
	{
		protected override Color MapColor => new Color(40, 40, 50);

		public override void SetStaticDefaults() {
			base.SetStaticDefaults();
			Main.tileFrameImportant[Type] = true;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.AnchorBottom = Terraria.DataStructures.AnchorData.Empty;
			TileObjectData.newTile.AnchorTop = Terraria.DataStructures.AnchorData.Empty;
			TileObjectData.newTile.AnchorLeft = Terraria.DataStructures.AnchorData.Empty;
			TileObjectData.newTile.AnchorRight = Terraria.DataStructures.AnchorData.Empty;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.UsesCustomCanPlace = true;
			TileObjectData.addTile(Type);
		}
	}

	public class BlueCoreBlockTile : SelectivelySolidBlock
	{
		protected override Color MapColor => new Color(60, 120, 255);
	}

	// Keeps Blue Core blocks passable for NPCs during the NPC update, and solid for everything else.
	public class PassableBlockSystem : ModSystem
	{
		public override void PreUpdateNPCs() => Main.tileSolid[ModContent.TileType<BlueCoreBlockTile>()] = false;
		public override void PostUpdateNPCs() => Main.tileSolid[ModContent.TileType<BlueCoreBlockTile>()] = true;
	}

	// Model Demon Altar: vanilla altar sprite tinted blue; crafts like a demon altar; mined normally; no ores.
	public class ModelDemonAltarTile : ModTile
	{
		public override string Texture => "Terraria/Images/Tiles_" + TileID.DemonAltar;

		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.CoordinateHeights = [16, 18];
			TileObjectData.addTile(Type);

			AdjTiles = [TileID.DemonAltar];
			DustType = DustID.Stone;
			AddMapEntry(new Color(80, 110, 200), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.1f;
			g = 0.2f;
			b = 0.6f;
		}

		public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) {
			Tile tile = Main.tile[i, j];
			Texture2D texture = TextureAssets.Tile[Type].Value;
			Color color = Lighting.GetColor(i, j).MultiplyRGB(new Color(120, 160, 255));
			Vector2 offset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
			Vector2 position = new Vector2(i * 16f, j * 16f) - Main.screenPosition + offset;
			int height = tile.TileFrameY % 36 == 18 ? 18 : 16;
			// Use the demon altar's frames (the first 3x2 block of the vanilla sheet).
			spriteBatch.Draw(texture, position, new Rectangle(tile.TileFrameX % 54, tile.TileFrameY % 36, 16, height), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			return false;
		}
	}

	// Chaotic Dehydrator: post-Moon Lord Extractinator for literally any item.
	public class ChaoticDehydratorTile : ModTile
	{
		private static int holdTimer;

		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;
			TileID.Sets.HasOutlines[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.CoordinateHeights = [16, 18];
			TileObjectData.addTile(Type);

			DustType = DustID.RainbowMk2;
			AddMapEntry(new Color(220, 60, 220), CreateMapEntryName());
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			Color c = Main.DiscoColor;
			r = c.R / 255f * 0.6f;
			g = c.G / 255f * 0.6f;
			b = c.B / 255f * 0.6f;
		}

		public override bool HasSmartInteract(int i, int j, Terraria.GameContent.ObjectInteractions.SmartInteractScanSettings settings) => true;

		public override void MouseOver(int i, int j) {
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = player.HeldItem.IsAir ? ModContent.ItemType<Items.Placeable.ChaoticDehydrator>() : player.HeldItem.type;
			if (Main.mouseRight && !Main.mouseRightRelease && !player.mouseInterface) {
				if (++holdTimer >= 8) {
					holdTimer = 0;
					ProcessOne(i, j);
				}
			}
			else {
				holdTimer = -20;
			}
		}

		public override bool RightClick(int i, int j) {
			holdTimer = -20;
			ProcessOne(i, j);
			return true;
		}

		private static void ProcessOne(int i, int j) {
			Player player = Main.LocalPlayer;
			Item held = player.HeldItem;
			if (held.IsAir)
				return;
			int result;
			int guard = 0;
			do {
				result = Main.rand.Next(1, ItemID.Count); // vanilla items only
			} while ((ContentSamples.ItemsByType[result].IsAir || ItemID.Sets.Deprecated[result]) && guard++ < 50);
			held.stack--;
			if (held.stack <= 0)
				held.TurnToAir();
			player.QuickSpawnItem(player.GetSource_TileInteraction(i, j), result, 1);
			SoundEngine.PlaySound(SoundID.Item4, new Vector2(i * 16f, j * 16f));
			for (int d = 0; d < 8; d++) {
				Dust dust = Dust.NewDustDirect(new Vector2(i * 16f - 16f, j * 16f - 8f), 48, 24, DustID.RainbowMk2, 0f, -1f, 0, Main.DiscoColor);
				dust.noGravity = true;
			}
		}
	}
}
