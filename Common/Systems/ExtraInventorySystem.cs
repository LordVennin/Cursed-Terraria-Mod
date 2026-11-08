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
	// Rat Package Co.: nine extra slots in a row along the bottom of the inventory, right beside the trash can.
	// Vanilla puts the trash can under the last inventory column normally, and under the last chest column when a chest
	// (or shop) is open, so the row follows it: inventory on top, chest in the middle, trash + this row at the bottom.
	// Slots are drawn and hit-tested from the same rectangle, so the mouse always lines up. They use the bank slot context:
	// shift-click sends items to your inventory, and (unlike the chest context) it never sends chest sync packets.
	public class ExtraInventorySystem : ModSystem
	{
		public static readonly Color RowTint = new(210, 170, 120); // light brown, rat approved

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
			if (index < 0)
				return;
			layers.Insert(index + 1, new LegacyGameInterfaceLayer("BagofChaos: Extra Inventory Row", DrawRow, InterfaceScaleType.UI));
		}

		/// <summary>Mirrors Main.DrawTrashItemSlot: where the trash can is, and at what scale.</summary>
		private static void Layout(out float startX, out float y, out float scale, out float spacing) {
			bool chestLayout = (Main.LocalPlayer.chest != -1 || Main.npcShop > 0) && !Main.recBigList;
			if (chestLayout) {
				scale = 0.755f;
				startX = 73f;           // the chest's first column; column 9 is where the trash can sits (453)
				y = 258f + 168f;
			}
			else {
				scale = 0.85f;
				startX = 20f;           // the inventory's first column; column 9 is the trash can (448)
				y = 258f;
			}
			spacing = 56f * scale;
		}

		private static bool DrawRow() {
			if (!Main.playerInventory || Main.LocalPlayer == null || Main.hairWindow || Main.InReforgeMenu || Main.InGuideCraftMenu)
				return true;
			ChaosPlayer chaos = Main.LocalPlayer.GetModPlayer<ChaosPlayer>();
			if (!chaos.ExtraRowUnlocked)
				return true;
			Item[] slots = chaos.ExtraRow;
			Layout(out float startX, out float y, out float scale, out float spacing);
			float oldScale = Main.inventoryScale;
			Main.inventoryScale = scale;
			for (int i = 0; i < slots.Length; i++) {
				int x = (int)(startX + i * spacing);
				Vector2 position = new Vector2(x, (int)y);
				Rectangle slotRect = new Rectangle(x, (int)y, (int)(TextureAssets.InventoryBack.Width() * scale), (int)(TextureAssets.InventoryBack.Height() * scale));
				if (slotRect.Contains(Main.mouseX, Main.mouseY) && !PlayerInput.IgnoreMouseInterface) {
					Main.LocalPlayer.mouseInterface = true;
					ItemSlot.Handle(slots, ItemSlot.Context.BankItem, i);
				}
				Color previous = Main.inventoryBack;
				Main.inventoryBack = RowTint;
				ItemSlot.Draw(Main.spriteBatch, slots, ItemSlot.Context.BankItem, i, position);
				Main.inventoryBack = previous;
			}
			Main.inventoryScale = oldScale;
			return true;
		}
	}
}
