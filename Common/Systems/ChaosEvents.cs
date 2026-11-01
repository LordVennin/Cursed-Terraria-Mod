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
	// World-level events: the Delirium Moon, the Corporate Zombie Invasion, Night of Moon Shoes,
	// plus the two "weather" states driven by beacons (Bouncing Jelly Storm, Slippery Frog Rain).
	public class ChaosEvents : ModSystem
	{
		// ---- Delirium Moon: 1/3 chance on a new moon night. Every enemy is a silhouette and one keeps swapping places with you. ----
		public static bool DeliriumActive;
		private static int deliriumSwapTimer;

		// ---- Corporate Zombie Invasion: night only, 50 layoffs, vanilla goblin bar ----
		public static bool InvasionActive;
		public static int InvasionKills;
		public const int InvasionKillsNeeded = 50;
		public const int InvasionKillsForCEO = 20;
		public static bool CEODefeated;

		// ---- Night of Moon Shoes ----
		public static bool MoonShoesActive;
		private static bool checkedTonight;

		// ---- Glue kills (for Shlue) ----
		public static int GlueKills;

		// ---- midnight counter (Gremlot's stock) ----
		public static int DayCount;
		private static bool passedMidnight;

		// ---- new bosses ----
		public static bool DownedChaoticSlime;
		public static bool DownedCaterpillar;
		public static bool DownedBonemass;

		// ---- beacon weather ----
		public static bool JellyStormActive => WeatherBeaconSystem.ActiveStyle == WeatherBeaconSystem.StyleJellyStorm;
		public static bool FrogRainActive => WeatherBeaconSystem.ActiveStyle == WeatherBeaconSystem.StyleFrogRain;

		public override void ClearWorld() {
			DeliriumActive = false;
			deliriumSwapTimer = 0;
			InvasionActive = false;
			InvasionKills = 0;
			CEODefeated = false;
			MoonShoesActive = false;
			checkedTonight = false;
			GlueKills = 0;
			DownedChaoticSlime = DownedCaterpillar = DownedBonemass = false;
			DayCount = 0;
			passedMidnight = false;
		}

		public override void SaveWorldData(TagCompound tag) {
			if (DownedChaoticSlime) tag["downedChaoticSlime"] = true;
			if (DownedCaterpillar) tag["downedCaterpillar"] = true;
			if (DownedBonemass) tag["downedBonemass"] = true;
			if (DeliriumActive)
				tag["deliriumActive"] = true;
			if (InvasionActive) {
				tag["invasionActive"] = true;
				tag["invasionKills"] = InvasionKills;
			}
			if (CEODefeated)
				tag["ceoDefeated"] = true;
			if (MoonShoesActive)
				tag["moonShoes"] = true;
			tag["glueKills"] = GlueKills;
			tag["dayCount"] = DayCount;
		}

		public override void LoadWorldData(TagCompound tag) {
			DeliriumActive = tag.ContainsKey("deliriumActive");
			InvasionActive = tag.ContainsKey("invasionActive");
			InvasionKills = tag.GetInt("invasionKills");
			CEODefeated = tag.ContainsKey("ceoDefeated");
			MoonShoesActive = tag.ContainsKey("moonShoes");
			GlueKills = tag.GetInt("glueKills");
			DayCount = tag.GetInt("dayCount");
			DownedChaoticSlime = tag.ContainsKey("downedChaoticSlime");
			DownedCaterpillar = tag.ContainsKey("downedCaterpillar");
			DownedBonemass = tag.ContainsKey("downedBonemass");
		}

		public override void NetSend(BinaryWriter writer) {
			writer.WriteFlags(DeliriumActive, InvasionActive, MoonShoesActive, CEODefeated, DownedChaoticSlime, DownedCaterpillar, DownedBonemass);
			writer.Write(InvasionKills);
			writer.Write(GlueKills);
			writer.Write(DayCount);
		}

		public override void NetReceive(BinaryReader reader) {
			reader.ReadFlags(out DeliriumActive, out InvasionActive, out MoonShoesActive, out CEODefeated, out DownedChaoticSlime, out DownedCaterpillar, out DownedBonemass);
			InvasionKills = reader.ReadInt32();
			GlueKills = reader.ReadInt32();
			DayCount = reader.ReadInt32();
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
			if (DeliriumActive || Main.netMode == NetmodeID.MultiplayerClient || Main.dayTime)
				return;
			DeliriumActive = true;
			deliriumSwapTimer = 60 * Main.rand.Next(10, 16);
			Announce("DeliriumStart", new Color(150, 60, 200));
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
			ReportInvasion();
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
			ReportInvasion();
		}

		// The vanilla event bar (goblin icon). Icon 1 = Goblin Army.
		private static void ReportInvasion() {
			int progress = InvasionActive ? InvasionKills : InvasionKillsNeeded;
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.InvasionProgressReport, -1, -1, null, progress, InvasionKillsNeeded, 1, 0);
			else
				Main.ReportInvasionProgress(progress, InvasionKillsNeeded, 1, 0);
		}

		public static void RegisterGlueKill() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			GlueKills++;
			if (GlueKills % 10 == 0)
				Sync();
		}

		public static void SetDowned(ref bool flag) {
			flag = true;
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
				if (--deliriumSwapTimer <= 0) {
					TryDeliriumSwap();
					deliriumSwapTimer = 60 * Main.rand.Next(10, 16); // once every 10-15 seconds
				}
			}

			if (InvasionActive && Main.GameUpdateCount % 60 == 0)
				ReportInvasion();

			// Midnight (night time 16200): Gremlot restocks.
			if (!Main.dayTime && Main.time >= 16200.0 && !passedMidnight) {
				passedMidnight = true;
				DayCount++;
				Sync();
			}
			if (Main.dayTime)
				passedMidnight = false;

			// Rolled once per night: Moon Shoes (hardmode, 1/12) and the Delirium Moon (new moon, 1/3).
			if (!Main.dayTime && !checkedTonight) {
				checkedTonight = true;
				if (!MoonShoesActive && Main.hardMode && Main.rand.NextBool(12))
					StartMoonShoes();
				if (!DeliriumActive && Main.moonPhase == 4 && NPC.downedBoss1 && Main.rand.NextBool(3))
					StartDelirium();
			}
			if (Main.dayTime) {
				checkedTonight = false;
				if (MoonShoesActive) {
					MoonShoesActive = false;
					Announce("MoonShoesEnd", new Color(240, 240, 160));
					Sync();
				}
				if (DeliriumActive) {
					DeliriumActive = false;
					Announce("DeliriumEnd", new Color(150, 60, 200));
					Sync();
				}
				// The corporate zombies clock out at dawn.
				if (InvasionActive) {
					InvasionActive = false;
					Announce("InvasionDawn", new Color(170, 230, 120));
					Sync();
					ReportInvasion();
				}
			}
		}

		public static bool IsSilhouette(NPC npc) => DeliriumActive && ChaosUtils.IsHostile(npc) && !npc.boss;

		private static void TryDeliriumSwap() {
			// Pick a silhouette and a player on the same screen and swap them.
			foreach (Player player in Main.ActivePlayers) {
				if (player.dead)
					continue;
				Rectangle screen = new Rectangle((int)player.Center.X - 1000, (int)player.Center.Y - 600, 2000, 1200);
				foreach (NPC npc in Main.ActiveNPCs) {
					if (!IsSilhouette(npc) || !screen.Intersects(npc.Hitbox))
						continue;
					if (!Main.rand.NextBool(3))
						continue;
					Vector2 npcPos = npc.Center;
					Vector2 playerPos = player.Center;
					npc.Center = playerPos;
					npc.netUpdate = true;
					TeleportPlayer(player, npcPos);
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
