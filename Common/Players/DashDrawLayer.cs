using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace BagofChaos.Common.Players
{
	// Draws the GIGA rocket (or the drill) over the player while a chaos dash is active. The "transformation" part of the Rocket Pack.
	public class DashDrawLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.LastVanillaLayer, null);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			return drawInfo.drawPlayer.GetModPlayer<ChaosPlayer>().DashTimer > 0;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			ChaosPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<ChaosPlayer>();
			string path = modPlayer.DashIsDrill ? "BagofChaos/Content/Projectiles/DrillerPackDrill" : "BagofChaos/Content/Projectiles/RocketPackRocket";
			Texture2D texture = ModContent.Request<Texture2D>(path).Value;
			Vector2 position = drawInfo.Center - Main.screenPosition;
			position = new Vector2((int)position.X, (int)position.Y);
			float rotation = modPlayer.DashVelocity.ToRotation();
			float scale = modPlayer.DashIsDrill ? 1.4f : 2.2f; // GIGA
			drawInfo.DrawDataCache.Add(new DrawData(texture, position, null, Color.White, rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0));
		}
	}
}
