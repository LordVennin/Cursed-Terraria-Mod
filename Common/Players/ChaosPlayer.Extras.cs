using System;
using System.Collections.Generic;
using BagofChaos.Content.Buffs;
using BagofChaos.Content.Items.Accessories;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BagofChaos.Common.Players
{
	// The second half of ChaosPlayer: everything added by the big "new items" update.
	public partial class ChaosPlayer
	{
		// ---------------- per-frame flags ----------------
		public bool CompleteTriforce;
		public bool Stalemate;
		public bool LootScanner;
		public bool TouchGrass;
		public bool FrogShoes;
		public bool NTerraria;
		public bool UnstableConcoction;
		public bool SpiderWings;
		public bool BanditMask, BanditBandana, BanditGoggles, BanditWarpaint, BanditSuit, BanditShoes, BanditSetBonus;
		public bool TanningMirrorHeld;
		public bool ChaoticArrows, ChaoticBullets, AmmoRoulette, ChaoticDash, ChaoticBanker;
		public float WeaponAttackSpeed;

		// ---------------- saved ----------------
		public DamageConversion HyruleMode = DamageConversion.Melee;
		public bool ImmuneToCobwebs;
		public bool ConfettiOnHurt;
		public bool ExtraRowUnlocked;
		public Item[] ExtraRow = new Item[ExtraRowSlots];
		public const int ExtraRowSlots = 9; // nine slots beside the trash can
		private readonly List<Item> extraRowOverflow = new(); // items from the old ten-slot row, handed back on spawn
		public bool RatTacksSummon;
		public float SkewerBonusLength;
		public List<int> TrashedTypes = new();           // last 20 distinct trashed item types (Rat Merchant stock)
		public Item[] StolenRow = new Item[10];           // the Slot-bar Caterpillar's hostages
		public bool RowStolen;
		private int lastTrashType;

		// ---------------- transient state ----------------
		public int BoxReturnSlot = -1;
		public bool BoxBonusActive;
		public int FartCharge;
		private int warpaintBurst;
		private float frogLockedY = float.NaN;
		private bool wasPotionSick;
		private bool unstableDamageRoll;
		private int chosenAmmoType;
		private int tanTagTimer;
		private readonly int[] tetherCooldown = new int[Main.maxPlayers];
		private int chaoticDashTimer;
		private int chaoticDashChain;
		private Vector2 chaoticDashDir;

		public override void Initialize() {
			for (int i = 0; i < ExtraRow.Length; i++)
				ExtraRow[i] = new Item();
			for (int i = 0; i < StolenRow.Length; i++)
				StolenRow[i] = new Item();
		}

		public override void SaveData(TagCompound tag) {
			tag["hyruleMode"] = (int)HyruleMode;
			tag["cobwebs"] = ImmuneToCobwebs;
			tag["confetti"] = ConfettiOnHurt;
			tag["extraRow"] = ExtraRowUnlocked;
			tag["extraRowItems"] = ExtraRow;
			tag["ratTacksSummon"] = RatTacksSummon;
			tag["skewer"] = SkewerBonusLength;
			tag["trashed"] = TrashedTypes;
			tag["rowStolen"] = RowStolen;
			tag["stolenRow"] = StolenRow;
			tag["hasReforged"] = HasReforged;
			tag["hasUsedHair"] = HasUsedHairWindow;
		}

		public override void LoadData(TagCompound tag) {
			HyruleMode = (DamageConversion)tag.GetInt("hyruleMode");
			if (HyruleMode == DamageConversion.None) HyruleMode = DamageConversion.Melee;
			ImmuneToCobwebs = tag.GetBool("cobwebs");
			ConfettiOnHurt = tag.GetBool("confetti");
			ExtraRowUnlocked = tag.GetBool("extraRow");
			RatTacksSummon = tag.GetBool("ratTacksSummon");
			SkewerBonusLength = tag.GetFloat("skewer");
			var items = tag.Get<Item[]>("extraRowItems");
			if (items != null) {
				for (int i = 0; i < ExtraRow.Length; i++)
					ExtraRow[i] = i < items.Length && items[i] != null ? items[i] : new Item();
				for (int i = ExtraRow.Length; i < items.Length; i++) {
					if (items[i] != null && !items[i].IsAir)
						extraRowOverflow.Add(items[i]);
				}
			}
			TrashedTypes = new List<int>(tag.GetList<int>("trashed"));
			RowStolen = tag.GetBool("rowStolen");
			HasReforged = tag.GetBool("hasReforged");
			HasUsedHairWindow = tag.GetBool("hasUsedHair");
			var stolen = tag.Get<Item[]>("stolenRow");
			if (stolen != null) {
				for (int i = 0; i < StolenRow.Length; i++)
					StolenRow[i] = i < stolen.Length && stolen[i] != null ? stolen[i] : new Item();
			}
		}

		// Rat Merchant: remember what you threw away (last 20 distinct types).
		private void TrackTrash() {
			int type = Player.trashItem?.type ?? 0;
			if (type == lastTrashType)
				return;
			lastTrashType = type;
			if (type <= 0)
				return;
			TrashedTypes.Remove(type);
			TrashedTypes.Add(type);
			while (TrashedTypes.Count > 20)
				TrashedTypes.RemoveAt(0);
		}

		// Slot-bar Caterpillar: give the bottom row back once the boss is gone.
		private void UpdateStolenRow() {
			if (!RowStolen || NPC.AnyNPCs(ModContent.NPCType<Content.NPCs.Bosses.SlotBarCaterpillar>()))
				return;
			RowStolen = false;
			for (int i = 0; i < 10; i++) {
				if (StolenRow[i].IsAir)
					continue;
				if (Player.inventory[40 + i].IsAir)
					Player.inventory[40 + i] = StolenRow[i].Clone();
				else
					Player.QuickSpawnItem(Player.GetSource_Misc("Caterpillar"), StolenRow[i].Clone(), StolenRow[i].stack);
				StolenRow[i].TurnToAir();
			}
			Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.NPCs.SlotBarCaterpillar.Returned"), Color.LightGreen);
		}

		private void ResetExtras() {
			ResetRound4();
			TriforceSources.Clear();
			CompleteTriforce = Stalemate = LootScanner = TouchGrass = FrogShoes = NTerraria = UnstableConcoction = SpiderWings = false;
			BanditMask = BanditBandana = BanditGoggles = BanditWarpaint = BanditSuit = BanditShoes = BanditSetBonus = false;
			TanningMirrorHeld = ChaoticArrows = ChaoticBullets = AmmoRoulette = ChaoticDash = ChaoticBanker = false;
			WeaponAttackSpeed = 0f;
		}

		// ------------------------------------------------------------------------------------------------------------
		private void PostUpdateEquipsExtras() {
			// Chaotic Banker: passive tools keep working from the piggy bank and safe.
			if (ChaoticBanker)
				ApplyBankPassives();

			// Triforces: give every weapon in the inventory its effective damage class for this frame.
			ApplyTriforceConversions();

			// Weapon Attack Speed: whips and melee through vanilla attack speed; the rest through use time (ChaosGlobalItem).
			if (Player.HasBuff<UnstablePower>() && !unstableDamageRoll)
				WeaponAttackSpeed += 0.5f;
			if (warpaintBurst > 0) {
				warpaintBurst--;
				Player.moveSpeed += 1.5f;
				Player.runAcceleration *= 3f;
				Player.maxRunSpeed *= 2f;
				Player.accRunSpeed *= 2f;
			}
			if (WeaponAttackSpeed != 0f) {
				Player.GetAttackSpeed(DamageClass.Melee) += WeaponAttackSpeed;
				Player.GetAttackSpeed(DamageClass.SummonMeleeSpeed) += WeaponAttackSpeed;
				Player.manaCost -= WeaponAttackSpeed * 0.2f;
			}
			if (Player.HasBuff<UnstablePower>() && unstableDamageRoll)
				Player.GetDamage(DamageClass.Generic) += 0.5f;
		}

		private void ApplyBankPassives() {
			foreach (Item[] bank in new[] { Player.bank.item, Player.bank2.item }) {
				foreach (Item item in bank) {
					if (item == null || item.IsAir)
						continue;
					switch (item.type) {
						case ItemID.DontHurtCrittersBook: Player.dontHurtCritters = true; break;
						case ItemID.CordageGuide: Player.cordage = true; break;
						case ItemID.DontHurtNatureBook: Player.dontHurtNature = true; break;
						case ItemID.DontHurtComboBook: Player.dontHurtCritters = true; Player.dontHurtNature = true; break;
						case ItemID.GoldWatch: case ItemID.PlatinumWatch: Player.accWatch = 3; break;
						case ItemID.Compass: Player.accCompass = 1; break;
						case ItemID.DepthMeter: Player.accDepthMeter = 1; break;
						case ItemID.CellPhone: Player.accWatch = 3; Player.accCompass = 1; Player.accDepthMeter = 1; Player.accOreFinder = true; Player.accFishFinder = true; Player.accWeatherRadio = true; Player.accCalendar = true; Player.accThirdEye = true; Player.accJarOfSouls = true; Player.accCritterGuide = true; Player.accStopwatch = true; Player.accDreamCatcher = true; break;
					}
					if (item.ModItem is Content.Items.Tools.ChaoticAmmoTool ammoTool) {
						if (ammoTool.Arrows) ChaoticArrows = true;
						if (ammoTool.Bullets) ChaoticBullets = true;
						if (ammoTool is Content.Items.Tools.ChaoticAmmunitionRoulette) AmmoRoulette = true;
					}
					if (item.ModItem is Content.Items.Tools.ChaoticDashBox) ChaoticDash = true;
					if (item.ModItem is Content.Items.Tools.GentlemenStack) {
						foreach (Item inv in Player.inventory)
							if (!inv.IsAir && inv.maxStack < 99) inv.maxStack = 99;
					}
				}
			}
		}

		// ------------------------------------------------------------------------------------------------------------
		private bool PreUpdateMovementExtras() {
			// Hollow Blocks: solid for everything except players. Vanilla moves the player right after this hook.
			Main.tileSolid[ModContent.TileType<Content.Tiles.HollowBlockTile>()] = false;

			// Cobweb immunity: webs touching you simply stop existing.
			if (ImmuneToCobwebs && Player.whoAmI == Main.myPlayer) {
				Rectangle box = Player.Hitbox;
				box.Inflate(4, 4);
				for (int x = box.Left / 16; x <= box.Right / 16; x++) {
					for (int y = box.Top / 16; y <= box.Bottom / 16; y++) {
						if (WorldGen.InWorld(x, y, 5) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == TileID.Cobweb) {
							WorldGen.KillTile(x, y, fail: false, effectOnly: false, noItem: true);
							if (Main.netMode == NetmodeID.MultiplayerClient)
								NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: x, number3: y);
						}
					}
				}
			}

			// Balls of Spiders: cling to walls while pushing into them.
			if (SpiderWings && Player.velocity.Y > 0f && ((Player.controlLeft && Collision.SolidCollision(Player.position - new Vector2(2f, 0f), 2, Player.height)) || (Player.controlRight && Collision.SolidCollision(Player.position + new Vector2(Player.width, 0f), 2, Player.height))))
				Player.velocity.Y = 0f;

			// Chaotic Dash Box: double tap -> dash in a random direction, no cooldown, chains twice.
			if (ChaoticDash && !Player.mount.Active) {
				if (DashDir != -1 && (chaoticDashTimer == 0 || chaoticDashChain < 2)) {
					if (chaoticDashTimer == 0) {
						chaoticDashDir = Main.rand.Next(4) switch { 0 => Vector2.UnitX, 1 => -Vector2.UnitX, 2 => -Vector2.UnitY, _ => Vector2.UnitY };
						chaoticDashChain = 0;
					}
					else {
						chaoticDashChain++;
					}
					chaoticDashTimer = 14;
					SoundEngine.PlaySound(SoundID.Item13, Player.Center);
				}
				if (chaoticDashTimer > 0) {
					chaoticDashTimer--;
					Player.velocity = chaoticDashDir * 14f;
					Player.eocDash = chaoticDashTimer;
					Player.armorEffectDrawShadowEOCShield = true;
					Player.dashDelay = 0;
					return true;
				}
			}

			// Slippery Frog Shoes: your Y coordinate is a constant now.
			if (FrogShoes) {
				if (float.IsNaN(frogLockedY))
					frogLockedY = Player.position.Y;
				Player.position.Y = frogLockedY;
				Player.velocity.Y = 0f;
				Player.gravity = 0f;
				Player.fallStart = (int)(Player.position.Y / 16f);
			}
			else {
				frogLockedY = float.NaN;
			}
			return false;
		}

		// ------------------------------------------------------------------------------------------------------------
		private void PostUpdateExtras() {
			UpdateWarpaintMagnet();
			if (Player.whoAmI == Main.myPlayer)
				TrackHairWindow();
			Main.tileSolid[ModContent.TileType<Content.Tiles.HollowBlockTile>()] = true;

			if (FrogShoes)
				frogLockedY = Player.position.Y;

			if (Player.whoAmI == Main.myPlayer)
				UpdateBoxSession();

			// Rubber Banding Bandit Shoes: bounce off tiles and enemies.
			if (BanditShoes && Player.whoAmI == Main.myPlayer && !Player.mount.Active) {
				if (Player.velocity.X == 0f && Player.oldVelocity.X != 0f && Math.Abs(Player.oldVelocity.X) > 1f)
					Player.velocity.X = -Player.oldVelocity.X * 0.9f;
				if (Player.velocity.Y == 0f && Player.oldVelocity.Y > 4f)
					Player.velocity.Y = -Player.oldVelocity.Y * 0.8f;
				foreach (NPC npc in Main.ActiveNPCs) {
					if (ChaosUtils.IsHostile(npc) && npc.Hitbox.Intersects(Player.Hitbox)) {
						Player.velocity = (Player.Center - npc.Center).SafeNormalize(-Vector2.UnitY) * 12f;
						break;
					}
				}
			}

			if (Player.whoAmI != Main.myPlayer || Player.dead)
				return;

			TrackTrash();
			UpdateStolenRow();
			if (TanningMirrorHeld)
				UpdateTanningMirror();
			UpdateUnstableConcoction();
			UpdateTeamTether();
		}

		// Tanning Mirror: a 75 degree cone of light above you. Enemies inside get tagged once a second.
		// Minions inside play pinball (see ChaosGlobalProjectile.TanningPinball).
		public const float TanHalfAngle = 37.5f * (float)Math.PI / 180f;
		public const float TanRange = 420f;
		public Vector2 TanApex => Player.Top;
		public float TanFloorY => Player.Top.Y - 12f; // the bumper floor just above your head

		public bool InTanCone(Vector2 pos) {
			Vector2 d = pos - TanApex;
			if (d.Length() > TanRange || d.Y > 0f) return false;
			float angle = Math.Abs(MathHelper.WrapAngle(d.ToRotation() + MathHelper.PiOver2));
			return angle <= TanHalfAngle;
		}

		private void UpdateTanningMirror() {
			Vector2 apex = TanApex;
			for (int i = 0; i < 6; i++) {
				float angle = -MathHelper.PiOver2 + Main.rand.NextFloat(-TanHalfAngle, TanHalfAngle);
				Vector2 point = apex + angle.ToRotationVector2() * Main.rand.NextFloat(20f, TanRange);
				Lighting.AddLight(point, 1f, 0.9f, 0.5f);
				Dust dust = Dust.NewDustPerfect(point, DustID.GoldFlame, Vector2.Zero, 0, default, 0.8f);
				dust.noGravity = true;
			}
			// The bumper floor.
			for (int i = 0; i < 2; i++) {
				Dust dust = Dust.NewDustPerfect(new Vector2(apex.X + Main.rand.NextFloat(-40f, 40f), TanFloorY), DustID.YellowTorch, Vector2.Zero, 0, default, 0.9f);
				dust.noGravity = true;
			}
			if (++tanTagTimer >= 60) {
				tanTagTimer = 0;
				foreach (NPC npc in Main.ActiveNPCs) {
					if (ChaosUtils.IsHostile(npc) && InTanCone(npc.Center))
						BagofChaos.SendApplyBuffToNPC(npc, ModContent.BuffType<Tanned>(), 90);
				}
			}
		}

		// Unstable Concoction: while potion sick you can't die and get random power. When it ends, you explode.
		private void UpdateUnstableConcoction() {
			bool sick = Player.HasBuff(BuffID.PotionSickness);
			if (!UnstableConcoction) {
				wasPotionSick = sick;
				return;
			}
			if (sick && !wasPotionSick) {
				unstableDamageRoll = Main.rand.NextBool();
				Player.AddBuff(ModContent.BuffType<UnstablePower>(), 2);
				Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.BagofChaos.Items.UnstableConcoction." + (unstableDamageRoll ? "RolledDamage" : "RolledSpeed")), Color.MediumPurple);
			}
			if (sick) {
				Player.AddBuff(ModContent.BuffType<UnstablePower>(), 2);
				if (Player.statLife < 1)
					Player.statLife = 1;
			}
			if (!sick && wasPotionSick) {
				int explosion = Projectile.NewProjectile(Player.GetSource_Accessory(Player.HeldItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<Content.Projectiles.CanExplosion>(), 60, 8f, Player.whoAmI);
				if (explosion >= 0) Main.projectile[explosion].DamageType = DamageClass.Magic;
				Player.KillMe(PlayerDeathReason.ByCustomReason(Terraria.Localization.NetworkText.FromKey("Mods.BagofChaos.DeathMessages.UnstableConcoction", Player.name)), 9999, 0);
			}
			wasPotionSick = sick;
		}

		// Team Tether: automatically grapple a nearby ally under 25% HP, pull them in and heal them for 25%.
		private void UpdateTeamTether() {
			for (int i = 0; i < tetherCooldown.Length; i++)
				if (tetherCooldown[i] > 0) tetherCooldown[i]--;
			if (!HasTeamTether())
				return;
			foreach (Player ally in Main.ActivePlayers) {
				if (ally.whoAmI == Player.whoAmI || ally.dead || tetherCooldown[ally.whoAmI] > 0)
					continue;
				if (ally.statLife > ally.statLifeMax2 / 4 || Vector2.Distance(ally.Center, Player.Center) > 700f)
					continue;
				tetherCooldown[ally.whoAmI] = 60 * 60;
				// Visual hook flying out, then the pull, then the heal (unless they carry a tether themselves).
				Projectile.NewProjectile(Player.GetSource_Misc("TeamTether"), Player.Center, (ally.Center - Player.Center).SafeNormalize(Vector2.Zero) * 20f, ProjectileID.GemHookDiamond, 0, 0f, Player.whoAmI);
				BagofChaos.SendTetherPull(ally.whoAmI, Player.whoAmI);
				if (!HasTeamTether(ally))
					ChaosUtils.HealPlayer(ally, ally.statLifeMax2 / 4);
				SoundEngine.PlaySound(SoundID.Item9, Player.Center);
			}
		}

		private static bool HasTeamTether(Player player) {
			int type = ModContent.ItemType<TeamTether>();
			foreach (Item item in player.inventory)
				if (item.type == type) return true;
			foreach (Item item in player.miscEquips)
				if (item.type == type) return true;
			return false;
		}

		private bool HasTeamTether() => HasTeamTether(Player);

		// ------------------------------------------------------------------------------------------------------------
		// Hooks
		// ------------------------------------------------------------------------------------------------------------
		public override void UpdateLifeRegen() {
			// Touch Grass: +5 HP per second (lifeRegen is in half-HP per second, so +10) while standing on plain,
			// corrupt or crimson grass. Not jungle, not hallowed, not while airborne.
			if (TouchGrass && Player.velocity.Y == 0f && Player.gravDir == 1f) {
				int ty = (int)((Player.Bottom.Y + 2f) / 16f);
				bool onGrass = false;
				foreach (float fx in new[] { Player.position.X + 2f, Player.Center.X, Player.position.X + Player.width - 2f }) {
					int tx = (int)(fx / 16f);
					if (!WorldGen.InWorld(tx, ty, 5) || !Main.tile[tx, ty].HasTile)
						continue;
					int type = Main.tile[tx, ty].TileType;
					if (type == TileID.Grass || type == TileID.CorruptGrass || type == TileID.CrimsonGrass)
						onGrass = true;
				}
				if (onGrass)
					Player.lifeRegen += 10;
			}
			if (Stalemate) {
				Player.lifeRegen = 0;
				Player.lifeRegenTime = 0;
			}
		}

		public override void GetHealLife(Item item, bool quickHeal, ref int healValue) {
			if (Stalemate || (UnstableConcoction && item != null && item.potion))
				healValue = 0;
		}

		public override bool CanBeHitByProjectile(Projectile proj) {
			// Warpaint magnet: anything inside the field is being captured, not hitting you.
			if (WarpaintMagnetActive && InWarpaintField(proj))
				return false;
			if (Stalemate && proj.npcProj)
				return false;
			// Rubber Banding Bandit Suit: 10% chance any projectile bounces off you.
			if (BanditSuit && Main.rand.NextBool(10)) {
				proj.velocity = -proj.velocity;
				if (proj.hostile) {
					proj.hostile = false;
					proj.friendly = true;
					proj.owner = Player.whoAmI;
				}
				SoundEngine.PlaySound(SoundID.Item10, Player.Center);
				return false;
			}
			return true;
		}

		public override void OnHurt(Player.HurtInfo info) {
			if (ConfettiOnHurt && Main.netMode != NetmodeID.Server) {
				for (int i = 0; i < 25; i++) {
					Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, Main.rand.Next(139, 143), 0f, 0f, 0, default, 1.3f);
					dust.velocity *= 3f;
				}
				SoundEngine.PlaySound(SoundID.Item59, Player.Center);
			}
			if (BanditWarpaint && info.DamageSource.SourceProjectileLocalIndex >= 0)
				warpaintBurst = 120;
		}

		public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource) {
			if (UnstableConcoction && Player.HasBuff(BuffID.PotionSickness)) {
				Player.statLife = 1; // can't die while sick. The hangover is another matter.
				return false;
			}
			return true;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (BoxBonusActive && BoxClassMatches(item))
				damage.Base += 10f;
			// Individual Triforce pieces cost 10%; the Complete Triforce is free.
			if (!CompleteTriforce && item.TryGetGlobalItem(out GlobalItems.TriforceGlobalItem tri) && tri.ConvertedFrom != null)
				damage *= 0.9f;
			if ((ChaoticArrows && item.useAmmo == AmmoID.Arrow) || (ChaoticBullets && item.useAmmo == AmmoID.Bullet) || (AmmoRoulette && (item.useAmmo == AmmoID.Arrow || item.useAmmo == AmmoID.Bullet))) {
				int distinct = DistinctAmmoCount(item);
				if (distinct >= 2)
					damage.Flat += distinct;
			}
		}

		// Chaotic ammo: pick a random equipped ammo type each shot and only allow that one.
		public override bool CanUseItem(Item item) {
			if (item.useAmmo == AmmoID.Arrow || item.useAmmo == AmmoID.Bullet) {
				bool roulette = AmmoRoulette;
				bool chaotic = roulette || (ChaoticArrows && item.useAmmo == AmmoID.Arrow) || (ChaoticBullets && item.useAmmo == AmmoID.Bullet);
				chosenAmmoType = 0;
				if (chaotic) {
					var types = new List<int>();
					foreach (Item ammo in Player.inventory) {
						if (ammo.IsAir || ammo.stack <= 0) continue;
						bool ok = roulette ? (ammo.ammo == AmmoID.Arrow || ammo.ammo == AmmoID.Bullet) : ammo.ammo == item.useAmmo;
						if (ok && !types.Contains(ammo.type)) types.Add(ammo.type);
					}
					if (types.Count > 0)
						chosenAmmoType = types[Main.rand.Next(types.Count)];
				}
			}
			return true;
		}

		public bool? ChooseAmmoFilter(Item weapon, Item ammo) {
			if (chosenAmmoType <= 0)
				return null;
			if (AmmoRoulette && (weapon.useAmmo == AmmoID.Arrow || weapon.useAmmo == AmmoID.Bullet))
				return ammo.type == chosenAmmoType;
			if ((ChaoticArrows && weapon.useAmmo == AmmoID.Arrow) || (ChaoticBullets && weapon.useAmmo == AmmoID.Bullet))
				return ammo.type == chosenAmmoType;
			return null;
		}

		private int DistinctAmmoCount(Item weapon) {
			var types = new HashSet<int>();
			foreach (Item ammo in Player.inventory) {
				if (ammo.IsAir) continue;
				bool ok = AmmoRoulette ? (ammo.ammo == AmmoID.Arrow || ammo.ammo == AmmoID.Bullet) : ammo.ammo == weapon.useAmmo;
				if (ok) types.Add(ammo.type);
			}
			return types.Count;
		}

		public override bool CanConsumeAmmo(Item weapon, Item ammo) {
			if (BanditMask && Main.rand.NextBool(10))
				return false;
			return true;
		}

		// Eye on Me!: the camera is the Eye's now.
		public override void ModifyScreenPosition() {
			if (!Player.HasBuff<EyeOnMe>())
				return;
			int eye = NPC.FindFirstNPC(NPCID.EyeofCthulhu);
			if (eye < 0)
				return;
			Main.screenPosition = Main.npc[eye].Center - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
		}
	}
}
