using BagofChaos.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BagofChaos.Content.Tiles
{
	// A 2x2 crystal ball in seven colours (one per weather). Right click toggles it on/off (off = dim).
	// The vanilla Crystal Ball sheet is used and tinted at draw time; the on/off state lives in TileFrameY (+36 = off).
	public class WeatherBeaconTile : ModTile
	{
		public static readonly Color[] StyleColors = {
			new Color(90, 150, 255),   // rain
			new Color(60, 70, 160),    // storm
			new Color(220, 240, 255),  // blizzard
			new Color(255, 230, 90),   // sunshine
			new Color(230, 200, 130),  // sandstorm
			new Color(255, 130, 220),  // jelly storm
			new Color(110, 230, 110),  // frog rain
		};

		public override string Texture => "Terraria/Images/Tiles_" + TileID.CrystalBall;

		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLighted[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;
			TileID.Sets.HasOutlines[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = WeatherBeaconSystem.StyleCount;
			TileObjectData.addTile(Type);

			DustType = DustID.Glass;
			AddMapEntry(new Color(120, 200, 255), CreateMapEntryName());
		}

		public static int StyleOf(int i, int j) => Main.tile[i, j].TileFrameX / 36;
		public static bool IsOn(int i, int j) => Main.tile[i, j].TileFrameY < 36;

		public override void PlaceInWorld(int i, int j, Item item) {
			var topLeft = TileObjectData.TopLeft(i, j);
			BagofChaos.SendWeatherBeacon(topLeft.X, topLeft.Y, item.placeStyle, placed: true);
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY) {
			BagofChaos.SendWeatherBeacon(i, j, frameX / 36, placed: false);
		}

		public override bool HasSmartInteract(int i, int j, Terraria.GameContent.ObjectInteractions.SmartInteractScanSettings settings) => true;

		public override void MouseOver(int i, int j) {
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ItemID.CrystalBall;
		}

		public override bool RightClick(int i, int j) {
			var topLeft = TileObjectData.TopLeft(i, j);
			bool turnOn = !IsOn(topLeft.X, topLeft.Y);
			short delta = (short)(turnOn ? -36 : 36);
			for (int x = 0; x < 2; x++) {
				for (int y = 0; y < 2; y++) {
					Tile tile = Main.tile[topLeft.X + x, topLeft.Y + y];
					tile.TileFrameY = (short)(tile.TileFrameY + delta);
				}
			}
			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendTileSquare(-1, topLeft.X, topLeft.Y, 2, 2);
			SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16f, j * 16f));
			BagofChaos.SendWeatherBeacon(topLeft.X, topLeft.Y, StyleOf(topLeft.X, topLeft.Y), placed: turnOn);
			return true;
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			if (!IsOn(i, j)) {
				r = g = b = 0.05f;
				return;
			}
			Color c = StyleColors[System.Math.Clamp(StyleOf(i, j), 0, StyleColors.Length - 1)];
			r = c.R / 255f * 0.6f;
			g = c.G / 255f * 0.6f;
			b = c.B / 255f * 0.6f;
		}

		public override bool PreDraw(int i, int j, SpriteBatch spriteBatch) {
			Tile tile = Main.tile[i, j];
			Texture2D texture = TextureAssets.Tile[Type].Value;
			int localX = tile.TileFrameX % 36;
			int localY = tile.TileFrameY % 36;
			Color tint = StyleColors[System.Math.Clamp(tile.TileFrameX / 36, 0, StyleColors.Length - 1)];
			Color light = Lighting.GetColor(i, j);
			Color color = light.MultiplyRGB(tint);
			if (!IsOn(i, j))
				color = color.MultiplyRGB(new Color(90, 90, 90));
			Vector2 offset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
			Vector2 position = new Vector2(i * 16f, j * 16f) - Main.screenPosition + offset;
			spriteBatch.Draw(texture, position, new Rectangle(localX, localY, 16, 16), color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			return false;
		}
	}
}
