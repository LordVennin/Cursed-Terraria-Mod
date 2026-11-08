using System;
using System.Collections.Generic;
using BagofChaos.Common.GlobalItems;
using BagofChaos.Content.Items.Accessories;
using BagofChaos.Common.GlobalProjectiles;
using BagofChaos.Common.Systems;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common.Players
{
	// The third part of ChaosPlayer: things added by the testers' round 4 change list.
	public partial class ChaosPlayer
	{
		// ---------------- Rubber Banding Bandit Warpaint: projectile magnet ----------------
		public const int WarpaintMagnetDuration = 60 * 5;
		public const int WarpaintCooldownTime = 60 * 45;
		public const float WarpaintRadius = 6f * 16f;
		public bool WarpaintMagnetActive => Player.HasBuff<WarpaintMagnetBuff>();

		public override void ProcessTriggers(TriggersSet triggersSet) {
			if (ChaosKeybinds.WarpaintMagnet != null && ChaosKeybinds.WarpaintMagnet.JustPressed && BanditWarpaint && BanditSetBonus
				&& !Player.HasBuff<WarpaintCooldown>() && !Player.dead) {
				Player.AddBuff(ModContent.BuffType<WarpaintMagnetBuff>(), WarpaintMagnetDuration);
				Player.AddBuff(ModContent.BuffType<WarpaintCooldown>(), WarpaintCooldownTime);
				SoundEngine.PlaySound(SoundID.Item122, Player.Center);
			}
		}

		private bool InWarpaintField(Projectile projectile) {
			return Vector2.Distance(projectile.Center, Player.Center) <= WarpaintRadius + Math.Max(projectile.width, projectile.height) * 0.5f;
		}

		private bool WarpaintCanCapture(Projectile p) {
			if (!p.active || p.damage <= 0 && !p.hostile)
				return false;
			var global = p.GetGlobalProjectile<ChaosGlobalProjectile>();
			if (global.Reflected && p.owner == Player.whoAmI)
				return false;
			if (Main.projPet[p.type] || p.minion || p.sentry || p.aiStyle == ProjAIStyleID.Hook)
				return false;
			if (p.owner == Player.whoAmI) {
				// Never grab what you're currently holding or swinging.
				if (Player.heldProj == p.whoAmI || p.ownerHitCheck || ProjectileID.Sets.IsAWhip[p.type])
					return false;
				int ai = p.aiStyle;
				if (ai == ProjAIStyleID.Yoyo || ai == ProjAIStyleID.Flail || ai == ProjAIStyleID.Spear || ai == ProjAIStyleID.Drill
					|| ai == ProjAIStyleID.HeldProjectile || ai == ProjAIStyleID.ShortSword)
					return false;
			}
			return true;
		}

		// Pull every projectile in six tiles toward you; whatever reaches you becomes yours and is fired straight back out.
		private void UpdateWarpaintMagnet() {
			if (!WarpaintMagnetActive)
				return;
			if (Main.netMode != NetmodeID.Server) {
				for (int i = 0; i < 3; i++) {
					Vector2 ring = Player.Center + Main.rand.NextVector2CircularEdge(WarpaintRadius, WarpaintRadius);
					Dust dust = Dust.NewDustPerfect(ring, DustID.PinkTorch, (Player.Center - ring) * 0.04f, 0, default, 1.1f);
					dust.noGravity = true;
				}
			}
			if (Player.whoAmI != Main.myPlayer)
				return;
			foreach (Projectile p in Main.ActiveProjectiles) {
				if (!InWarpaintField(p) || !WarpaintCanCapture(p))
					continue;
				Vector2 toPlayer = Player.Center - p.Center;
				float speed = Math.Max(p.velocity.Length(), 10f);
				if (toPlayer.Length() > 40f) {
					p.velocity = Vector2.Lerp(p.velocity, toPlayer.SafeNormalize(Vector2.Zero) * speed, 0.35f); // attract
					continue;
				}
				CaptureProjectile(p, speed);
			}
		}

		private void CaptureProjectile(Projectile p, float speed) {
			// Reflect: straight back out, away from you, along the line it came in on.
			Vector2 outward = (p.Center - Player.Center).SafeNormalize(-p.velocity.SafeNormalize(Vector2.UnitX * Player.direction));
			if (outward == Vector2.Zero)
				outward = Vector2.UnitX * Player.direction;
			Vector2 velocity = outward * speed;
			if (p.owner == Player.whoAmI) {
				var global = p.GetGlobalProjectile<ChaosGlobalProjectile>();
				global.Reflected = true;
				p.friendly = true;
				p.hostile = false;
				p.velocity = velocity;
				p.netUpdate = true;
			}
			else {
				int damage = Math.Max(p.damage, 10);
				int index = Projectile.NewProjectile(Player.GetSource_Misc("WarpaintMagnet"), p.Center, velocity, p.type, damage, Math.Max(p.knockBack, 4f), Player.whoAmI, p.ai[0], p.ai[1], p.ai[2]);
				if (index >= 0 && index < Main.maxProjectiles) {
					Projectile copy = Main.projectile[index];
					copy.GetGlobalProjectile<ChaosGlobalProjectile>().Reflected = true;
					copy.friendly = true;
					copy.hostile = false;
					copy.DamageType = DamageClass.Melee;
					copy.netUpdate = true;
				}
				BagofChaos.SendKillProjectile(p);
			}
			if (Main.netMode != NetmodeID.Server) {
				SoundEngine.PlaySound(SoundID.Item150 with { Volume = 0.6f }, p.Center);
				for (int i = 0; i < 6; i++)
					Dust.NewDustPerfect(p.Center, DustID.PinkTorch, velocity * 0.2f + Main.rand.NextVector2Circular(2f, 2f)).noGravity = true;
			}
		}
	
		// ---------------- Triforces ----------------
		// Individual pieces, in the order they are equipped. Each vanilla class is converted by the first piece that wants it,
		// and never again (Power + Wisdom swaps melee and magic instead of looping).
		public readonly List<DamageConversion> TriforceSources = new();

		public static DamageClass ClassOf(DamageConversion c) => c switch {
			DamageConversion.Melee => DamageClass.Melee,
			DamageConversion.Ranged => DamageClass.Ranged,
			DamageConversion.Magic => DamageClass.Magic,
			DamageConversion.Summon => DamageClass.Summon,
			_ => null,
		};

		/// <summary>The vanilla family of a damage class, or None for anything the Triforces leave alone (generic, throwing, modded).</summary>
		public static DamageConversion ConversionOf(DamageClass c) {
			if (c == DamageClass.Melee || c == DamageClass.MeleeNoSpeed) return DamageConversion.Melee;
			if (c == DamageClass.Ranged) return DamageConversion.Ranged;
			if (c == DamageClass.Magic) return DamageConversion.Magic;
			if (c == DamageClass.Summon || c == DamageClass.SummonMeleeSpeed) return DamageConversion.Summon;
			return DamageConversion.None;
		}

		public DamageClass EffectiveDamageClass(DamageClass original) {
			DamageConversion family = ConversionOf(original);
			if (family == DamageConversion.None)
				return original;
			if (CompleteTriforce)
				return family == HyruleMode ? original : ClassOf(HyruleMode);
			if (family == DamageConversion.Summon)
				return original; // summon damage is untouched by the individual pieces
			foreach (DamageConversion source in TriforceSources) {
				if (family != source)
					return ClassOf(source);
			}
			return original;
		}

		private void ApplyTriforceConversions() {
			void Apply(Item item) {
				if (item != null && !item.IsAir && item.TryGetGlobalItem(out TriforceGlobalItem tri))
					tri.Apply(item, this);
			}
			foreach (Item item in Player.inventory)
				Apply(item);
			if (Player.whoAmI == Main.myPlayer) {
				Apply(Main.mouseItem);
				// Put back anything sitting in an open chest or bank so it never stays converted there.
				Item[] open = Player.chest >= 0 ? Main.chest[Player.chest].item : Player.chest == -2 ? Player.bank.item : Player.chest == -3 ? Player.bank2.item
					: Player.chest == -4 ? Player.bank3.item : Player.chest == -5 ? Player.bank4.item : null;
				if (open != null) {
					foreach (Item item in open) {
						if (item != null && !item.IsAir && item.TryGetGlobalItem(out TriforceGlobalItem tri) && tri.ConvertedFrom != null) {
							item.DamageType = tri.ConvertedFrom;
							tri.ConvertedFrom = null;
						}
					}
				}
			}
		}
	
		// ---------------- NPC Directory build tree unlocks ----------------
		public bool HasReforged;       // Goblin' Your Money is sold after your first reforge
		public bool HasUsedHairWindow; // the Snipping Tool is sold after your first haircut
		private bool hairWindowWasOpen;
		private int hairBefore;
		private Color hairColorBefore;

		private void TrackHairWindow() {
			if (Main.hairWindow && !hairWindowWasOpen) {
				hairBefore = Player.hair;
				hairColorBefore = Player.hairColor;
			}
			// Cancelling puts the old hair back; paying keeps the new one.
			if (!Main.hairWindow && hairWindowWasOpen && (Player.hair != hairBefore || Player.hairColor != hairColorBefore))
				HasUsedHairWindow = true;
			hairWindowWasOpen = Main.hairWindow;
		}

		// Angler's Pocketguide: guaranteed on your 5th finished quest, and again every 5 quests so it can always be replaced.
		public override void AnglerQuestReward(float rareMultiplier, List<Item> rewardItems) {
			if (Player.anglerQuestsFinished > 0 && Player.anglerQuestsFinished % 5 == 0)
				rewardItems.Add(new Item(ModContent.ItemType<Content.Items.Tools.AnglersPocketguide>()));
		}
	
		// ---------------- "Harpy" Wings ----------------
		public bool HarpyWings, HarpyWingsVisible;

		private void ResetRound4() {
			HarpyWings = HarpyWingsVisible = false;
			OceanFishbowl = false;
			ResetRound4More();
		}

		partial void ResetRound4More();

		// Holding jump while falling glides, at a gentle (if undignified) pace.
		private void UpdateHarpyGlide() {
			if (!HarpyWings || Player.mount.Active || Player.wet)
				return;
			if (Player.controlJump && Player.velocity.Y > 0f && Player.gravDir == 1f) {
				Player.velocity.Y = System.Math.Min(Player.velocity.Y, 1.6f);
				Player.fallStart = (int)(Player.position.Y / 16f);
			}
		}
	
		// ---------------- Chaotic weapon boxes ----------------
		public const int BoxSwapTime = 30; // half a second
		private Content.Items.Weapons.ChaoticWeaponBox boxInUse;
		private int boxSwapTimer;

		public void StartBoxSession(Content.Items.Weapons.ChaoticWeaponBox box, int slot) {
			boxInUse = box;
			BoxReturnSlot = Player.selectedItem;
			BoxBonusActive = true;
			boxSwapTimer = BoxSwapTime;
			Player.selectedItem = slot;
		}

		private bool BoxClassMatches(Item item) => boxInUse != null && boxInUse.Matches(item);

		private void EndBoxSession() {
			if (BoxReturnSlot >= 0 && BoxReturnSlot < 10)
				Player.selectedItem = BoxReturnSlot;
			BoxReturnSlot = -1;
			BoxBonusActive = false;
			boxInUse = null;
		}

		// Runs on its own clock and only swaps between attacks, so the weapons' own timing is never cut short.
		private void UpdateBoxSession() {
			if (!BoxBonusActive)
				return;
			bool boxStillThere = BoxReturnSlot >= 0 && BoxReturnSlot < 10 && Player.inventory[BoxReturnSlot].ModItem is Content.Items.Weapons.ChaoticWeaponBox;
			if (!boxStillThere || Player.dead || (!Player.controlUseItem && Player.itemAnimation == 0)) {
				EndBoxSession();
				return;
			}
			if (--boxSwapTimer > 0 || Player.itemAnimation > 0)
				return;
			var candidates = Content.Items.Weapons.ChaoticWeaponBox.Candidates(Player, boxInUse);
			if (candidates.Count == 0) {
				EndBoxSession();
				return;
			}
			Player.selectedItem = candidates[Main.rand.Next(candidates.Count)];
			boxSwapTimer = BoxSwapTime;
		}
	
		// The extra row used to have ten slots; the tenth now sits under the trash can, so its item is given back.
		public override void OnEnterWorld() {
			foreach (Item item in extraRowOverflow)
				Player.QuickSpawnItem(Player.GetSource_Misc("RatPackageCo"), item, item.stack);
			extraRowOverflow.Clear();
		}
	
		// ---------------- Ocean in a Fishbowl ----------------
		// Air counts as water: vanilla picks water movement right after PreUpdateMovement, so we set wet there. wetCount
		// is held up so leaving "water" every frame doesn't splash every frame. Breathing is handled after vanilla.
		public bool OceanFishbowl;
		private int breathBeforeUpdate;
		private int fishbowlBreathTimer;

		public override void PreUpdate() {
			breathBeforeUpdate = Player.breath;
		}

		private void UpdateFishbowlWater() {
			if (!OceanFishbowl || Player.lavaWet || Player.honeyWet || Player.shimmerWet)
				return;
			Player.wet = true;
			Player.wetCount = 10;
		}

		// Vanilla refilled your breath because you're "in air"; take that back and drown you at vanilla's pace instead.
		private void UpdateFishbowlBreath() {
			if (!OceanFishbowl || Player.dead || Player.whoAmI != Main.myPlayer || Player.gills || Player.accMerman)
				return;
			if (Collision.DrownCollision(Player.position, Player.width, Player.height, Player.gravDir))
				return; // actually underwater: vanilla is already drowning you
			Player.breath = breathBeforeUpdate;
			int pace = 7 * (Player.accDivingHelm ? 4 : 1);
			if (++fishbowlBreathTimer < pace)
				return;
			fishbowlBreathTimer = 0;
			Player.breath--;
			if (Player.breath == 0)
				SoundEngine.PlaySound(SoundID.Drown, Player.Center);
			if (Player.breath <= 0) {
				Player.breath = 0;
				Player.lifeRegenTime = 0;
				Player.statLife -= 2;
				if (Player.statLife <= 0) {
					Player.statLife = 0;
					Player.KillMe(Terraria.DataStructures.PlayerDeathReason.ByOther(1), 10.0, 0);
				}
			}
		}
	}
}
