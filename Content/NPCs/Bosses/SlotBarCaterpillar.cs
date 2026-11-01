using System;
using BagofChaos.Common.Players;
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
using Terraria.ModLoader;

namespace BagofChaos.Content.NPCs.Bosses
{
	// Steals the closest player's bottom inventory row and wears it as a body. Wiggles like a Pokey.
	// Attacks: wiggle-charge, boomerang segments, segment rain, spin-dash. 10,000 HP.
	[AutoloadBossHead]
	public class SlotBarCaterpillar : ModNPC
	{
		public const int Segments = 10;
		private ref float Timer => ref NPC.ai[0];
		private ref float AttackIndex => ref NPC.ai[1];
		private ref float SpinAngle => ref NPC.ai[2];
		public int Victim { get => (int)NPC.ai[3]; set => NPC.ai[3] = value; }

		public override string Texture => "BagofChaos/Content/Projectiles/PongBall";

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 1;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.ImmuneToRegularBuffs[Type] = true;
		}

		public override void SetDefaults() {
			NPC.width = 44;
			NPC.height = 44 * Segments;
			NPC.damage = 50;
			NPC.defense = 20;
			NPC.lifeMax = 10000;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(gold: 15);
			NPC.knockBackResist = 0f;
			NPC.boss = true;
			NPC.npcSlots = 10f;
			NPC.aiStyle = -1;
			if (!Main.dedServ)
				Music = MusicID.Boss2;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) {
			NPC.lifeMax = (int)(NPC.lifeMax * balance * bossAdjustment);
		}

		public override void OnSpawn(IEntitySource source) {
			NPC.TargetClosest(false);
			Victim = NPC.target;
			NPC.netUpdate = true;
		}

		public override void AI() {
			// Steal the victim's bottom row (only that player's own client touches their inventory).
			if (Victim == Main.myPlayer && !Main.player[Victim].dead) {
				ChaosPlayer chaos = Main.LocalPlayer.GetModPlayer<ChaosPlayer>();
				if (!chaos.RowStolen) {
					chaos.RowStolen = true;
					for (int i = 0; i < Segments; i++) {
						chaos.StolenRow[i] = Main.LocalPlayer.inventory[40 + i].Clone();
						Main.LocalPlayer.inventory[40 + i].TurnToAir();
					}
					Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.NPCs.SlotBarCaterpillar.Stolen"), Color.Orange);
				}
			}

			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (target.dead || !target.active) {
				NPC.velocity.Y += 0.3f;
				if (NPC.timeLeft > 60) NPC.timeLeft = 60;
				return;
			}

			Timer++;
			int attack = (int)AttackIndex % 4;
			float wiggle = (float)Math.Sin(Main.GameUpdateCount * 0.1f);
			switch (attack) {
				case 0: // wiggle toward the player
					NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, Math.Sign(target.Center.X - NPC.Center.X) * 4f + wiggle * 2f, 0.05f);
					NPC.velocity.Y += 0.3f;
					if (NPC.collideX && NPC.velocity.Y == 0f) NPC.velocity.Y = -9f;
					if (Timer >= 240) NextAttack();
					break;
				case 1: // launch segments as boomerangs
					NPC.velocity.X *= 0.9f;
					NPC.velocity.Y += 0.3f;
					if (Timer % 40 == 0 && Timer <= 120 && Main.netMode != NetmodeID.MultiplayerClient) {
						int segment = (int)(Timer / 40) * 3 % Segments;
						Vector2 from = NPC.Bottom - new Vector2(0f, 44f * segment + 22f);
						Vector2 velocity = (target.Center - from).SafeNormalize(Vector2.UnitX) * 11f;
						Projectile.NewProjectile(NPC.GetSource_FromAI(), from, velocity, ModContent.ProjectileType<SlotBoomerang>(), 35, 6f, Main.myPlayer, NPC.whoAmI, segment);
						SoundEngine.PlaySound(SoundID.Item1, from);
					}
					if (Timer >= 200) NextAttack();
					break;
				case 2: // segment rain
					NPC.velocity.X *= 0.9f;
					NPC.velocity.Y += 0.3f;
					if (Timer % 15 == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
						Vector2 spawn = new Vector2(target.Center.X + Main.rand.NextFloat(-400f, 400f), target.Center.Y - 600f);
						Projectile.NewProjectile(NPC.GetSource_FromAI(), spawn, new Vector2(0f, 9f), ModContent.ProjectileType<SlotBoomerang>(), 30, 4f, Main.myPlayer, NPC.whoAmI, Main.rand.Next(Segments), 1f);
					}
					if (Timer >= 180) NextAttack();
					break;
				case 3: // spin dash
					SpinAngle += 0.15f;
					if (Timer == 1) {
						NPC.velocity = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX) * 14f;
						SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
					}
					if (NPC.collideX) NPC.velocity.X *= -1f;
					if (NPC.collideY) NPC.velocity.Y *= -1f;
					if (Timer >= 120) { SpinAngle = 0f; NPC.velocity *= 0.3f; NextAttack(); }
					break;
			}
		}

		private void NextAttack() {
			Timer = 0;
			AttackIndex++;
			NPC.netUpdate = true;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D back = TextureAssets.InventoryBack.Value;
			Item[] row = Victim >= 0 && Victim < Main.maxPlayers ? Main.player[Victim].GetModPlayer<ChaosPlayer>().StolenRow : null;
			float wiggle = (float)Math.Sin(Main.GameUpdateCount * 0.1f);
			for (int i = 0; i < Segments; i++) {
				float offsetX = (float)Math.Sin(Main.GameUpdateCount * 0.1f + i * 0.5f) * 10f * (i / (float)Segments) + wiggle * i;
				Vector2 center = NPC.Bottom - screenPos + new Vector2(offsetX, -22f - i * 44f);
				if (SpinAngle != 0f)
					center = NPC.Center - screenPos + (center - (NPC.Center - screenPos)).RotatedBy(SpinAngle);
				spriteBatch.Draw(back, center, null, drawColor, SpinAngle, back.Size() * 0.5f, 0.85f, SpriteEffects.None, 0f);
				if (row != null && i < row.Length && !row[i].IsAir) {
					Main.instance.LoadItem(row[i].type);
					Texture2D icon = TextureAssets.Item[row[i].type].Value;
					float scale = Math.Min(1f, 28f / Math.Max(icon.Width, icon.Height));
					spriteBatch.Draw(icon, center, null, drawColor, SpinAngle, icon.Size() * 0.5f, scale, SpriteEffects.None, 0f);
				}
			}
			// Googly eyes on the top segment, obviously.
			Texture2D eye = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/GooglyEye").Value;
			Vector2 top = NPC.Bottom - screenPos + new Vector2(wiggle * (Segments - 1), -22f - (Segments - 1) * 44f);
			spriteBatch.Draw(eye, top + new Vector2(-9f, -4f), null, Color.White, 0f, eye.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			spriteBatch.Draw(eye, top + new Vector2(9f, -4f), null, Color.White, 0f, eye.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
			return false;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.Consumables.CaterpillarBag>()));
			LeadingConditionRule notExpert = new LeadingConditionRule(new Conditions.NotExpert());
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.SlotterPillar>()));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Items.Ammo.SlottingPillarAmmo>()));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Items.Consumables.RatPackageCo>()));
			npcLoot.Add(notExpert);
			npcLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 10, 30));
		}

		public override void OnKill() {
			ChaosEvents.SetDowned(ref ChaosEvents.DownedCaterpillar);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.SlotBarCaterpillar"));
		}
	}

	// A stolen inventory slot thrown like a boomerang (ai[2] = 1: just falls).
	public class SlotBoomerang : ModProjectile
	{
		public override string Texture => "BagofChaos/Content/Projectiles/PongBall";

		public override void SetDefaults() {
			Projectile.width = 36;
			Projectile.height = 36;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 180;
		}

		public override void AI() {
			Projectile.rotation += 0.15f;
			if (Projectile.ai[2] == 1f)
				return; // falling segment
			if (Projectile.timeLeft < 120) {
				int owner = (int)Projectile.ai[0];
				if (owner >= 0 && owner < Main.maxNPCs && Main.npc[owner].active) {
					Vector2 to = Main.npc[owner].Center - Projectile.Center;
					if (to.Length() < 40f) { Projectile.Kill(); return; }
					Projectile.velocity = (Projectile.velocity * 10f + to.SafeNormalize(Vector2.Zero) * 12f) / 11f;
				}
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D back = TextureAssets.InventoryBack.Value;
			Main.EntitySpriteDraw(back, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, back.Size() * 0.5f, 0.85f, SpriteEffects.None, 0);
			int owner = (int)Projectile.ai[0];
			int segment = (int)Projectile.ai[1];
			if (owner >= 0 && owner < Main.maxNPCs && Main.npc[owner].ModNPC is SlotBarCaterpillar boss && boss.Victim >= 0 && boss.Victim < Main.maxPlayers) {
				Item[] row = Main.player[boss.Victim].GetModPlayer<ChaosPlayer>().StolenRow;
				if (segment >= 0 && segment < row.Length && !row[segment].IsAir) {
					Main.instance.LoadItem(row[segment].type);
					Texture2D icon = TextureAssets.Item[row[segment].type].Value;
					float scale = Math.Min(1f, 28f / Math.Max(icon.Width, icon.Height));
					Main.EntitySpriteDraw(icon, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, icon.Size() * 0.5f, scale, SpriteEffects.None, 0);
				}
			}
			return false;
		}
	}
}
