using BagofChaos.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.NPCs.Bosses
{
	// A King Slime that can't decide what colour it is and teleports whenever it feels like it. Unlocks the Chaotic Beholder.
	[AutoloadBossHead]
	public class ChaoticSlime : ModNPC
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.KingSlime;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.KingSlime];
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
		}

		public override void SetDefaults() {
			NPC.CloneDefaults(NPCID.KingSlime);
			NPC.lifeMax = 3000;
			NPC.damage = 45;
			NPC.defense = 14;
			NPC.boss = true;
			NPC.value = Item.buyPrice(gold: 8);
			NPC.aiStyle = NPCAIStyleID.KingSlime;
			AIType = NPCID.KingSlime;
			AnimationType = NPCID.KingSlime;
			if (!Main.dedServ)
				Music = MusicID.Boss1;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) {
			NPC.lifeMax = (int)(NPC.lifeMax * balance * bossAdjustment);
		}

		public override void AI() {
			NPC.color = Main.DiscoColor;
			// Random teleports, because chaos.
			if (Main.netMode != NetmodeID.MultiplayerClient && Main.rand.NextBool(240)) {
				NPC.TargetClosest();
				Player target = Main.player[NPC.target];
				NPC.Center = target.Center + new Vector2(Main.rand.NextFloat(-300f, 300f), -200f);
				NPC.velocity = Vector2.Zero;
				NPC.netUpdate = true;
				SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
			}
			if (Main.rand.NextBool(3))
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.RainbowMk2, 0f, 0f, 0, Main.DiscoColor);
		}

		public override Color? GetAlpha(Color drawColor) => Main.DiscoColor * 0.9f;

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.Consumables.ChaoticSlimeBag>()));
			npcLoot.Add(ItemDropRule.Common(ItemID.Gel, 1, 50, 100));
			npcLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 5, 15));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Tools.ChaoticDashBox>(), 4));
		}

		public override void OnKill() {
			ChaosEvents.SetDowned(ref ChaosEvents.DownedChaoticSlime);
			ChaosEvents.Announce("ChaoticSlimeDown", Main.DiscoColor);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.ChaoticSlime"));
		}
	}
}
