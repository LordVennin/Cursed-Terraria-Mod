using BagofChaos.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common
{
	public static class ChaosUtils
	{
		public static Rectangle ScreenWorldRect => new Rectangle((int)Main.screenPosition.X - 64, (int)Main.screenPosition.Y - 64, Main.screenWidth + 128, Main.screenHeight + 128);

		public static bool OnScreen(Entity entity) => ScreenWorldRect.Intersects(entity.Hitbox);

		/// <summary>True for enemies that are actually fighting the player.</summary>
		public static bool IsHostile(NPC npc) => npc.active && !npc.friendly && !npc.townNPC && npc.lifeMax > 5 && !npc.dontTakeDamage && !NPCID.Sets.ProjectileNPC[npc.type];

		/// <summary>"Non-hostile" for the lawnmower: town folk, critters, anything friendly. Pets are projectiles, so the mower leaves them alone (lucky them).</summary>
		public static bool IsMowable(NPC npc) => npc.active && (npc.townNPC || npc.friendly || npc.CountsAsACritter) && !npc.boss;

		public static void InstantKillNPC(NPC npc) {
			if (!npc.active)
				return;
			// Mirror what vanilla does for unavoidable deaths, then let the netcode know.
			npc.life = 0;
			npc.HitEffect();
			npc.checkDead();
			npc.active = false;
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
		}

		public static int SpawnNPCDirect(int type, Vector2 position, int lifeOverride = -1) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return -1;
			int index = NPC.NewNPC(new EntitySource_Misc("BagofChaos"), (int)position.X, (int)position.Y, type);
			if (index >= 0 && index < Main.maxNPCs) {
				NPC npc = Main.npc[index];
				if (lifeOverride > 0) {
					npc.life = System.Math.Min(lifeOverride, npc.lifeMax);
				}
				npc.CanBeReplacedByOtherNPCs = true;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.SyncNPC, number: index);
			}
			return index;
		}

		public static void FartShockwave(Vector2 position, int sourcePlayer, bool affectNPCs, bool affectLocalPlayer) {
			const float Radius = 900f;
			const float Power = 70f; // "cross map levels of knockback"

			if (Main.netMode != NetmodeID.Server) {
				SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/Fart") { Volume = 1.2f, PitchVariance = 0.3f }, position);
				for (int i = 0; i < 40; i++) {
					Dust dust = Dust.NewDustPerfect(position, DustID.Smoke, Main.rand.NextVector2Circular(12f, 12f), 100, new Color(150, 200, 60), 2f);
					dust.noGravity = true;
				}
			}

			if (affectNPCs) {
				foreach (NPC npc in Main.ActiveNPCs) {
					if (npc.dontTakeDamage && npc.boss)
						continue;
					Vector2 delta = npc.Center - position;
					float distance = delta.Length();
					if (distance > Radius)
						continue;
					Vector2 dir = distance < 1f ? -Vector2.UnitY : delta / distance;
					float falloff = 1f - distance / Radius * 0.5f;
					npc.velocity = dir * Power * falloff + new Vector2(0f, -30f); // more upward: into the stratosphere
					npc.netUpdate = true;
					if (Main.netMode != NetmodeID.MultiplayerClient && !npc.friendly) {
						// Does 1 damage. One. That's the joke.
						npc.SimpleStrikeNPC(1, npc.Center.X < position.X ? -1 : 1, noPlayerInteraction: true);
					}
				}
			}

			if (affectLocalPlayer && Main.netMode != NetmodeID.Server && ModContent.GetInstance<ChaosConfig>().FartAffectsPlayers) {
				Player player = Main.LocalPlayer;
				if (player.active && !player.dead) {
					Vector2 delta = player.Center - position;
					float distance = delta.Length();
					if (distance <= Radius) {
						Vector2 dir = distance < 1f ? -Vector2.UnitY : delta / distance;
						if (player.whoAmI == sourcePlayer)
							dir = -Vector2.UnitY; // you launched yourself. congratulations.
						player.velocity = dir * Power * 0.6f + new Vector2(0f, -24f);
						player.fallStart = (int)(player.position.Y / 16f);
						player.noFallDmg = true; // just for this frame of flight; reset by the engine next tick
					}
				}
			}
		}

		public static void HealNPC(NPC npc, int amount) {
			if (amount <= 0 || npc.life >= npc.lifeMax)
				return;
			npc.life = System.Math.Min(npc.lifeMax, npc.life + amount);
			if (Main.netMode != NetmodeID.Server)
				CombatText.NewText(npc.Hitbox, CombatText.HealLife, amount);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
		}

		public static void HealPlayer(Player player, int amount) {
			if (amount <= 0)
				return;
			if (player.whoAmI == Main.myPlayer) {
				player.Heal(amount);
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient) {
				// Same message the Spectre Hood uses to heal teammates remotely.
				NetMessage.SendData(MessageID.SpiritHeal, number: player.whoAmI, number2: amount);
			}
		}
	}
}
