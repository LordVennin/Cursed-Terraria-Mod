using System.IO;
using BagofChaos.Common;
using BagofChaos.Common.GlobalNPCs;
using BagofChaos.Common.Players;
using BagofChaos.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos
{
	// Bag of Chaos: a Terraria mod that is not meant to be taken seriously.
	// Everything in here was built from loose text files of ideas, with the blanks filled in by whatever seemed funniest.
	public class BagofChaos : Mod
	{
		public const string AssetPath = $"{nameof(BagofChaos)}/Assets/";

		public static BagofChaos Instance => ModContent.GetInstance<BagofChaos>();

		internal enum MessageType : byte
		{
			MoveNPC,          // client -> server: physically relocate an NPC (NPC Hand, Gparrling Hook, Succ)
			KillNPC,          // client -> server: instantly kill an NPC (NPC Lawnmower)
			SpawnNPC,         // client -> server: spawn an NPC at a position (Tool Gun duplication, Chain Can tin cans, bottles)
			SpawnBoss,        // client -> server: spawn a boss on a player (bottled enemies)
			NPCVisual,        // client -> server -> clients: sync cosmetic NPC flags (googly eyes, slashed, stretched)
			Fart,             // client -> server -> clients: the Fart Reverb shockwave
			PlaceCloud,       // client -> server: Sky in a Bottle cloud platform
			WeatherBeacon,    // client -> server: a weather beacon was placed, toggled or broken
			ApplyBuffToNPC,   // client -> server: apply a buff to an NPC (glue, life link) so the server-side logic sees it
			Succ,             // client -> server -> clients: every player on screen gets pulled to the user
			TetherPull,       // client -> server -> client: Team Tether pulls (and heals) a specific player
			PushNPC,          // client -> server: add velocity to an NPC (Succ.)
			KillProjectile,   // client -> server: remove a projectile the client doesn't own (Warpaint capture)
			SnailMark,        // client -> server: a snail flail hit this NPC (loot is decided on the server)
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			MessageType msgType = (MessageType)reader.ReadByte();

			switch (msgType) {
				case MessageType.MoveNPC: {
					int npcIndex = reader.ReadInt16();
					Vector2 position = reader.ReadVector2();
					if (Main.netMode == NetmodeID.Server && npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active) {
						NPC npc = Main.npc[npcIndex];
						npc.position = position;
						npc.velocity = Vector2.Zero;
						npc.netUpdate = true;
					}
					break;
				}
				case MessageType.KillNPC: {
					int npcIndex = reader.ReadInt16();
					if (Main.netMode == NetmodeID.Server && npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active) {
						ChaosUtils.InstantKillNPC(Main.npc[npcIndex]);
					}
					break;
				}
				case MessageType.SpawnNPC: {
					int type = reader.ReadInt32();
					Vector2 position = reader.ReadVector2();
					int lifeOverride = reader.ReadInt32();
					Vector2 velocity = reader.ReadVector2();
					if (Main.netMode == NetmodeID.Server) {
						ChaosUtils.SpawnNPCDirect(type, position, lifeOverride, velocity);
					}
					break;
				}
				case MessageType.SpawnBoss: {
					int player = reader.ReadByte();
					int type = reader.ReadInt32();
					if (Main.netMode == NetmodeID.Server && !NPC.AnyNPCs(type))
						NPC.SpawnOnPlayer(player, type);
					break;
				}
				case MessageType.NPCVisual: {
					int npcIndex = reader.ReadInt16();
					byte flags = reader.ReadByte();
					if (npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active) {
						Main.npc[npcIndex].GetGlobalNPC<ChaosGlobalNPC>().ApplyVisualFlags(flags);
					}
					if (Main.netMode == NetmodeID.Server) {
						ModPacket packet = GetPacket();
						packet.Write((byte)MessageType.NPCVisual);
						packet.Write((short)npcIndex);
						packet.Write(flags);
						packet.Send(-1, whoAmI);
					}
					break;
				}
				case MessageType.Fart: {
					Vector2 position = reader.ReadVector2();
					int sourcePlayer = reader.ReadByte();
					if (Main.netMode == NetmodeID.Server) {
						ChaosUtils.FartShockwave(position, sourcePlayer, affectNPCs: true, affectLocalPlayer: false);
						ModPacket packet = GetPacket();
						packet.Write((byte)MessageType.Fart);
						packet.WriteVector2(position);
						packet.Write((byte)sourcePlayer);
						packet.Send(-1, whoAmI);
					}
					else {
						ChaosUtils.FartShockwave(position, sourcePlayer, affectNPCs: false, affectLocalPlayer: true);
					}
					break;
				}
				case MessageType.PlaceCloud: {
					int x = reader.ReadInt32();
					int y = reader.ReadInt32();
					int width = reader.ReadByte();
					if (Main.netMode == NetmodeID.Server) {
						CloudPlatformSystem.PlaceCloudDirect(x, y, width);
					}
					break;
				}
				case MessageType.WeatherBeacon: {
					int x = reader.ReadInt32();
					int y = reader.ReadInt32();
					int style = reader.ReadByte();
					bool placed = reader.ReadBoolean();
					if (Main.netMode == NetmodeID.Server) {
						if (placed)
							WeatherBeaconSystem.BeaconPlacedDirect(x, y, style);
						else
							WeatherBeaconSystem.BeaconRemovedDirect(x, y);
					}
					break;
				}
				case MessageType.ApplyBuffToNPC: {
					int npcIndex = reader.ReadInt16();
					int buffType = reader.ReadInt32();
					int time = reader.ReadInt32();
					if (Main.netMode == NetmodeID.Server && npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active) {
						Main.npc[npcIndex].AddBuff(buffType, time);
					}
					break;
				}
				case MessageType.Succ: {
					int sourcePlayer = reader.ReadByte();
					Vector2 position = reader.ReadVector2();
					if (Main.netMode == NetmodeID.Server) {
						ModPacket packet = GetPacket();
						packet.Write((byte)MessageType.Succ);
						packet.Write((byte)sourcePlayer);
						packet.WriteVector2(position);
						packet.Send(-1, whoAmI);
					}
					else {
						ChaosUtils.SuccLocalPlayer(sourcePlayer, position);
					}
					break;
				}
				case MessageType.TetherPull: {
					int target = reader.ReadByte();
					int source = reader.ReadByte();
					if (Main.netMode == NetmodeID.Server) {
						ModPacket packet = GetPacket();
						packet.Write((byte)MessageType.TetherPull);
						packet.Write((byte)target);
						packet.Write((byte)source);
						packet.Send(target);
					}
					else if (target == Main.myPlayer) {
						ChaosUtils.TetherPullLocalPlayer(source);
					}
					break;
				}
				case MessageType.PushNPC: {
					int npcIndex = reader.ReadInt16();
					Vector2 impulse = reader.ReadVector2();
					if (Main.netMode == NetmodeID.Server && npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
						ChaosUtils.PushNPC(Main.npc[npcIndex], impulse);
					break;
				}
				case MessageType.SnailMark: {
					int npcIndex = reader.ReadInt16();
					int mark = reader.ReadByte();
					if (Main.netMode == NetmodeID.Server && npcIndex >= 0 && npcIndex < Main.maxNPCs && Main.npc[npcIndex].active)
						Main.npc[npcIndex].GetGlobalNPC<ChaosGlobalNPC>().MarkForSnails(mark);
					break;
				}
				case MessageType.KillProjectile: {
					int identity = reader.ReadInt16();
					int owner = reader.ReadByte();
					if (Main.netMode == NetmodeID.Server) {
						int index = Projectile.GetByUUID(owner, identity);
						if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].active)
							Main.projectile[index].Kill();
					}
					break;
				}
				default:
					Logger.WarnFormat("BagofChaos: Unknown Message type: {0}", msgType);
					break;
			}
		}

		// Convenience senders used all over the mod. Each one does the right thing in singleplayer too.
		internal static void SendMoveNPC(NPC npc, Vector2 position) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			ModPacket packet = Instance.GetPacket();
			packet.Write((byte)MessageType.MoveNPC);
			packet.Write((short)npc.whoAmI);
			packet.WriteVector2(position);
			packet.Send();
		}

		internal static void SendPushNPC(NPC npc, Vector2 impulse) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.PushNPC);
				packet.Write((short)npc.whoAmI);
				packet.WriteVector2(impulse);
				packet.Send();
			}
			else {
				ChaosUtils.PushNPC(npc, impulse);
			}
		}

		/// <summary>Removes a projectile everywhere, even one owned by the server or another player.</summary>
		internal static void SendKillProjectile(Projectile projectile) {
			if (Main.netMode == NetmodeID.MultiplayerClient && projectile.owner != Main.myPlayer) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.KillProjectile);
				packet.Write((short)projectile.identity);
				packet.Write((byte)projectile.owner);
				packet.Send();
				projectile.active = false; // hide it locally right away; the server's kill sync follows
			}
			else {
				projectile.Kill();
			}
		}

		internal static void SendSnailMark(NPC npc, int mark) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			ModPacket packet = Instance.GetPacket();
			packet.Write((byte)MessageType.SnailMark);
			packet.Write((short)npc.whoAmI);
			packet.Write((byte)mark);
			packet.Send();
		}

		internal static void SendKillNPC(NPC npc) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.KillNPC);
				packet.Write((short)npc.whoAmI);
				packet.Send();
			}
			else {
				ChaosUtils.InstantKillNPC(npc);
			}
		}

		internal static void SendSpawnNPC(int type, Vector2 position, int lifeOverride = -1, Vector2 velocity = default) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.SpawnNPC);
				packet.Write(type);
				packet.WriteVector2(position);
				packet.Write(lifeOverride);
				packet.WriteVector2(velocity);
				packet.Send();
			}
			else {
				ChaosUtils.SpawnNPCDirect(type, position, lifeOverride, velocity);
			}
		}

		internal static void SendSpawnBoss(int player, int type) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.SpawnBoss);
				packet.Write((byte)player);
				packet.Write(type);
				packet.Send();
			}
			else if (!NPC.AnyNPCs(type)) {
				NPC.SpawnOnPlayer(player, type);
			}
		}

		internal static void SendNPCVisual(NPC npc) {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			ModPacket packet = Instance.GetPacket();
			packet.Write((byte)MessageType.NPCVisual);
			packet.Write((short)npc.whoAmI);
			packet.Write(npc.GetGlobalNPC<ChaosGlobalNPC>().GetVisualFlags());
			packet.Send();
		}

		internal static void SendFart(Vector2 position, int sourcePlayer) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.Fart);
				packet.WriteVector2(position);
				packet.Write((byte)sourcePlayer);
				packet.Send();
			}
			else {
				ChaosUtils.FartShockwave(position, sourcePlayer, affectNPCs: true, affectLocalPlayer: true);
			}
		}

		internal static void SendPlaceCloud(int x, int y, int width) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.PlaceCloud);
				packet.Write(x);
				packet.Write(y);
				packet.Write((byte)width);
				packet.Send();
			}
			else {
				CloudPlatformSystem.PlaceCloudDirect(x, y, width);
			}
		}

		internal static void SendWeatherBeacon(int x, int y, int style, bool placed) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.WeatherBeacon);
				packet.Write(x);
				packet.Write(y);
				packet.Write((byte)style);
				packet.Write(placed);
				packet.Send();
			}
			else if (placed) {
				WeatherBeaconSystem.BeaconPlacedDirect(x, y, style);
			}
			else {
				WeatherBeaconSystem.BeaconRemovedDirect(x, y);
			}
		}

		internal static void SendApplyBuffToNPC(NPC npc, int buffType, int time) {
			npc.AddBuff(buffType, time);
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			ModPacket packet = Instance.GetPacket();
			packet.Write((byte)MessageType.ApplyBuffToNPC);
			packet.Write((short)npc.whoAmI);
			packet.Write(buffType);
			packet.Write(time);
			packet.Send();
		}

		internal static void SendSucc(int sourcePlayer, Vector2 position) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.Succ);
				packet.Write((byte)sourcePlayer);
				packet.WriteVector2(position);
				packet.Send();
			}
		}

		internal static void SendTetherPull(int target, int source) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = Instance.GetPacket();
				packet.Write((byte)MessageType.TetherPull);
				packet.Write((byte)target);
				packet.Write((byte)source);
				packet.Send();
			}
		}
	}
}
