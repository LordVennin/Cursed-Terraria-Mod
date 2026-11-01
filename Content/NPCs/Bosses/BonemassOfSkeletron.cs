using System;
using BagofChaos.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Content.NPCs.Bosses
{
	// 1/4 of the time the Old Man's curse summons this instead of Skeletron. A slow pile of bones that spews bones,
	// sweeps the screen with bony hands, rains bones and raises skeletons. Beating it counts as beating Skeletron.
	[AutoloadBossHead]
	public class BonemassOfSkeletron : ModNPC
	{
		private enum Attack { Spew, Hands, Rain, Raise }

		private ref float Timer => ref NPC.ai[0];
		private ref float AttackIndex => ref NPC.ai[1];

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
		}

		public override void SetDefaults() {
			NPC.width = 120;
			NPC.height = 100;
			NPC.damage = 40;
			NPC.defense = 12;
			NPC.lifeMax = 4400;
			NPC.HitSound = SoundID.NPCHit2;
			NPC.DeathSound = SoundID.NPCDeath2;
			NPC.value = Item.buyPrice(gold: 5);
			NPC.knockBackResist = 0f;
			NPC.boss = true;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.npcSlots = 10f;
			NPC.aiStyle = -1;
			if (!Main.dedServ)
				Music = MusicID.Boss1;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) {
			NPC.lifeMax = (int)(NPC.lifeMax * balance * bossAdjustment);
		}

		public override void AI() {
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (target.dead || !target.active || Main.dayTime) {
				NPC.velocity.Y -= 0.2f; // leaves at dawn or when everyone's dead
				if (NPC.timeLeft > 30) NPC.timeLeft = 30;
				return;
			}

			// Slow drift toward the player, hovering above.
			Vector2 goal = target.Center + new Vector2(0f, -160f);
			Vector2 toGoal = goal - NPC.Center;
			NPC.velocity = (NPC.velocity * 40f + toGoal.SafeNormalize(Vector2.Zero) * 2.5f) / 41f;
			NPC.rotation = NPC.velocity.X * 0.02f;

			Timer++;
			Attack attack = (Attack)((int)AttackIndex % 4);
			switch (attack) {
				case Attack.Spew:
					if (Timer % 20 == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
						Vector2 velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.4f) * 9f;
						Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity, ProjectileID.SkeletonBone, 25, 3f, Main.myPlayer);
					}
					if (Timer >= 180) NextAttack();
					break;
				case Attack.Hands:
					if (Timer == 1 && Main.netMode != NetmodeID.MultiplayerClient) {
						float y = target.Center.Y;
						Projectile.NewProjectile(NPC.GetSource_FromAI(), new Vector2(target.Center.X - 900f, y), new Vector2(12f, 0f), ModContent.ProjectileType<BoneHandSweep>(), 35, 6f, Main.myPlayer);
						Projectile.NewProjectile(NPC.GetSource_FromAI(), new Vector2(target.Center.X + 900f, y - 60f), new Vector2(-12f, 0f), ModContent.ProjectileType<BoneHandSweep>(), 35, 6f, Main.myPlayer);
						SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
					}
					if (Timer >= 200) NextAttack();
					break;
				case Attack.Rain:
					if (Timer % 10 == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
						Vector2 spawn = new Vector2(target.Center.X + Main.rand.NextFloat(-500f, 500f), target.Center.Y - 600f);
						Projectile.NewProjectile(NPC.GetSource_FromAI(), spawn, new Vector2(0f, 10f), ProjectileID.SkeletonBone, 20, 2f, Main.myPlayer);
					}
					if (Timer >= 150) NextAttack();
					break;
				case Attack.Raise:
					if (Timer == 30 && Main.netMode != NetmodeID.MultiplayerClient) {
						for (int i = 0; i < 3; i++)
							Common.ChaosUtils.SpawnNPCDirect(NPCID.Skeleton, NPC.Bottom + new Vector2(Main.rand.NextFloat(-80f, 80f), 0f));
						SoundEngine.PlaySound(SoundID.NPCDeath2, NPC.Center);
					}
					if (Timer >= 120) NextAttack();
					break;
			}
			if (Main.rand.NextBool(4))
				Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Bone);
		}

		private void NextAttack() {
			Timer = 0;
			AttackIndex++;
			NPC.netUpdate = true;
		}

		public override void FindFrame(int frameHeight) {
			NPC.frameCounter++;
			NPC.frame.Y = ((int)(NPC.frameCounter / 10) % 4) * frameHeight;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			// Same drops as Skeletron (it is, legally, Skeletron).
			foreach (IItemDropRule rule in Main.ItemDropsDB.GetRulesForNPCID(NPCID.SkeletronHead, false))
				npcLoot.Add(rule);
		}

		public override void OnKill() {
			NPC.SetEventFlagCleared(ref NPC.downedBoss3, GameEventClearedID.DefeatedSkeletron);
			ChaosEvents.SetDowned(ref ChaosEvents.DownedBonemass);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.TheDungeon, new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.BonemassOfSkeletron")]);
		}
	}

	// A huge bony hand sweeping across the screen. Drawn with Skeletron's hand sprite.
	public class BoneHandSweep : ModProjectile
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.SkeletronHand;

		public override void SetDefaults() {
			Projectile.width = 52;
			Projectile.height = 52;
			Projectile.hostile = true;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 200;
		}

		public override void AI() {
			Projectile.rotation = Projectile.velocity.X > 0 ? MathHelper.PiOver2 : -MathHelper.PiOver2;
			if (Main.rand.NextBool(3))
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Bone);
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D texture = TextureAssets.Projectile[Type].Value;
			Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, 1.6f, Projectile.velocity.X > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
			return false;
		}
	}

	// 1 in 4 Skeletron summons become the Bonemass.
	public class BonemassSwap : GlobalNPC
	{
		public override void OnSpawn(NPC npc, IEntitySource source) {
			if (npc.type != NPCID.SkeletronHead || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (NPC.AnyNPCs(ModContent.NPCType<BonemassOfSkeletron>()) || !Main.rand.NextBool(4))
				return;
			int index = NPC.NewNPC(npc.GetSource_FromThis(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BonemassOfSkeletron>());
			if (index >= 0 && index < Main.maxNPCs) {
				npc.active = false;
				if (Main.netMode == NetmodeID.Server) {
					NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
					NetMessage.SendData(MessageID.SyncNPC, number: index);
				}
				string text = Language.GetTextValue("Mods.BagofChaos.Events.BonemassRises");
				if (Main.netMode == NetmodeID.Server)
					Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), new Color(175, 75, 255));
				else
					Main.NewText(text, new Color(175, 75, 255));
			}
		}
	}
}
