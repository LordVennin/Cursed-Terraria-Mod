using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common.Players
{
	// "Harpy" Wings: a vanilla Harpy hovering just above the player, talons on the shoulders, flapping frantically.
	// Purely visual; the actual wings are an invisible vanilla wing slot.
	public class HarpyDrawLayer : PlayerDrawLayer
	{
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			return player.GetModPlayer<ChaosPlayer>().HarpyWingsVisible && !player.mount.Active && !player.dead;
		}

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			if (drawInfo.shadow != 0f)
				return;
			Player player = drawInfo.drawPlayer;
			Main.instance.LoadNPC(NPCID.Harpy);
			Texture2D texture = TextureAssets.Npc[NPCID.Harpy].Value;
			int frames = Main.npcFrameCount[NPCID.Harpy];
			bool gliding = player.velocity.Y != 0f;
			// Frantic when you're in the air, merely panicked on the ground.
			int frame = (int)(Main.GameUpdateCount / (gliding ? 2 : 5) % frames);
			Rectangle source = texture.Frame(1, frames, 0, frame);
			float bob = (float)System.Math.Sin(Main.GameUpdateCount * (gliding ? 0.9f : 0.3f)) * 2f;
			Vector2 top = new Vector2(drawInfo.Position.X + player.width / 2f, drawInfo.Position.Y + player.gfxOffY);
			Vector2 position = top - Main.screenPosition + new Vector2(0f, -source.Height * 0.35f + bob);
			position = new Vector2((int)position.X, (int)position.Y);
			Color color = Lighting.GetColor((top - new Vector2(0f, 16f)).ToTileCoordinates());
			SpriteEffects effects = player.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			drawInfo.DrawDataCache.Add(new DrawData(texture, position, source, color, 0f, source.Size() * 0.5f, 0.85f, effects, 0));
		}
	}
}
