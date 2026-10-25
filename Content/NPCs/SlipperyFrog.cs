using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace BagofChaos.Content.NPCs
{
	// A critter that slides left and right across the screen and teleports up or down a block whenever it hits something.
	public class SlipperyFrog : ModNPC
	{
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 4;
			Main.npcCatchable[Type] = true;
			NPCID.Sets.CountsAsCritter[Type] = true;
			NPCID.Sets.TakesDamageFromHostilesWithoutBeingFriendly[Type] = true;
			NPCID.Sets.TownCritter[Type] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
		}

		public override void SetDefaults() {
			NPC.width = 20;
			NPC.height = 14;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 5;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.2f;
			NPC.aiStyle = -1;
			NPC.catchItem = ModContent.ItemType<Items.Consumables.SlipperyFrogItem>();
			NPC.npcSlots = 0.3f;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([
				BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
				new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.SlipperyFrog"),
			]);
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) {
			if (Common.Systems.ChaosEvents.FrogRainActive)
				return SpawnCondition.OverworldDay.Chance * 2f;
			return SpawnCondition.OverworldDay.Chance * 0.08f;
		}

		public override void AI() {
			if (NPC.ai[0] == 0f) {
				NPC.ai[0] = Main.rand.NextBool() ? -1f : 1f;
				NPC.netUpdate = true;
			}
			float speed = 4.5f;
			NPC.velocity.X = NPC.ai[0] * speed;
			NPC.spriteDirection = NPC.direction = (int)NPC.ai[0];

			// Hit a wall? Teleport up or down a block and keep going. If that's also blocked, turn around.
			if (NPC.collideX) {
				int dir = Main.rand.NextBool() ? -1 : 1;
				Vector2 tryPos = NPC.position + new Vector2(NPC.ai[0] * 18f, dir * 16f);
				if (!Collision.SolidCollision(tryPos, NPC.width, NPC.height)) {
					Poof();
					NPC.position = tryPos;
					Poof();
				}
				else {
					tryPos = NPC.position + new Vector2(NPC.ai[0] * 18f, -dir * 16f);
					if (!Collision.SolidCollision(tryPos, NPC.width, NPC.height)) {
						Poof();
						NPC.position = tryPos;
						Poof();
					}
					else {
						NPC.ai[0] *= -1f;
					}
				}
				NPC.netUpdate = true;
			}

			// Occasionally flip for no reason. It's slippery.
			if (Main.rand.NextBool(600)) {
				NPC.ai[0] *= -1f;
				NPC.netUpdate = true;
			}
		}

		private void Poof() {
			if (Main.netMode == NetmodeID.Server)
				return;
			for (int i = 0; i < 6; i++) {
				Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Water, 0f, 0f, 100, default, 1.2f);
				dust.noGravity = true;
			}
		}

		public override void FindFrame(int frameHeight) {
			NPC.frameCounter++;
			NPC.frame.Y = ((int)(NPC.frameCounter / 6) % 4) * frameHeight;
		}

		public override void HitEffect(NPC.HitInfo hit) {
			if (NPC.life <= 0 && Main.netMode != NetmodeID.Server) {
				for (int i = 0; i < 10; i++)
					Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.GreenBlood, hit.HitDirection, -1f);
				SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);
			}
		}
	}
}
