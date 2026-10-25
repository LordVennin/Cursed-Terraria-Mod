using System.Collections.Generic;
using BagofChaos.Common.Configs;
using BagofChaos.Common.Players;
using BagofChaos.Content.Buffs;
using BagofChaos.Content.Items.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BagofChaos.Common.Systems
{
	// Full-screen effects drawn as the very first interface layer (so they sit over the world but under the UI):
	//  - the 3D debuff: red/cyan anaglyph by re-drawing the captured scene with masked color channels
	//  - TM Flash: darkness beyond ~20 tiles while underground
	//  - the Block Rod's selection rectangle and ghost preview
	public class ScreenEffectSystem : ModSystem
	{
		private static BlendState redOnly;
		private static BlendState cyanOnly;

		public override void Load() {
			if (Main.dedServ)
				return;
			redOnly = new BlendState {
				ColorSourceBlend = Blend.One,
				ColorDestinationBlend = Blend.Zero,
				AlphaSourceBlend = Blend.One,
				AlphaDestinationBlend = Blend.Zero,
				ColorWriteChannels = ColorWriteChannels.Red,
			};
			cyanOnly = new BlendState {
				ColorSourceBlend = Blend.One,
				ColorDestinationBlend = Blend.Zero,
				AlphaSourceBlend = Blend.One,
				AlphaDestinationBlend = Blend.Zero,
				ColorWriteChannels = ColorWriteChannels.Green | ColorWriteChannels.Blue,
			};
		}

		public override void Unload() {
			redOnly?.Dispose();
			cyanOnly?.Dispose();
			redOnly = cyanOnly = null;
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			layers.Insert(0, new LegacyGameInterfaceLayer("BagofChaos: Screen Effects", DrawEffects, InterfaceScaleType.UI));
		}

		private static bool DrawEffects() {
			if (Main.gameMenu || Main.LocalPlayer == null || !Main.LocalPlayer.active)
				return true;
			ChaosConfig config = ModContent.GetInstance<ChaosConfig>();
			ChaosPlayer modPlayer = Main.LocalPlayer.GetModPlayer<ChaosPlayer>();

			if (config.EnableTMFlashDarkness && modPlayer.FlashActive)
				DrawFlashDarkness(config);

			DrawBlockRodSelection(modPlayer);

			if (config.Enable3DEffect && Main.LocalPlayer.HasBuff<ThreeDDebuff>())
				DrawAnaglyph(config);

			return true;
		}

		private static Vector2 WorldToScreen(Vector2 world) {
			// Interface layers use the UI matrix; the world is drawn zoomed around the screen center.
			Vector2 half = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
			Vector2 screen = (world - Main.screenPosition - half) * Main.GameViewMatrix.Zoom + half;
			return screen / Main.UIScale;
		}

		private static void DrawFlashDarkness(ChaosConfig config) {
			Texture2D mask = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/DarknessMask").Value;
			Texture2D pixel = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/Pixel").Value;
			Vector2 center = WorldToScreen(Main.LocalPlayer.Center);
			float holeRadius = config.TMFlashVisionTiles * 16f * Main.GameViewMatrix.Zoom.X / Main.UIScale;
			float scale = holeRadius / (0.55f * mask.Width / 2f);
			float half = mask.Width * scale / 2f;
			Color black = Color.Black;
			Main.spriteBatch.Draw(mask, center, null, black, 0f, mask.Size() * 0.5f, scale, SpriteEffects.None, 0f);
			int w = (int)(Main.screenWidth / Main.UIScale) + 4;
			int h = (int)(Main.screenHeight / Main.UIScale) + 4;
			int left = (int)(center.X - half), right = (int)(center.X + half), top = (int)(center.Y - half), bottom = (int)(center.Y + half);
			if (left > 0) Main.spriteBatch.Draw(pixel, new Rectangle(-2, -2, left + 2, h), black);
			if (right < w) Main.spriteBatch.Draw(pixel, new Rectangle(right, -2, w - right + 2, h), black);
			if (top > 0) Main.spriteBatch.Draw(pixel, new Rectangle(-2, -2, w, top + 2), black);
			if (bottom < h) Main.spriteBatch.Draw(pixel, new Rectangle(-2, bottom, w, h - bottom + 2), black);
		}

		private static void DrawAnaglyph(ChaosConfig config) {
			if (Main.drawToScreen || Main.screenTarget == null || Main.screenTarget.IsDisposed || Main.screenTarget.IsContentLost || Main.mapFullscreen)
				return;
			float offset = config.ThreeDOffsetPixels;
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, redOnly, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
			Main.spriteBatch.Draw(Main.screenTarget, new Vector2(-offset, 0f), Color.White);
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, cyanOnly, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
			Main.spriteBatch.Draw(Main.screenTarget, new Vector2(offset, 0f), Color.White);
			Main.spriteBatch.End();
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
		}

		private static void DrawBlockRodSelection(ChaosPlayer modPlayer) {
			if (Main.LocalPlayer.HeldItem.type != ModContent.ItemType<BlockRod>())
				return;
			Texture2D pixel = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/Pixel").Value;
			float zoom = Main.GameViewMatrix.Zoom.X / Main.UIScale;

			if (modPlayer.Selecting) {
				Rectangle sel = BlockRod.SelectionRect(modPlayer.SelectionStart, modPlayer.SelectionEnd);
				DrawTileOutline(pixel, sel, Color.Yellow * 0.8f, zoom);
			}

			if (modPlayer.Clipboard != null) {
				Point anchor = BlockRod.PasteAnchor(modPlayer.Clipboard);
				Rectangle rect = new Rectangle(anchor.X, anchor.Y, modPlayer.Clipboard.Width, modPlayer.Clipboard.Height);
				bool ok = modPlayer.Clipboard.CanPlace(anchor);
				DrawTileOutline(pixel, rect, (ok ? Color.LimeGreen : Color.Red) * 0.8f, zoom);
				// Ghost of the blocks
				for (int x = 0; x < modPlayer.Clipboard.Width; x++) {
					for (int y = 0; y < modPlayer.Clipboard.Height; y++) {
						var snap = modPlayer.Clipboard.Tiles[x, y];
						if (!snap.HasTile)
							continue;
						Vector2 pos = WorldToScreen(new Vector2((anchor.X + x) * 16f, (anchor.Y + y) * 16f));
						Main.spriteBatch.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, (int)(16 * zoom), (int)(16 * zoom)), (ok ? Color.White : Color.Red) * 0.35f);
					}
				}
			}
		}

		private static void DrawTileOutline(Texture2D pixel, Rectangle tileRect, Color color, float zoom) {
			Vector2 tl = WorldToScreen(new Vector2(tileRect.X * 16f, tileRect.Y * 16f));
			int w = (int)(tileRect.Width * 16 * zoom), h = (int)(tileRect.Height * 16 * zoom);
			Main.spriteBatch.Draw(pixel, new Rectangle((int)tl.X, (int)tl.Y, w, 2), color);
			Main.spriteBatch.Draw(pixel, new Rectangle((int)tl.X, (int)tl.Y + h - 2, w, 2), color);
			Main.spriteBatch.Draw(pixel, new Rectangle((int)tl.X, (int)tl.Y, 2, h), color);
			Main.spriteBatch.Draw(pixel, new Rectangle((int)tl.X + w - 2, (int)tl.Y, 2, h), color);
		}
	}
}
