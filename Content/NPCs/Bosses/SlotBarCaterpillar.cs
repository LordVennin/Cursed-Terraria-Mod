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
	// Copies the closest player's hotbar and wears it as a body (only a picture: nothing is taken, used up or duplicated).
	// Wiggles like a Pokey. Attacks: wiggle-charge, boomerang segments, segment rain, spin-dash, and every 15 seconds it
	// picks a weapon off its copied hotbar and uses it on you. 10,000 HP, half the defense it used to have.
	[AutoloadBossHead]
	public class SlotBarCaterpillar : ModNPC
	{
		public const int Segments = 10;
		private ref float Timer => ref NPC.ai[0];
		private ref float AttackIndex => ref NPC.ai[1];
		private ref float SpinAngle => ref NPC.ai[2];
		public int Victim { get => (int)NPC.ai[3]; set => NPC.ai[3] = value; }

		public override string Texture => "BagofChaos/Content/Projectiles/PongBall";
		public override string BossHeadTexture => "BagofChaos/Content/NPCs/Bosses/SlotBarCaterpillar_Head_Boss";

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
			NPC.defense = 10;
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

		/// <summary>The hotbar slot shown on segment i (segment 0 is the tail at the bottom, the head is hotbar slot 1).</summary>
		public static int SlotForSegment(int segment) => Segments - 1 - segment;

		/// <summary>The item the given segment is showing, or null for an empty slot (or no victim).</summary>
		public Item SegmentItem(int segment) {
			if (Victim < 0 || Victim >= Main.maxPlayers || !Main.player[Victim].active)
				return null;
			Item item = Main.player[Victim].inventory[SlotForSegment(segment)];
			return item == null || item.IsAir ? null : item;
		}

		private static bool IsWeapon(Item item) => item != null && item.damage > 0 && !item.accessory && item.ammo == AmmoID.None && item.createTile < 0
			&& item.createWall < 0 && !item.potion && (item.pick == 0 && item.axe == 0 && item.hammer == 0 || item.shoot > ProjectileID.None);

		// Projectiles it can fire as its own: plain vanilla ones that don't need a player holding them and don't break tiles.
		private static bool CanReplicate(Item item) {
			int type = item.shoot;
			if (type <= ProjectileID.None || type >= ProjectileID.Count || item.channel)
				return false;
			Projectile sample = ContentSamples.ProjectilesByType[type];
			if (sample.minion || sample.sentry || Main.projPet[type] || ProjectileID.Sets.IsAWhip[type] || ProjectileID.Sets.Explosive[type] || sample.ownerHitCheck)
				return false;
			int ai = sample.aiStyle;
			// No boomerangs (they fly back to their owner) and no falling sand/blocks (they place tiles).
			return ai != ProjAIStyleID.Hook && ai != ProjAIStyleID.Yoyo && ai != ProjAIStyleID.Flail && ai != ProjAIStyleID.Spear && ai != ProjAIStyleID.Drill
				&& ai != ProjAIStyleID.HeldProjectile && ai != ProjAIStyleID.ShortSword && ai != ProjAIStyleID.Explosive
				&& ai != ProjAIStyleID.Boomerang && ai != ProjAIStyleID.FallingTile;
		}

		private ref float WeaponTimer => ref NPC.localAI[0];
		private int weaponSegment = -1;
		private int weaponShots;

		// Every 15 seconds: pick a random weapon from the copied hotbar and use it (a 3-shot burst). Skip if there are none.
		private void UpdateCopiedWeapon(Player target) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			if (weaponSegment < 0) {
				if (++WeaponTimer < 60 * 15)
					return;
				WeaponTimer = 0;
				var weapons = new System.Collections.Generic.List<int>();
				for (int i = 0; i < Segments; i++) {
					if (IsWeapon(SegmentItem(i)))
						weapons.Add(i);
				}
				if (weapons.Count == 0)
					return;
				weaponSegment = weapons[Main.rand.Next(weapons.Count)];
				weaponShots = 0;
				return;
			}
			if (++WeaponTimer % 8 != 0)
				return;
			Item item = SegmentItem(weaponSegment);
			if (!IsWeapon(item) || weaponShots >= 3) {
				weaponSegment = -1;
				WeaponTimer = 0;
				return;
			}
			weaponShots++;
			Vector2 from = SegmentCenter(weaponSegment);
			int damage = Math.Clamp(item.damage, 20, 80);
			if (CanReplicate(item)) {
				Vector2 velocity = (target.Center - from).SafeNormalize(Vector2.UnitX) * Math.Max(8f, item.shootSpeed);
				// Owned by nobody (255), like a trap's shot, so no player's own logic ever treats it as theirs.
				int index = Projectile.NewProjectile(NPC.GetSource_FromAI(), from, velocity, item.shoot, damage, item.knockBack, Main.maxPlayers);
				if (index >= 0 && index < Main.maxProjectiles) {
					Projectile shot = Main.projectile[index];
					shot.GetGlobalProjectile<Common.GlobalProjectiles.ChaosGlobalProjectile>().BossCopy = true;
					shot.hostile = true;
					shot.friendly = false;
					shot.netUpdate = true;
				}
			}
			else {
				// Fallback (swords, summons, anything that can't be fired on its own): fling that segment at you.
				Vector2 velocity = (target.Center - from).SafeNormalize(Vector2.UnitX) * 12f;
				Projectile.NewProjectile(NPC.GetSource_FromAI(), from, velocity, ModContent.ProjectileType<SlotBoomerang>(), damage, 6f, Main.myPlayer, NPC.whoAmI, weaponSegment);
			}
			if (item.UseSound.HasValue)
				SoundEngine.PlaySound(item.UseSound.Value, from);
		}

		private Vector2 SegmentCenter(int segment) => NPC.Bottom - new Vector2(0f, 44f * segment + 22f);

		public override void AI() {
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			if (target.dead || !target.active) {
				NPC.velocity.Y += 0.3f;
				if (NPC.timeLeft > 60) NPC.timeLeft = 60;
				return;
			}
			if (Victim < 0 || Victim >= Main.maxPlayers || !Main.player[Victim].active || Main.player[Victim].dead) {
				Victim = NPC.target; // copy whoever it's chasing now
				NPC.netUpdate = true;
			}
			UpdateCopiedWeapon(target);

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
			Texture2D blob = TextureAssets.Npc[Type].Value;
			float wiggle = (float)Math.Sin(Main.GameUpdateCount * 0.1f);
			for (int i = 0; i < Segments; i++) {
				float offsetX = (float)Math.Sin(Main.GameUpdateCount * 0.1f + i * 0.5f) * 10f * (i / (float)Segments) + wiggle * i;
				Vector2 center = NPC.Bottom - screenPos + new Vector2(offsetX, -22f - i * 44f);
				if (SpinAngle != 0f)
					center = NPC.Center - screenPos + (center - (NPC.Center - screenPos)).RotatedBy(SpinAngle);
				Item item = SegmentItem(i);
				bool firing = i == weaponSegment;
				spriteBatch.Draw(back, center, null, firing ? Color.Lerp(drawColor, Color.OrangeRed, 0.6f) : drawColor, SpinAngle, back.Size() * 0.5f, 0.85f, SpriteEffects.None, 0f);
				if (item != null) {
					Main.instance.LoadItem(item.type);
					Texture2D icon = TextureAssets.Item[item.type].Value;
					Rectangle frame = Main.itemAnimations[item.type] != null ? Main.itemAnimations[item.type].GetFrame(icon) : icon.Frame();
					float scale = Math.Min(1f, 28f / Math.Max(frame.Width, frame.Height));
					spriteBatch.Draw(icon, center, frame, drawColor, SpinAngle, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
				}
				else {
					// Empty hotbar slot: a plain green caterpillar segment instead.
					spriteBatch.Draw(blob, center, null, drawColor.MultiplyRGB(new Color(120, 200, 90)), SpinAngle, blob.Size() * 0.5f, 1.4f, SpriteEffects.None, 0f);
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

	// A copied hotbar slot thrown like a boomerang (ai[2] = 1: just falls). ai[1] = segment.
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
			if (owner >= 0 && owner < Main.maxNPCs && Main.npc[owner].ModNPC is SlotBarCaterpillar boss) {
				Item item = boss.SegmentItem(segment);
				if (item != null) {
					Main.instance.LoadItem(item.type);
					Texture2D icon = TextureAssets.Item[item.type].Value;
					Rectangle frame = Main.itemAnimations[item.type] != null ? Main.itemAnimations[item.type].GetFrame(icon) : icon.Frame();
					float scale = Math.Min(1f, 28f / Math.Max(frame.Width, frame.Height));
					Main.EntitySpriteDraw(icon, Projectile.Center - Main.screenPosition, frame, lightColor, Projectile.rotation, frame.Size() * 0.5f, scale, SpriteEffects.None, 0);
				}
			}
			return false;
		}
	}
}
