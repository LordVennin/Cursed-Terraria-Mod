using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BagofChaos.Common.Systems
{
	public enum RemoteMenu
	{
		None,
		Chat,
		SellOnlyShop,
		Reforge,
		Hair,
		AnglerQuest,
		Directory,
	}

	// Opens a town NPC's interface from anywhere. Vanilla closes the dialogue when you walk away from the NPC,
	// so while a remote menu is open we re-assert the talk target every frame.
	public class RemoteMenuSystem : ModSystem
	{
		public static RemoteMenu Open;
		private static int talkNPCIndex = -1;

		private UserInterface directoryInterface;
		private DirectoryUI directoryUI;

		public override void Load() {
			if (Main.dedServ)
				return;
			directoryInterface = new UserInterface();
			directoryUI = new DirectoryUI();
			directoryUI.Activate();
		}

		public static void Toggle(RemoteMenu menu, int npcType) {
			if (Open == menu) {
				Close();
				return;
			}
			Close();
			if (menu == RemoteMenu.Directory) {
				Open = menu;
				ModContent.GetInstance<RemoteMenuSystem>().directoryInterface.SetState(ModContent.GetInstance<RemoteMenuSystem>().directoryUI);
				Main.playerInventory = false;
				SoundEngine.PlaySound(SoundID.MenuOpen);
				return;
			}
			int index = NPC.FindFirstNPC(npcType);
			if (index < 0) {
				Main.NewText(Language.GetTextValue("Mods.BagofChaos.RemoteMenu.NoNPC", Lang.GetNPCNameValue(npcType)), Color.Orange);
				return;
			}
			OpenFor(menu, index);
		}

		public static void OpenFor(RemoteMenu menu, int npcIndex) {
			Open = menu;
			talkNPCIndex = npcIndex;
			Player player = Main.LocalPlayer;
			player.SetTalkNPC(npcIndex);
			Main.playerInventory = true;
			Main.npcChatText = "";
			Main.npcChatCornerItem = 0;
			SoundEngine.PlaySound(SoundID.MenuOpen);
			switch (menu) {
				case RemoteMenu.Chat:
					Main.npcChatText = Main.npc[npcIndex].GetChat();
					break;
				case RemoteMenu.SellOnlyShop:
					Main.SetNPCShopIndex(1);
					for (int i = 0; i < Main.instance.shop[1].item.Length; i++)
						Main.instance.shop[1].item[i] = new Item();
					Main.npcChatText = Language.GetTextValue("Mods.BagofChaos.RemoteMenu.SellOnly");
					break;
				case RemoteMenu.Reforge:
					Main.InReforgeMenu = true;
					break;
				case RemoteMenu.Hair:
					Main.hairWindow = true;
					Main.playerInventory = false;
					break;
				case RemoteMenu.AnglerQuest:
					Main.npcChatText = Lang.AnglerQuestChat(false);
					break;
			}
		}

		public static void Close() {
			if (Open == RemoteMenu.None)
				return;
			RemoteMenu was = Open;
			Open = RemoteMenu.None;
			talkNPCIndex = -1;
			var system = ModContent.GetInstance<RemoteMenuSystem>();
			system.directoryInterface?.SetState(null);
			if (was == RemoteMenu.Directory)
				return;
			Player player = Main.LocalPlayer;
			player.SetTalkNPC(-1);
			Main.npcChatText = "";
			Main.SetNPCShopIndex(0);
			Main.InReforgeMenu = false;
			Main.hairWindow = false;
			Main.playerInventory = false;
			SoundEngine.PlaySound(SoundID.MenuClose);
		}

		public override void UpdateUI(GameTime gameTime) {
			if (Open == RemoteMenu.None)
				return;
			if (Open == RemoteMenu.Directory) {
				directoryInterface?.Update(gameTime);
				if (Main.LocalPlayer.dead)
					Close();
				return;
			}
			Player player = Main.LocalPlayer;
			if (talkNPCIndex < 0 || !Main.npc[talkNPCIndex].active || player.dead) {
				Close();
				return;
			}
			// The user closed the inventory / chat by other means: let go.
			bool stillOpen = Open == RemoteMenu.Hair ? Main.hairWindow : Main.playerInventory;
			if (!stillOpen) {
				Close();
				return;
			}
			// Vanilla drops the talk target when the NPC is far away; put it back.
			if (player.talkNPC != talkNPCIndex)
				player.SetTalkNPC(talkNPCIndex);
			if (Open == RemoteMenu.SellOnlyShop && Main.npcShop != 1)
				Main.SetNPCShopIndex(1);
			if (Open == RemoteMenu.Reforge)
				Main.InReforgeMenu = true;
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (index < 0)
				return;
			layers.Insert(index, new LegacyGameInterfaceLayer("BagofChaos: NPC Directory", () => {
				if (directoryInterface?.CurrentState != null)
					directoryInterface.Draw(Main.spriteBatch, new GameTime());
				return true;
			}, InterfaceScaleType.UI));
		}
	}

	// A list of every town NPC currently living in the world. Click one to talk to it from wherever you are.
	public class DirectoryUI : UIState
	{
		private UIPanel panel;
		private UIList list;

		public override void OnInitialize() {
			panel = new UIPanel();
			panel.Width.Set(300f, 0f);
			panel.Height.Set(420f, 0f);
			panel.HAlign = 0.5f;
			panel.VAlign = 0.4f;
			panel.BackgroundColor = new Color(63, 65, 151, 220);
			Append(panel);

			var title = new UIText(Language.GetTextValue("Mods.BagofChaos.RemoteMenu.DirectoryTitle"), 1.1f);
			title.HAlign = 0.5f;
			panel.Append(title);

			list = new UIList();
			list.Top.Set(36f, 0f);
			list.Width.Set(-20f, 1f);
			list.Height.Set(-40f, 1f);
			list.ListPadding = 4f;
			panel.Append(list);

			var scrollbar = new UIScrollbar();
			scrollbar.Top.Set(36f, 0f);
			scrollbar.Height.Set(-40f, 1f);
			scrollbar.HAlign = 1f;
			panel.Append(scrollbar);
			list.SetScrollbar(scrollbar);
		}

		public override void OnActivate() {
			Rebuild();
		}

		private int rebuildTimer;

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (panel.ContainsPoint(Main.MouseScreen))
				Main.LocalPlayer.mouseInterface = true;
			if (++rebuildTimer % 60 == 0)
				Rebuild();
		}

		private void Rebuild() {
			list.Clear();
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.townNPC || NPCID.Sets.IsTownPet[npc.type])
					continue;
				int index = npc.whoAmI;
				var entry = new UITextPanel<string>($"{npc.FullName}", 0.9f);
				entry.Width.Set(0f, 1f);
				entry.Height.Set(36f, 0f);
				entry.OnLeftClick += (evt, element) => {
					RemoteMenuSystem.Close();
					RemoteMenuSystem.OpenFor(RemoteMenu.Chat, index);
				};
				entry.OnMouseOver += (evt, element) => entry.BackgroundColor = new Color(90, 100, 200);
				entry.OnMouseOut += (evt, element) => entry.BackgroundColor = new Color(63, 82, 151) * 0.7f;
				list.Add(entry);
			}
		}
	}
}
