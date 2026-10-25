using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace BagofChaos.Content.NPCs
{
	// A neutral slime. Deals no contact damage, but everything it touches gets the Glue debuff. Then it bounces off with a new friend attached.
	public class GlueSlime : ModNPC
	{
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 2;
			NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Velocity = 1f });
		}

		public override void SetDefaults() {
			NPC.width = 30;
			NPC.height = 20;
			NPC.damage = 0;
			NPC.defense = 2;
			NPC.lifeMax = 40;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 25f;
			NPC.knockBackResist = 0.8f;
			NPC.aiStyle = NPCAIStyleID.Slime;
			NPC.alpha = 30;
			AIType = NPCID.BlueSlime;
			AnimationType = NPCID.BlueSlime;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) {
			return SpawnCondition.OverworldDaySlime.Chance * 0.12f + SpawnCondition.OverworldNightMonster.Chance * 0.05f;
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

		public override void AI() {
			// Glue anything we touch. NPCs on the authority, players on their own client.
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				foreach (NPC other in Main.ActiveNPCs) {
					if (other.whoAmI == NPC.whoAmI || other.type == Type || !other.Hitbox.Intersects(NPC.Hitbox))
						continue;
					if (!other.HasBuff<GlueDebuff>())
						other.AddBuff(ModContent.BuffType<GlueDebuff>(), 60 * 20);
				}
			}
			if (Main.netMode != NetmodeID.Server) {
				Player local = Main.LocalPlayer;
				if (local.active && !local.dead && local.Hitbox.Intersects(NPC.Hitbox))
					local.AddBuff(ModContent.BuffType<GlueDebuff>(), 60 * 20);
			}
			if (Main.rand.NextBool(8)) {
				Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Cloud, 0f, 0f, 100, new Color(255, 250, 220), 0.8f);
				dust.noGravity = true;
			}
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.Gel, 1, 2, 5));
		}

		public override void HitEffect(NPC.HitInfo hit) {
			for (int i = 0; i < (NPC.life <= 0 ? 20 : 4); i++) {
				Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Cloud, hit.HitDirection, -1f, 100, new Color(255, 250, 220), 1.2f);
				dust.velocity *= 0.6f;
			}
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.GlueSlime"),
			]);
		}
	}
}
