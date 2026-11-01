using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace BagofChaos.Common
{
	// "Hue shifted to white" versions of vanilla sprites, built once on the client from the original pixels.
	public static class WhitenedTextures
	{
		private static readonly Dictionary<string, Texture2D> cache = new();

		public static Texture2D Get(string vanillaPath) {
			if (cache.TryGetValue(vanillaPath, out Texture2D cached) && cached != null && !cached.IsDisposed)
				return cached;
			Texture2D source = ModContent.Request<Texture2D>(vanillaPath, AssetRequestMode.ImmediateLoad).Value;
			Color[] data = new Color[source.Width * source.Height];
			source.GetData(data);
			for (int i = 0; i < data.Length; i++) {
				Color c = data[i];
				if (c.A == 0)
					continue;
				// Luminance, pushed toward white: dark lines stay dark enough to read, everything else goes pale.
				float l = (0.3f * c.R + 0.59f * c.G + 0.11f * c.B) / 255f;
				byte v = (byte)(MathHelper.Clamp(0.45f + l * 0.6f, 0f, 1f) * 255f);
				data[i] = new Color(v, v, (byte)System.Math.Min(255, v + 10), c.A);
			}
			Texture2D result = new Texture2D(Main.graphics.GraphicsDevice, source.Width, source.Height);
			result.SetData(data);
			cache[vanillaPath] = result;
			return result;
		}

		public static void Unload() {
			foreach (Texture2D texture in cache.Values)
				texture?.Dispose();
			cache.Clear();
		}
	}

	public class WhitenedTexturesSystem : ModSystem
	{
		public override void Unload() => WhitenedTextures.Unload();
	}
}
