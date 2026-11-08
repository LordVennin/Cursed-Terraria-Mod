using System.Collections.Generic;
using System.IO;
using System.Linq;
using BagofChaos.Common.GlobalNPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BagofChaos.Common.Systems
{
	// Remembers every town NPC that has ever lived in this world (and its last name), for the NPC Directory.
	public class TownRegistrySystem : ModSystem
	{
		public static readonly Dictionary<int, string> Known = new();

		public static bool Eligible(NPC npc) {
			return npc.active && npc.townNPC && !NPCID.Sets.IsTownPet[npc.type] && npc.type != NPCID.OldMan && npc.type != NPCID.TravellingMerchant
				&& !npc.GetGlobalNPC<ChaosGlobalNPC>().RemoteGhost;
		}

		public static string NameOf(int type) => Known.TryGetValue(type, out string name) ? name : "";

		public override void ClearWorld() => Known.Clear();

		public override void PostUpdateNPCs() {
			if (Main.netMode == NetmodeID.MultiplayerClient || Main.GameUpdateCount % 60 != 0)
				return;
			bool changed = false;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!Eligible(npc))
					continue;
				if (!Known.TryGetValue(npc.type, out string name) || name != npc.GivenName) {
					Known[npc.type] = npc.GivenName;
					changed = true;
				}
			}
			if (changed && Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);
		}

		private static string Key(int type) => type < NPCID.Count ? NPCID.Search.GetName(type) : NPCLoader.GetNPC(type).FullName;

		private static bool TryType(string key, out int type) {
			if (NPCID.Search.TryGetId(key, out type))
				return true;
			if (ModContent.TryFind(key, out ModNPC modNPC)) {
				type = modNPC.Type;
				return true;
			}
			return false;
		}

		public override void SaveWorldData(TagCompound tag) {
			tag["townRegistryKeys"] = Known.Keys.Select(Key).ToList();
			tag["townRegistryNames"] = Known.Values.ToList();
		}

		public override void LoadWorldData(TagCompound tag) {
			Known.Clear();
			IList<string> keys = tag.GetList<string>("townRegistryKeys");
			IList<string> names = tag.GetList<string>("townRegistryNames");
			for (int i = 0; i < keys.Count && i < names.Count; i++) {
				if (TryType(keys[i], out int type))
					Known[type] = names[i];
			}
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write((short)Known.Count);
			foreach (var pair in Known) {
				writer.Write(pair.Key);
				writer.Write(pair.Value ?? "");
			}
		}

		public override void NetReceive(BinaryReader reader) {
			Known.Clear();
			int count = reader.ReadInt16();
			for (int i = 0; i < count; i++) {
				int type = reader.ReadInt32();
				Known[type] = reader.ReadString();
			}
		}
	}
}
