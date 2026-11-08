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

	// Opens a town NPC's interface from anywhere, whether or not that NPC is alive or even in the world.
	// Vanilla UI needs an NPC to talk to within reach, so we make an invisible, client-only stand-in of the right type
	// that sits on the player. It never runs AI, can't be hit, isn't drawn, and has townNPC off so it is never saved,
	// never counted for housing and never shows on the map. It is removed the moment the menu closes.
	public class RemoteMenuSystem : ModSystem
	{
		public static RemoteMenu Open;
		private static int ghostIndex = -1;
		private static int ghostType = -1;

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
				Close(closeInventory: true);
				return;
			}
			Close(closeInventory: true);
			if (menu == RemoteMenu.Directory) {
				Open = menu;
				ModContent.GetInstance<RemoteMenuSystem>().directoryInterface.SetState(ModContent.GetInstance<RemoteMenuSystem>().directoryUI);
				Main.playerInventory = false;
				SoundEngine.PlaySound(SoundID.MenuOpen);
				return;
			}
			OpenFor(menu, npcType);
		}

		private static bool GhostValid => ghostIndex >= 0 && Main.npc[ghostIndex].active && Main.npc[ghostIndex].type == ghostType
			&& Main.npc[ghostIndex].GetGlobalNPC<GlobalNPCs.ChaosGlobalNPC>().RemoteGhost;

		private static int SpawnGhost(int npcType) {
			for (int i = Main.maxNPCs - 1; i >= 0; i--) {
				if (Main.npc[i].active)
					continue;
				NPC ghost = Main.npc[i];
				ghost.SetDefaults(npcType);
				ghost.whoAmI = i;
				ghost.active = true;
				ghost.townNPC = false;
				ghost.homeless = true;
				ghost.friendly = true;
				ghost.dontTakeDamage = true;
				ghost.Center = Main.LocalPlayer.Center;
				ghost.velocity = Vector2.Zero;
				int real = NPC.FindFirstNPC(npcType);
				ghost.GivenName = real >= 0 && real != i ? Main.npc[real].GivenName : TownRegistrySystem.NameOf(npcType);
				ghost.GetGlobalNPC<GlobalNPCs.ChaosGlobalNPC>().RemoteGhost = true;
				return i;
			}
			return -1;
		}

		private static void RemoveGhost() {
			if (GhostValid) {
				Main.npc[ghostIndex].active = false;
				Main.npc[ghostIndex].GetGlobalNPC<GlobalNPCs.ChaosGlobalNPC>().RemoteGhost = false;
			}
			ghostIndex = -1;
			ghostType = -1;
		}

		public static void OpenFor(RemoteMenu menu, int npcType) {
			int index = SpawnGhost(npcType);
			if (index < 0) {
				Main.NewText(Language.GetTextValue("Mods.BagofChaos.RemoteMenu.NoNPC", Lang.GetNPCNameValue(npcType)), Color.Orange);
				return;
			}
			Open = menu;
			ghostIndex = index;
			ghostType = npcType;
			NPC ghost = Main.npc[index];
			Player player = Main.LocalPlayer;
			player.SetTalkNPC(index);
			Main.npcChatCornerItem = 0;
			Main.npcChatText = "";
			Main.playerInventory = false;
			SoundEngine.PlaySound(SoundID.MenuOpen);
			switch (menu) {
				case RemoteMenu.Chat:
					Main.npcChatText = ghost.GetChat();
					break;
				case RemoteMenu.SellOnlyShop:
					// An empty shop window: you can sell into it (and buy back what you sold), but the Merchant's stock isn't there.
					Main.playerInventory = true;
					Main.SetNPCShopIndex(1);
					for (int i = 0; i < Main.instance.shop[1].item.Length; i++)
						Main.instance.shop[1].item[i] = new Item();
					break;
				case RemoteMenu.Reforge:
					Main.playerInventory = true;
					Main.InReforgeMenu = true;
					break;
				case RemoteMenu.Hair:
					Main.OpenHairWindow();
					break;
				case RemoteMenu.AnglerQuest:
					Main.npcChatText = Lang.AnglerQuestChat(false);
					break;
			}
		}

		/// <param name="closeInventory">True when the player closed it with the item (resets everything);
		/// false when vanilla already closed the window and we just clean up.</param>
		public static void Close(bool closeInventory = false) {
			if (Open == RemoteMenu.None)
				return;
			RemoteMenu was = Open;
			Open = RemoteMenu.None;
			var system = ModContent.GetInstance<RemoteMenuSystem>();
			system.directoryInterface?.SetState(null);
			if (was == RemoteMenu.Directory) {
				RemoveGhost();
				return;
			}
			Player player = Main.LocalPlayer;
			if (player.talkNPC == ghostIndex && ghostIndex >= 0)
				player.SetTalkNPC(-1);
			if (closeInventory) {
				Main.npcChatText = "";
				Main.SetNPCShopIndex(0);
				Main.InReforgeMenu = false;
				if (Main.hairWindow)
					Main.CancelHairWindow();
				Main.playerInventory = false;
				SoundEngine.PlaySound(SoundID.MenuClose);
			}
			RemoveGhost();
		}

		public override void OnWorldUnload() {
			Open = RemoteMenu.None;
			RemoveGhost();
		}

		public override void UpdateUI(GameTime gameTime) {
			if (Open == RemoteMenu.None)
				return;
			Player player = Main.LocalPlayer;
			if (Open == RemoteMenu.Directory) {
				directoryInterface?.Update(gameTime);
				if (player.dead)
					Close();
				return;
			}
			if (!GhostValid || player.dead) {
				Close();
				return;
			}
			// Keep the stand-in on top of the player so vanilla's "too far away" check never trips.
			NPC ghost = Main.npc[ghostIndex];
			ghost.Center = player.Center;
			ghost.velocity = Vector2.Zero;
			ghost.direction = -player.direction;

			bool stillOpen = player.talkNPC == ghostIndex && Open switch {
				RemoteMenu.Hair => Main.hairWindow,
				RemoteMenu.SellOnlyShop => Main.playerInventory && Main.npcShop == 1,
				RemoteMenu.Reforge => Main.playerInventory && Main.InReforgeMenu,
				_ => true,
			};
			if (!stillOpen)
				Close();
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

	// Every town NPC that has ever moved into this world, alive or not. Click one to talk to it from wherever you are.
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
			foreach (var pair in TownRegistrySystem.Known) {
				int type = pair.Key;
				string typeName = Lang.GetNPCNameValue(type);
				bool alive = NPC.AnyNPCs(type);
				string label = string.IsNullOrEmpty(pair.Value) ? typeName : pair.Value + " the " + typeName;
				if (!alive)
					label += " " + Language.GetTextValue("Mods.BagofChaos.RemoteMenu.Absent");
				var entry = new UITextPanel<string>(label, 0.9f);
				entry.Width.Set(0f, 1f);
				entry.Height.Set(36f, 0f);
				entry.OnLeftClick += (evt, element) => {
					RemoteMenuSystem.Close();
					RemoteMenuSystem.OpenFor(RemoteMenu.Chat, type);
				};
				entry.OnMouseOver += (evt, element) => entry.BackgroundColor = new Color(90, 100, 200);
				entry.OnMouseOut += (evt, element) => entry.BackgroundColor = new Color(63, 82, 151) * 0.7f;
				list.Add(entry);
			}
		}
	}
}
