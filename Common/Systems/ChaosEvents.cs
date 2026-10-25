using System.Collections.Generic;
using System.IO;
using BagofChaos.Content.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace BagofChaos.Common.Systems
{
	// World-level events: Delirium, the Corporate Zombie Invasion, Night of Moon Shoes,
	// plus the two "weather" states driven by beacons (Bouncing Jelly Storm, Slippery Frog Rain).
	public class ChaosEvents : ModSystem
	{
		// ---- Delirium: 100 silhouettes must fall ----
		public static bool DeliriumActive;
		public static int DeliriumKills;
		public const int DeliriumKillsNeeded = 100;
		private static int deliriumSwapCooldown;
		public const int DeliriumSwapCooldown = 60 * 15; // capped at once per 15 seconds

		// ---- Corporate Zombie Invasion: night only, 100 layoffs ----
		public static bool InvasionActive;
		public static int InvasionKills;
		public const int InvasionKillsNeeded = 100;
		public const int InvasionKillsForCEO = 30;
		public static bool CEODefeated;

		// ---- Night of Moon Shoes ----
		public static bool MoonShoesActive;
		private static bool checkedTonight;

		// ---- beacon weather ----
		public static bool JellyStormActive => WeatherBeaconSystem.ActiveStyle == WeatherBeaconSystem.StyleJellyStorm;
		public static bool FrogRainActive => WeatherBeaconSystem.ActiveStyle == WeatherBeaconSystem.StyleFrogRain;

		public override void ClearWorld() {
			DeliriumActive = false;
			DeliriumKills = 0;
			deliriumSwapCooldown = 0;
			InvasionActive = false;
			InvasionKills = 0;
			CEODefeated = false;
			MoonShoesActive = false;
			checkedTonight = false;
		}

		public override void SaveWorldData(TagCompound tag) {
			if (DeliriumActive) {
				tag["deliriumActive"] = true;
				tag["deliriumKills"] = DeliriumKills;
			}
			if (InvasionActive) {
				tag["invasionActive"] = true;
				tag["invasionKills"] = InvasionKills;
			}
			if (CEODefeated)
				tag["ceoDefeated"] = true;
			if (MoonShoesActive)
				tag["moonShoes"] = true;
		}

		public override void LoadWorldData(TagCompound tag) {
			DeliriumActive = tag.ContainsKey("deliriumActive");
			DeliriumKills = tag.GetInt("deliriumKills");
			InvasionActive = tag.ContainsKey("invasionActive");
			InvasionKills = tag.GetInt("invasionKills");
			CEODefeated = tag.ContainsKey("ceoDefeated");
			MoonShoesActive = tag.ContainsKey("moonShoes");
		}

		public override void NetSend(BinaryWriter writer) {
			writer.WriteFlags(DeliriumActive, InvasionActive, MoonShoesActive, CEODefeated);
			writer.Write(DeliriumKills);
			writer.Write(InvasionKills);
		}

		public override void NetReceive(BinaryReader reader) {
			reader.ReadFlags(out DeliriumActive, out InvasionActive, out MoonShoesActive, out CEODefeated);
			DeliriumKills = reader.ReadInt32();
			InvasionKills = reader.ReadInt32();
		}

		private static void Sync() {
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);
		}

		public static void Announce(string key, Color color) {
			string text = Language.GetTextValue("Mods.BagofChaos.Events." + key);
			if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
			else
				Main.NewText(text, color);
		}

		public static bool NearSpawn(Player player) {
			Vector2 spawn = new Vector2(Main.spawnTileX * 16f, Main.spawnTileY * 16f);
			return Vector2.Distance(player.Center, spawn) < 3000f;
		}

		// ------------------------------------------------------------------------------------------------------------
		public static void StartDelirium() {
			if (DeliriumActive || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			DeliriumActive = true;
			DeliriumKills = 0;
			deliriumSwapCooldown = DeliriumSwapCooldown;
			Announce("DeliriumStart", new Color(150, 60, 200));
			Sync();
		}

		public static void RegisterDeliriumKill() {
			if (!DeliriumActive || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			DeliriumKills++;
			if (DeliriumKills >= DeliriumKillsNeeded) {
				DeliriumActive = false;
				Announce("DeliriumEnd", new Color(150, 60, 200));
			}
			Sync();
		}

		public static void StartInvasion() {
			if (InvasionActive || Main.netMode == NetmodeID.MultiplayerClient || Main.dayTime)
				return;
			InvasionActive = true;
			InvasionKills = 0;
			CEODefeated = false;
			Announce("InvasionStart", new Color(170, 230, 120));
			Sync();
		}

		public static void RegisterInvasionKill(NPC npc) {
			if (!InvasionActive || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			InvasionKills++;
			if (npc.type == ModContent.NPCType<ZombieCEO>())
				CEODefeated = true;
			if (InvasionKills >= InvasionKillsNeeded) {
				InvasionActive = false;
				Announce("InvasionEnd", new Color(170, 230, 120));
			}
			else if (InvasionKills == InvasionKillsForCEO) {
				Announce("InvasionCEO", new Color(170, 230, 120));
			}
			Sync();
		}

		public static void StartMoonShoes() {
			if (MoonShoesActive || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			MoonShoesActive = true;
			Announce("MoonShoesStart", new Color(240, 240, 160));
			Sync();
		}

		// ------------------------------------------------------------------------------------------------------------
		public override void PostUpdateWorld() {
			if (DeliriumActive) {
				if (deliriumSwapCooldown > 0)
					deliriumSwapCooldown--;
				else if (Main.rand.NextBool(90))
					TryDeliriumSwap();
			}

			// Night of Moon Shoes: rolled once per night, ends at dawn.
			if (!Main.dayTime && !checkedTonight) {
				checkedTonight = true;
				if (!MoonShoesActive && Main.hardMode && Main.rand.NextBool(12))
					StartMoonShoes();
			}
			if (Main.dayTime) {
				checkedTonight = false;
				if (MoonShoesActive) {
					MoonShoesActive = false;
					Announce("MoonShoesEnd", new Color(240, 240, 160));
					Sync();
				}
				// The corporate zombies clock out at dawn.
				if (InvasionActive) {
					InvasionActive = false;
					Announce("InvasionDawn", new Color(170, 230, 120));
					Sync();
				}
			}
		}

		private static void TryDeliriumSwap() {
			// Pick a silhouette and a player on the same screen and swap them.
			foreach (Player player in Main.ActivePlayers) {
				if (player.dead)
					continue;
				Rectangle screen = new Rectangle((int)player.Center.X - 1000, (int)player.Center.Y - 600, 2000, 1200);
				foreach (NPC npc in Main.ActiveNPCs) {
					if (!npc.GetGlobalNPC<GlobalNPCs.ChaosGlobalNPC>().Silhouette || !screen.Intersects(npc.Hitbox))
						continue;
					if (!Main.rand.NextBool(3))
						continue;
					Vector2 npcPos = npc.Center;
					Vector2 playerPos = player.Center;
					npc.Center = playerPos;
					npc.netUpdate = true;
					TeleportPlayer(player, npcPos);
					deliriumSwapCooldown = DeliriumSwapCooldown;
					return;
				}
			}
		}

		private static void TeleportPlayer(Player player, Vector2 center) {
			Vector2 position = center - new Vector2(player.width / 2f, player.height / 2f);
			if (Main.netMode == NetmodeID.Server) {
				RemoteClient.CheckSection(player.whoAmI, position);
				player.Teleport(position, 1);
				NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, player.whoAmI, position.X, position.Y, 1);
			}
			else {
				player.Teleport(position, 1);
			}
		}

		// ------------------------------------------------------------------------------------------------------------
		// Progress bars, drawn like the vanilla invasion bar (bottom right).
		// ------------------------------------------------------------------------------------------------------------
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Invasion Progress Bars"));
			if (index < 0)
				index = layers.Count - 1;
			layers.Insert(index + 1, new LegacyGameInterfaceLayer("BagofChaos: Event Progress", DrawProgressBars, InterfaceScaleType.UI));
		}

		private static bool DrawProgressBars() {
			if (Main.gameMenu || Main.LocalPlayer == null)
				return true;
			int slot = 0;
			if (DeliriumActive && NearSpawn(Main.LocalPlayer))
				DrawBar(slot++, Language.GetTextValue("Mods.BagofChaos.Events.DeliriumName"), DeliriumKills, DeliriumKillsNeeded, new Color(150, 60, 200));
			if (InvasionActive && NearSpawn(Main.LocalPlayer))
				DrawBar(slot++, Language.GetTextValue("Mods.BagofChaos.Events.InvasionName"), InvasionKills, InvasionKillsNeeded, new Color(120, 190, 90));
			return true;
		}

		private static void DrawBar(int slot, string name, int progress, int max, Color accent) {
			SpriteBatch sb = Main.spriteBatch;
			Texture2D pixel = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/Pixel").Value;
			int uiWidth = (int)(Main.screenWidth / Main.UIScale);
			int uiHeight = (int)(Main.screenHeight / Main.UIScale);
			// Same corner as the vanilla invasion bar; the vanilla one (if any) sits in the first slot area, so we stack upward.
			Vector2 center = new Vector2(uiWidth - 120, uiHeight - 40 - slot * 70 - (Main.invasionProgressDisplayLeft > 0 ? 70 : 0));
			float percent = MathHelper.Clamp(progress / (float)max, 0f, 1f);

			Rectangle panel = new Rectangle((int)center.X - 100, (int)center.Y - 36, 200, 72);
			Utils.DrawInvBG(sb, panel, new Color(63, 65, 151, 255) * 0.785f);
			Utils.DrawBorderString(sb, name, new Vector2(center.X, center.Y - 22), accent, 0.9f, 0.5f, 0.5f);

			Rectangle bar = new Rectangle((int)center.X - 85, (int)center.Y + 4, 170, 14);
			sb.Draw(pixel, bar, Color.Black * 0.6f);
			sb.Draw(pixel, new Rectangle(bar.X + 2, bar.Y + 2, (int)((bar.Width - 4) * percent), bar.Height - 4), accent);
			Utils.DrawBorderString(sb, $"{(int)(percent * 100f)}%", new Vector2(center.X, bar.Y + bar.Height / 2f + 1), Color.White, 0.75f, 0.5f, 0.5f);
		}
	}
}
