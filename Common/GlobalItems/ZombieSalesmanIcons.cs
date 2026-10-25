using BagofChaos.Common.Configs;
using BagofChaos.Content.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace BagofChaos.Common.GlobalItems
{
	// While a Zombie Salesman is alive, every item icon in your UI is a walking zombie. He's very persuasive.
	public class ZombieSalesmanIcons : GlobalItem
	{
		public static bool SalesmanAround;

		public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			if (!SalesmanAround || !ModContent.GetInstance<ChaosConfig>().ZombieSalesmanReplacesIcons)
				return true;
			Texture2D texture = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/ZombieIcon").Value;
			int frameIndex = (int)(Main.GameUpdateCount / 8 + (uint)item.type) % 4;
			Rectangle source = texture.Frame(1, 4, 0, frameIndex);
			// position is the slot center and origin is the item frame's center, so we draw our own frame centered there.
			spriteBatch.Draw(texture, position, source, drawColor, 0f, source.Size() * 0.5f, scale * 1.2f, SpriteEffects.None, 0f);
			return false;
		}
	}

	public class ZombieSalesmanIconsSystem : ModSystem
	{
		public override void PostUpdateNPCs() {
			ZombieSalesmanIcons.SalesmanAround = NPC.AnyNPCs(ModContent.NPCType<ZombieSalesman>());
		}
	}
}
