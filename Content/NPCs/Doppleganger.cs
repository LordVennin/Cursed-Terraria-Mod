using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace BagofChaos.Content.NPCs
{
	// Rarely shows up next to town NPCs at night wearing your name and your clothes. Harmless. Hit it and it "leaves".
	public class Doppleganger : ModNPC
	{
		private int HostPlayer { get => (int)NPC.ai[0]; set => NPC.ai[0] = value; }

		public override string Texture => "Terraria/Images/NPC_" + NPCID.Guide; // never actually drawn

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 1;
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
		}

		public override void SetDefaults() {
			NPC.width = 20;
			NPC.height = 42;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 1;
			NPC.friendly = true;
			NPC.aiStyle = -1;
			NPC.knockBackResist = 0f;
			NPC.value = 0f;
			NPC.npcSlots = 0.5f;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath6;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) {
			if (Main.dayTime || !spawnInfo.Player.ZoneOverworldHeight || NPC.AnyNPCs(Type))
				return 0f;
			// Only next to town NPCs.
			Vector2 spawn = new Vector2(spawnInfo.SpawnTileX * 16f, spawnInfo.SpawnTileY * 16f);
			foreach (NPC npc in Main.ActiveNPCs) {
				if (npc.townNPC && Vector2.Distance(npc.Center, spawn) < 400f)
					return 0.03f;
			}
			return 0f;
		}

		public override void OnSpawn(IEntitySource source) {
			NPC.TargetClosest(false);
			HostPlayer = NPC.target;
			if (HostPlayer >= 0 && HostPlayer < Main.maxPlayers)
				NPC.GivenName = Main.player[HostPlayer].name;
			NPC.netUpdate = true;
		}

		public override void AI() {
			NPC.velocity.X *= 0.8f;
			if (HostPlayer >= 0 && HostPlayer < Main.maxPlayers) {
				Player host = Main.player[HostPlayer];
				NPC.direction = NPC.spriteDirection = host.Center.X < NPC.Center.X ? -1 : 1; // always looking at you
				if (NPC.GivenName != host.name)
					NPC.GivenName = host.name;
			}
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

		public override bool? CanBeHitByItem(Player player, Item item) => true;
		public override bool? CanBeHitByProjectile(Projectile projectile) => projectile.owner >= 0 && !projectile.npcProj ? true : null;

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (HostPlayer < 0 || HostPlayer >= Main.maxPlayers || !Main.player[HostPlayer].active)
				return false;
			Player host = Main.player[HostPlayer];
			// Draw the host player's look at our position. The renderer reads direction from the player, so mirror it briefly.
			int oldDir = host.direction;
			host.direction = NPC.direction;
			Vector2 position = NPC.Bottom - new Vector2(host.width / 2f, host.height);
			Main.PlayerRenderer.DrawPlayer(Main.Camera, host, position, 0f, host.fullRotationOrigin, 0f, 1f);
			host.direction = oldDir;
			return false;
		}

		public override void HitEffect(NPC.HitInfo hit) {
			if (NPC.life > 0 || Main.netMode == NetmodeID.Server)
				return;
			Vector2 smoke = NPC.Center - new Vector2(10f);
			Utils.PoofOfSmoke(smoke);
		}

		public override void OnKill() {
			string name = NPC.GivenName;
			string text = Language.GetTextValue("Announcement.HasLeft", name);
			if (Main.netMode == NetmodeID.Server)
				Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), new Color(50, 125, 255));
			else
				Main.NewText(text, new Color(50, 125, 255));
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) { } // nothing

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.Doppleganger"));
		}
	}
}
