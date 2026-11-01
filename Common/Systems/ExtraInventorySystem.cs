using System.Collections.Generic;
using BagofChaos.Common.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace BagofChaos.Common.Systems
{
	// Rat Package Co.: an eleventh inventory row drawn under the vanilla inventory. Ten real slots, saved with the player.
	public class ExtraInventorySystem : ModSystem
	{
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
			if (index < 0)
				return;
			layers.Insert(index + 1, new LegacyGameInterfaceLayer("BagofChaos: Extra Inventory Row", DrawRow, InterfaceScaleType.UI));
		}

		private static bool DrawRow() {
			if (!Main.playerInventory || Main.LocalPlayer == null)
				return true;
			ChaosPlayer chaos = Main.LocalPlayer.GetModPlayer<ChaosPlayer>();
			if (!chaos.ExtraRowUnlocked)
				return true;
			Item[] slots = chaos.ExtraRow;
			float scale = 0.85f;
			Main.inventoryScale = scale;
			Color back = TextureAssets.InventoryBack.Value.Bounds.Size() == Vector2.Zero ? Color.White : new Color(210, 170, 120); // light brown, rat approved
			for (int i = 0; i < slots.Length; i++) {
				int x = (int)(20f + i * 56f * scale);
				int y = (int)(20f + 5 * 56f * scale); // one row under the fifth vanilla row
				Vector2 position = new Vector2(x, y);
				Rectangle slotRect = new Rectangle(x, y, (int)(TextureAssets.InventoryBack.Width() * scale), (int)(TextureAssets.InventoryBack.Height() * scale));
				if (slotRect.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
					Main.LocalPlayer.mouseInterface = true;
					ItemSlot.Handle(slots, ItemSlot.Context.ChestItem, i);
				}
				Color previous = Main.inventoryBack;
				Main.inventoryBack = back;
				ItemSlot.Draw(Main.spriteBatch, slots, ItemSlot.Context.ChestItem, i, position);
				Main.inventoryBack = previous;
			}
			return true;
		}
	}
}
