using System.IO;
using BagofChaos.Content.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BagofChaos.Common.Systems
{
	// World-level events: Delirium, the Corporate Zombie Invasion, Night of Moon Shoes,
	// plus the two "weather" states driven by beacons (Bouncing Jelly Storm, Slippery Frog Rain).
	public class ChaosEvents : ModSystem
	{
		// ---- Delirium ----
		public static bool DeliriumActive;
		public static int DeliriumTimeLeft;
		public const int DeliriumDuration = 60 * 60 * 4; // 4 minutes of silhouettes
		private static int deliriumSwapCooldown;
		public const int DeliriumSwapCooldown = 60 * 15; // capped at once per 15 seconds

		// ---- Corporate Zombie Invasion ----
		public static bool InvasionActive;
		public static int InvasionKills;
		public static int InvasionKillsNeeded = 60;
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
			DeliriumTimeLeft = 0;
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
				tag["deliriumTime"] = DeliriumTimeLeft;
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
			DeliriumTimeLeft = tag.GetInt("deliriumTime");
			InvasionActive = tag.ContainsKey("invasionActive");
			InvasionKills = tag.GetInt("invasionKills");
			CEODefeated = tag.ContainsKey("ceoDefeated");
			MoonShoesActive = tag.ContainsKey("moonShoes");
		}

		public override void NetSend(BinaryWriter writer) {
			writer.WriteFlags(DeliriumActive, InvasionActive, MoonShoesActive, CEODefeated);
			writer.Write(DeliriumTimeLeft);
			writer.Write(InvasionKills);
		}

		public override void NetReceive(BinaryReader reader) {
			reader.ReadFlags(out DeliriumActive, out InvasionActive, out MoonShoesActive, out CEODefeated);
			DeliriumTimeLeft = reader.ReadInt32();
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
			DeliriumTimeLeft = DeliriumDuration;
			deliriumSwapCooldown = DeliriumSwapCooldown;
			Announce("DeliriumStart", new Color(150, 60, 200));
			Sync();
		}

		public static void StartInvasion() {
			if (InvasionActive || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			InvasionActive = true;
			InvasionKills = 0;
			InvasionKillsNeeded = 60 + Main.CurrentFrameFlags.ActivePlayersCount * 20;
			Announce("InvasionStart", new Color(170, 230, 120));
			Sync();
		}

		public static void RegisterInvasionKill(NPC npc) {
			if (!InvasionActive || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			InvasionKills++;
			if (npc.type == ModContent.NPCType<ZombieCEO>())
				CEODefeated = true;
			if (InvasionKills >= InvasionKillsNeeded && CEODefeated) {
				InvasionActive = false;
				Announce("InvasionEnd", new Color(170, 230, 120));
			}
			else if (InvasionKills % 10 == 0) {
				Announce(InvasionKills >= InvasionKillsForCEO && !CEODefeated ? "InvasionCEO" : "InvasionProgress", new Color(170, 230, 120));
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
			// Delirium countdown + the swap
			if (DeliriumActive) {
				DeliriumTimeLeft--;
				if (deliriumSwapCooldown > 0)
					deliriumSwapCooldown--;
				else if (Main.rand.NextBool(90))
					TryDeliriumSwap();

				if (DeliriumTimeLeft <= 0) {
					DeliriumActive = false;
					Announce("DeliriumEnd", new Color(150, 60, 200));
					Sync();
				}
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
			}

			// Corporate zombies don't respect the day/night cycle, but if nobody is around the invasion just stalls.
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
	}
}
