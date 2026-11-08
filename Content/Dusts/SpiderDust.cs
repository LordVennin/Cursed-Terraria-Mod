using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace BagofChaos.Content.Dusts
{
	// A tiny spider that drifts away and fades out. Used by the Balls of Spiders wing trail.
	public class SpiderDust : ModDust
	{
		public const int Frames = 3;

		public override void OnSpawn(Dust dust) {
			dust.noGravity = true;
			dust.noLight = true;
			dust.frame = new Rectangle(0, Main.rand.Next(Frames) * 10, 10, 10);
			dust.scale = Main.rand.NextFloat(0.8f, 1.1f);
			dust.rotation = Main.rand.NextFloat(-0.4f, 0.4f);
		}

		public override bool Update(Dust dust) {
			dust.position += dust.velocity;
			dust.velocity *= 0.94f;
			dust.velocity.Y += 0.02f; // dangling on a thread
			dust.rotation += dust.velocity.X * 0.05f;
			dust.alpha += 8;
			dust.scale *= 0.985f;
			if (dust.alpha >= 255 || dust.scale < 0.3f)
				dust.active = false;
			return false;
		}

		public override Color? GetAlpha(Dust dust, Color lightColor) => lightColor * ((255 - dust.alpha) / 255f);
	}
}
