using System;
using System.Collections.Generic;
using BagofChaos.Common.Systems;
using BagofChaos.Content.Buffs;
using BagofChaos.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common.Players
{
	public class ChaosPlayer : ModPlayer
	{
		// ---------------- accessory / armor flags (reset every frame) ----------------
		public bool InstantUpDown;
		public bool InstantLeftRight;
		public bool UnreasonableUpDown;
		public bool UnreasonableLeftRight;
		public bool OmniMovement;
		public bool SkyInABottle;
		public bool ThreeDGlasses;
		public bool ExplosiveHealingKit;
		public bool TMFlash;
		public bool LiquidSnake;
		public bool RocketPack;
		public int DrillerTier; // 0 = none, 1 base, 2 hellstone, 3 chlorophyte, 4 luminite
		public bool TextBoxingGloves;
		public bool MuncherHead;
		public bool MuncherBody;
		public bool MuncherLegs;
		public bool MuncherSetBonus;

		/// <summary>TM Flash is only on in the Underground and Cavern layers, never on the surface or in the Underworld.</summary>
		public bool FlashActive;

		// ---------------- movement state ----------------
		public const float BaseUnreasonableSpeed = 34f;              // px per tick; vanilla max run speed is ~3 (6 with boots)
		public const float UnreasonableSpeed = BaseUnreasonableSpeed * 3f; // tier 2: triple
		public const float OmniSpeed = BaseUnreasonableSpeed * 4f;         // tier 3: quadruple
		public Vector2 OmniMomentum;
		private bool movementOverridden;
		private float restoreVelocityY;
		private bool restoreVelocity;

		// ---------------- dash (Rocket Pack / Driller Pack) ----------------
		public const int DashDown = 0, DashUp = 1, DashRight = 2, DashLeft = 3;
		public const float RocketSpeed = 36f;
		public const int RocketDuration = 28;
		public int DashDir = -1;
		public int DashTimer;
		public int DashDelay;
		public bool DashIsDrill;
		public Vector2 DashVelocity;
		public float DashRotation;

		// ---------------- glue ----------------
		public int GluedToNPC = -1;
		public Vector2 GlueOffset;

		// ---------------- Solid Snake (phasing) ----------------
		private Vector2 phaseVelocity;
		private Vector2 phaseTargetPosition;
		private bool phasingThisFrame;
		private int phaseFallStart;
		private bool wasPhasing;

		// ---------------- NPC Hand ----------------
		public int NPCHandTarget = -1;
		public Vector2 NPCHandOffset;

		// ---------------- Block Rod ----------------
		public bool Selecting;
		public Point SelectionStart;
		public Point SelectionEnd;
		public BlockRodClipboard Clipboard;

		// ---------------- misc ----------------
		private readonly int[] textHitCooldown = new int[Main.maxNPCs];
		private int muncherTimer;
		private int jellyBounceCooldown;

		public override void ResetEffects() {
			InstantUpDown = InstantLeftRight = UnreasonableUpDown = UnreasonableLeftRight = OmniMovement = false;
			SkyInABottle = ThreeDGlasses = ExplosiveHealingKit = TMFlash = LiquidSnake = RocketPack = TextBoxingGloves = false;
			MuncherHead = MuncherBody = MuncherLegs = MuncherSetBonus = false;
			DrillerTier = 0;
			FlashActive = false;

			// Double tap detection, lifted from ExampleMod's dash example.
			if (Player.controlDown && Player.releaseDown && Player.doubleTapCardinalTimer[DashDown] < 15)
				DashDir = DashDown;
			else if (Player.controlUp && Player.releaseUp && Player.doubleTapCardinalTimer[DashUp] < 15)
				DashDir = DashUp;
			else if (Player.controlRight && Player.releaseRight && Player.doubleTapCardinalTimer[DashRight] < 15 && Player.doubleTapCardinalTimer[DashLeft] == 0)
				DashDir = DashRight;
			else if (Player.controlLeft && Player.releaseLeft && Player.doubleTapCardinalTimer[DashLeft] < 15 && Player.doubleTapCardinalTimer[DashRight] == 0)
				DashDir = DashLeft;
			else
				DashDir = -1;
		}

		public override void UpdateDead() {
			GluedToNPC = -1;
			OmniMomentum = Vector2.Zero;
			DashTimer = 0;
			NPCHandTarget = -1;
			Player.fullRotation = 0f;
		}

		// ------------------------------------------------------------------------------------------------------------
		// Equipment effects
		// ------------------------------------------------------------------------------------------------------------
		public override void PostUpdateEquips() {
			FlashActive = TMFlash && (Player.ZoneDirtLayerHeight || Player.ZoneRockLayerHeight) && !Player.ZoneUnderworldHeight;
			if (FlashActive)
				Player.GetDamage(DamageClass.Generic) += 0.40f;

			if (LiquidSnake) {
				Player.ignoreWater = true;
				Player.accFlipper = true;
			}

			if (ThreeDGlasses)
				Player.buffImmune[ModContent.BuffType<ThreeDDebuff>()] = true;
		}

		public override void PostUpdateMiscEffects() {
			// Night of Moon Shoes: players float too.
			if (ChaosEvents.MoonShoesActive)
				Player.gravity *= 0.5f;
		}

		public override void PostUpdateRunSpeeds() {
			if (ChaosEvents.FrogRainActive) {
				// Everything is ice. Then twice as much ice. Then twice as much again.
				Player.runSlowdown *= 0.02f;
				Player.runAcceleration *= 0.35f;
			}
		}

		// ------------------------------------------------------------------------------------------------------------
		// Movement overrides. PreUpdateMovement runs after vanilla has computed velocity but before it moves the player.
		// ------------------------------------------------------------------------------------------------------------
		public override void PreUpdateMovement() {
			movementOverridden = false;
			restoreVelocity = false;
			phasingThisFrame = false;

			if (Player.dead || Player.mount.Active)
				return;

			if (GluedToNPC >= 0 && UpdateGluePosition())
				return;

			if (Player.HasBuff<CardboardBox>()) {
				UpdatePhasing();
				return;
			}

			UpdateDash();
			if (DashTimer > 0)
				return;

			UpdateInstantMovement();
		}

		private bool UpdateGluePosition() {
			NPC host = Main.npc[GluedToNPC];
			if (!host.active) {
				GluedToNPC = -1;
				return false;
			}
			Player.position = host.position + GlueOffset;
			Player.velocity = Vector2.Zero;
			Player.fallStart = (int)(Player.position.Y / 16f);
			return true;
		}

		private void UpdateInstantMovement() {
			bool overrideX = false, overrideY = false;
			float vx = Player.velocity.X, vy = Player.velocity.Y;
			float runMax = Math.Max(Player.maxRunSpeed, Player.accRunSpeed);
			float jumpMax = Player.jumpSpeed + Player.jumpSpeedBoost;

			if (OmniMovement) {
				// Keys set momentum. Releasing keeps it. Opposite key flips it. No diagonal normalization. No brakes.
				if (Player.controlLeft) OmniMomentum.X = -OmniSpeed;
				if (Player.controlRight) OmniMomentum.X = OmniSpeed;
				if (Player.controlUp) OmniMomentum.Y = -OmniSpeed;
				if (Player.controlDown) OmniMomentum.Y = OmniSpeed;
				if (OmniMomentum.X != 0f) { vx = OmniMomentum.X; overrideX = true; }
				if (OmniMomentum.Y != 0f) { vy = OmniMomentum.Y; overrideY = true; }
			}
			else {
				if (UnreasonableLeftRight) {
					if (Player.controlLeft) { vx = -UnreasonableSpeed; overrideX = true; }
					else if (Player.controlRight) { vx = UnreasonableSpeed; overrideX = true; }
				}
				else if (InstantLeftRight) {
					if (Player.controlLeft && Player.velocity.X > -runMax) Player.velocity.X = -runMax;
					else if (Player.controlRight && Player.velocity.X < runMax) Player.velocity.X = runMax;
				}

				if (UnreasonableUpDown) {
					if (Player.controlUp) { vy = -UnreasonableSpeed; overrideY = true; }
					else if (Player.controlDown) { vy = UnreasonableSpeed; overrideY = true; }
				}
				else if (InstantUpDown) {
					if (Player.controlUp && !Player.controlJump) Player.velocity.Y = -jumpMax * Player.gravDir;
					else if (Player.controlDown && Player.velocity.Y < Player.maxFallSpeed) Player.velocity.Y = Player.maxFallSpeed * Player.gravDir;
				}
			}

			if (!overrideX && !overrideY)
				return;

			// Unreasonable speeds tunnel straight through terrain with vanilla collision, so we sweep in small steps ourselves.
			Vector2 wanted = new Vector2(vx, vy);
			SweepMove(wanted, out bool hitX, out bool hitY);
			if (hitX) OmniMomentum.X = 0f;
			if (hitY) OmniMomentum.Y = 0f;

			// Vanilla would now move us again by velocity, so we hand it zero and restore the vertical momentum afterwards.
			movementOverridden = true;
			restoreVelocity = !overrideY && !hitY;
			restoreVelocityY = hitY ? 0f : vy;
			Player.velocity = Vector2.Zero;
			Player.fallStart = (int)(Player.position.Y / 16f);
			if (overrideX)
				Player.direction = vx < 0 ? -1 : 1;
			SpawnSpeedDust();
		}

		private void SweepMove(Vector2 velocity, out bool hitX, out bool hitY) {
			hitX = hitY = false;
			int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(velocity.X), Math.Abs(velocity.Y)) / 6f));
			Vector2 step = velocity / steps;
			for (int i = 0; i < steps; i++) {
				Vector2 allowed = Collision.TileCollision(Player.position, step, Player.width, Player.height, false, false, (int)Player.gravDir);
				Player.position += allowed;
				if (Math.Abs(allowed.X - step.X) > 0.001f) hitX = true;
				if (Math.Abs(allowed.Y - step.Y) > 0.001f) hitY = true;
				if (hitX && hitY)
					break;
				if (hitX) step.X = 0f;
				if (hitY) step.Y = 0f;
				if (step == Vector2.Zero)
					break;
			}
		}

		private void SpawnSpeedDust() {
			if (Main.netMode == NetmodeID.Server)
				return;
			for (int i = 0; i < 3; i++) {
				Dust dust = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.Smoke, 0f, 0f, 120, default, 1.4f);
				dust.noGravity = true;
				dust.velocity *= 0.3f;
			}
		}

		// ------------------------------------------------------------------------------------------------------------
		// Dashes
		// ------------------------------------------------------------------------------------------------------------
		private bool CanUseChaosDash() => (RocketPack || DrillerTier > 0) && Player.dashType == DashID.None && !Player.setSolar;

		private void UpdateDash() {
			if (DashDelay > 0)
				DashDelay--;

			if (CanUseChaosDash() && DashDir != -1 && DashDelay == 0 && DashTimer == 0) {
				DashIsDrill = !RocketPack; // Rocket Pack wins if both are equipped; nobody said this mod was balanced
				float speed = DashIsDrill ? 11f : RocketSpeed;
				DashVelocity = DashDir switch {
					DashUp => new Vector2(0f, -speed),
					DashDown => new Vector2(0f, speed),
					DashLeft => new Vector2(-speed, 0f),
					_ => new Vector2(speed, 0f),
				};
				DashTimer = DashIsDrill ? 32 : RocketDuration;
				DashDelay = DashIsDrill ? 45 : 70;
				DashRotation = DashVelocity.ToRotation();
				SoundEngine.PlaySound(DashIsDrill ? SoundID.Item23 : SoundID.Item13, Player.Center);
			}

			if (DashTimer <= 0) {
				if (Player.fullRotation != 0f && !Player.HasBuff<CardboardBox>()) {
					Player.fullRotation = 0f;
				}
				return;
			}

			DashTimer--;
			Player.fallStart = (int)(Player.position.Y / 16f);
			Player.eocDash = DashTimer;
			Player.armorEffectDrawShadowEOCShield = true;
			Player.fullRotation = DashRotation + (DashVelocity.X < 0 ? MathHelper.Pi : 0f);
			Player.fullRotationOrigin = Player.Size * 0.5f;

			if (DashIsDrill) {
				Player.velocity = DashVelocity;
				DrillThroughTiles();
			}
			else {
				// Rocket exhaust
				for (int i = 0; i < 4; i++) {
					Dust dust = Dust.NewDustDirect(Player.Center - DashVelocity * 0.5f - new Vector2(4f), 8, 8, DustID.Torch, 0f, 0f, 0, default, 2f);
					dust.noGravity = true;
					dust.velocity = -DashVelocity * 0.3f + Main.rand.NextVector2Circular(2f, 2f);
				}
				// Too fast for vanilla collision: sweep ourselves and blow up on the first wall.
				SweepMove(DashVelocity, out bool hitX, out bool hitY);
				Player.velocity = Vector2.Zero;
				if (hitX || hitY) {
					RocketImpact();
					return;
				}
				// Hitting an enemy ends the flight explosively.
				if (Player.whoAmI == Main.myPlayer) {
					foreach (NPC npc in Main.ActiveNPCs) {
						if (ChaosUtils.IsHostile(npc) && npc.Hitbox.Intersects(Player.Hitbox)) {
							RocketImpact();
							break;
						}
					}
				}
			}

			if (DashTimer == 0)
				Player.fullRotation = 0f;
		}

		private void RocketImpact() {
			if (DashTimer <= 0)
				return;
			DashTimer = 0;
			Player.fullRotation = 0f;
			Player.velocity = -DashVelocity * 0.2f;
			if (Player.whoAmI == Main.myPlayer) {
				int damage = (int)Player.GetTotalDamage(DamageClass.Melee).ApplyTo(90 + Player.statLifeMax2 / 5);
				Projectile.NewProjectile(Player.GetSource_Accessory(Player.HeldItem), Player.Center, Vector2.Zero, ModContent.ProjectileType<RocketPackExplosion>(), damage, 12f, Player.whoAmI);
			}
			Player.SetImmuneTimeForAllTypes(20);
		}

		private void DrillThroughTiles() {
			if (Player.whoAmI != Main.myPlayer)
				return;
			int power = DrillerTier switch { 1 => 55, 2 => 100, 3 => 200, _ => 230 };
			Rectangle next = Player.Hitbox;
			next.Offset((int)DashVelocity.X, (int)DashVelocity.Y);
			next.Inflate(2, 2);
			int x0 = next.Left / 16, x1 = next.Right / 16, y0 = next.Top / 16, y1 = next.Bottom / 16;
			for (int x = x0; x <= x1; x++) {
				for (int y = y0; y <= y1; y++) {
					if (!WorldGen.InWorld(x, y, 5))
						continue;
					Tile tile = Main.tile[x, y];
					if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileFrameImportant[tile.TileType])
						continue;
					for (int hit = 0; hit < 4 && Main.tile[x, y].HasTile; hit++)
						Player.PickTile(x, y, power);
				}
			}
			for (int i = 0; i < 3; i++) {
				Dust dust = Dust.NewDustDirect(Player.Center + DashVelocity * 1.5f - new Vector2(8f), 16, 16, DustID.Stone);
				dust.velocity = -DashVelocity * 0.2f + Main.rand.NextVector2Circular(3f, 3f);
			}
		}

		// ------------------------------------------------------------------------------------------------------------
		// Solid Snake: phase through blocks for a bit. Fall damage is still a thing. Sorry.
		// ------------------------------------------------------------------------------------------------------------
		private void UpdatePhasing() {
			float runMax = Math.Max(Player.maxRunSpeed, Player.accRunSpeed);
			if (!wasPhasing) {
				phaseVelocity = Player.velocity;
				phaseFallStart = (int)(Player.position.Y / 16f);
			}
			phaseVelocity.X = Player.controlLeft ? -runMax : Player.controlRight ? runMax : phaseVelocity.X * 0.8f;
			if (Player.controlUp || Player.controlJump)
				phaseVelocity.Y = -5.5f;
			else
				phaseVelocity.Y = Math.Min(phaseVelocity.Y + 0.4f, Player.maxFallSpeed);
			if (phaseVelocity.Y <= 0f)
				phaseFallStart = (int)(Player.position.Y / 16f);

			phaseTargetPosition = Player.position + phaseVelocity;
			// Keep inside the world, there is nothing to phase into out there.
			phaseTargetPosition.X = MathHelper.Clamp(phaseTargetPosition.X, 16f * 20f, Main.maxTilesX * 16f - 16f * 20f - Player.width);
			phaseTargetPosition.Y = MathHelper.Clamp(phaseTargetPosition.Y, 16f * 20f, Main.maxTilesY * 16f - 16f * 20f - Player.height);
			Player.velocity = Vector2.Zero;
			Player.direction = phaseVelocity.X < 0 ? -1 : phaseVelocity.X > 0 ? 1 : Player.direction;
			phasingThisFrame = true;
			wasPhasing = true;
			Player.fallStart = (int)(Player.position.Y / 16f);
		}

		private void EndPhasing() {
			wasPhasing = false;
			Player.fallStart = phaseFallStart; // does not prevent fall damage. We warned you.
			// If the box dissolves while inside a wall, push up until we find air.
			int guard = 0;
			while (Collision.SolidCollision(Player.position, Player.width, Player.height) && guard++ < 400)
				Player.position.Y -= 16f;
		}

		// ------------------------------------------------------------------------------------------------------------
		// After vanilla movement
		// ------------------------------------------------------------------------------------------------------------
		public override void PostUpdate() {
			if (phasingThisFrame) {
				Player.position = phaseTargetPosition;
				Player.velocity = Vector2.Zero;
			}
			else if (wasPhasing) {
				EndPhasing();
			}

			if (movementOverridden && restoreVelocity)
				Player.velocity.Y = restoreVelocityY;

			if (Player.whoAmI != Main.myPlayer || Player.dead)
				return;

			if (ChaosEvents.DeliriumActive && ChaosEvents.NearSpawn(Player))
				Player.AddBuff(ModContent.BuffType<Delirious>(), 10, quiet: true);

			if (ChaosEvents.JellyStormActive)
				UpdateJellyBounce();

			UpdateGlueAttachments();
			UpdateMuncher();
			if (TextBoxingGloves)
				UpdateTextBoxing();
			UpdateNPCHand();
		}

		// Bouncing Jelly Storm: players bounce off NPCs and other players too.
		private void UpdateJellyBounce() {
			if (jellyBounceCooldown > 0) {
				jellyBounceCooldown--;
				return;
			}
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!npc.Hitbox.Intersects(Player.Hitbox))
					continue;
				Bounce(npc.Center, npc.velocity.Length());
				Vector2 away = npc.Center - Player.Center;
				if (away != Vector2.Zero) away.Normalize(); else away = Vector2.UnitX;
				npc.velocity = away * Math.Max(6f, npc.velocity.Length()) * 3f;
				npc.netUpdate = true;
				return;
			}
			foreach (Player other in Main.ActivePlayers) {
				if (other.whoAmI == Player.whoAmI || other.dead || !other.Hitbox.Intersects(Player.Hitbox))
					continue;
				Bounce(other.Center, other.velocity.Length());
				return;
			}
		}

		private void Bounce(Vector2 from, float otherSpeed) {
			Vector2 away = Player.Center - from;
			if (away != Vector2.Zero) away.Normalize(); else away = -Vector2.UnitY;
			float speed = Math.Max(5f, (Player.velocity.Length() + otherSpeed) * 0.5f + 1f) * 3f;
			Player.velocity = away * Math.Min(speed, 30f);
			Player.fallStart = (int)(Player.position.Y / 16f);
			jellyBounceCooldown = 8;
			SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = 0.6f }, Player.Center);
		}

		private void UpdateGlueAttachments() {
			bool hasGlue = Player.HasBuff<GlueDebuff>();
			bool onFire = Player.onFire || Player.onFire2 || Player.onFire3 || Player.burned || Player.lavaWet;
			if (onFire && (hasGlue || GluedToNPC >= 0)) {
				if (hasGlue)
					Player.ClearBuff(ModContent.BuffType<GlueDebuff>());
				GluedToNPC = -1;
				return;
			}
			if (GluedToNPC >= 0)
				return;
			foreach (NPC npc in Main.ActiveNPCs) {
				// No allegiance checks: a glued player sticks to the Guide, a glued slime carries you off.
				bool npcGlued = npc.HasBuff<GlueDebuff>();
				if (!hasGlue && !npcGlued)
					continue;
				if (!npc.Hitbox.Intersects(Player.Hitbox))
					continue;
				GluedToNPC = npc.whoAmI;
				GlueOffset = Player.position - npc.position;
				SoundEngine.PlaySound(SoundID.Item16, Player.Center);
				break;
			}
		}

		private void UpdateMuncher() {
			if (!MuncherHead && !MuncherBody && !MuncherLegs)
				return;
			if (!Player.controlLeft && !Player.controlRight)
				return;
			if (++muncherTimer < 3)
				return;
			muncherTimer = 0;

			int power = 35;
			foreach (Item item in Player.inventory) {
				if (item.pick > power)
					power = item.pick;
			}
			int tx = Player.direction == 1 ? (Player.Hitbox.Right + 2) / 16 : (Player.Hitbox.Left - 2) / 16;
			int top = (int)Player.position.Y;
			var rows = new List<int>();
			if (MuncherHead) rows.Add((top + 6) / 16);
			if (MuncherBody) rows.Add((top + Player.height / 2) / 16);
			if (MuncherLegs) rows.Add((top + Player.height - 6) / 16);
			foreach (int ty in rows) {
				if (!WorldGen.InWorld(tx, ty, 5))
					continue;
				Tile tile = Main.tile[tx, ty];
				if (!tile.HasTile || !Main.tileSolid[tile.TileType])
					continue;
				for (int hit = 0; hit < 3 && Main.tile[tx, ty].HasTile; hit++)
					Player.PickTile(tx, ty, power);
				if (!Main.tile[tx, ty].HasTile) {
					SoundEngine.PlaySound(SoundID.Item2 with { Pitch = 0.4f }, Player.Center);
					if (MuncherSetBonus)
						Player.Heal(1);
				}
			}
		}

		// ------------------------------------------------------------------------------------------------------------
		// Text Boxing Gloves: every piece of text on screen is a weapon. Damage numbers, pickup text, and UI text.
		// ------------------------------------------------------------------------------------------------------------
		private static Vector2 UIToWorld(Vector2 uiPos) {
			// UI space (scaled by UIScale) -> screen pixels -> world, undoing the game zoom around the screen center.
			Vector2 screen = uiPos * Main.UIScale;
			Vector2 half = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
			return (screen - half) / Main.GameViewMatrix.Zoom + half + Main.screenPosition;
		}

		private static Rectangle UIRectToWorld(Rectangle ui) {
			Vector2 tl = UIToWorld(new Vector2(ui.X, ui.Y));
			Vector2 br = UIToWorld(new Vector2(ui.Right, ui.Bottom));
			return new Rectangle((int)tl.X, (int)tl.Y, (int)(br.X - tl.X), (int)(br.Y - tl.Y));
		}

		private void UpdateTextBoxing() {
			for (int i = 0; i < textHitCooldown.Length; i++) {
				if (textHitCooldown[i] > 0)
					textHitCooldown[i]--;
			}
			var rects = new List<(Rectangle rect, int length)>();

			// Damage numbers and other combat text (world space, centered on position).
			for (int i = 0; i < Main.combatText.Length; i++) {
				CombatText ct = Main.combatText[i];
				if (!ct.active || string.IsNullOrEmpty(ct.text))
					continue;
				Vector2 size = FontAssets.CombatText[ct.crit ? 1 : 0].Value.MeasureString(ct.text) * ct.scale;
				Rectangle rect = new Rectangle((int)(ct.position.X - size.X / 2f), (int)(ct.position.Y - size.Y / 2f), (int)size.X, (int)size.Y);
				rect.Inflate(6, 6);
				rects.Add((rect, ct.text.Length));
			}
			// Item pickup text.
			for (int i = 0; i < Main.popupText.Length; i++) {
				PopupText pt = Main.popupText[i];
				if (!pt.active || string.IsNullOrEmpty(pt.name))
					continue;
				string text = pt.stack > 1 ? $"{pt.name} ({pt.stack})" : pt.name;
				Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * pt.scale;
				Rectangle rect = new Rectangle((int)(pt.position.X - size.X / 2f), (int)(pt.position.Y - size.Y / 2f), (int)size.X, (int)size.Y);
				rect.Inflate(6, 6);
				rects.Add((rect, text.Length));
			}
			// UI text: the held item's name above the hotbar, the chat box when open, and the mouse-over text (tooltips, NPC names).
			string heldName = Player.HeldItem?.Name;
			if (!string.IsNullOrEmpty(heldName) && !Main.playerInventory) {
				Vector2 size = FontAssets.MouseText.Value.MeasureString(heldName);
				rects.Add((UIRectToWorld(new Rectangle((int)(236f - size.X / 2f), 0, (int)size.X, (int)size.Y)), heldName.Length));
			}
			if (Main.drawingPlayerChat) {
				int uiHeight = (int)(Main.screenHeight / Main.UIScale);
				rects.Add((UIRectToWorld(new Rectangle(78, uiHeight - 36, 520, 32)), 40)); // the input line
				rects.Add((UIRectToWorld(new Rectangle(78, uiHeight - 36 - 21 * 10, 520, 21 * 10)), 80)); // recent messages above it
			}
			string hover = Main.hoverItemName;
			if (!string.IsNullOrEmpty(hover) && !Main.playerInventory) {
				Vector2 size = FontAssets.MouseText.Value.MeasureString(hover);
				Vector2 mouse = new Vector2(Main.mouseX, Main.mouseY) / Main.UIScale + new Vector2(10f, 10f);
				rects.Add((UIRectToWorld(new Rectangle((int)mouse.X, (int)mouse.Y, (int)size.X, (int)size.Y)), hover.Length));
			}

			if (rects.Count == 0)
				return;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (!ChaosUtils.IsHostile(npc) || textHitCooldown[npc.whoAmI] > 0)
					continue;
				foreach (var (rect, length) in rects) {
					if (!rect.Intersects(npc.Hitbox))
						continue;
					int damage = (int)Player.GetTotalDamage(DamageClass.Melee).ApplyTo(6 + length * 3);
					npc.SimpleStrikeNPC(damage, npc.Center.X < Player.Center.X ? -1 : 1, Main.rand.NextBool(10), 3f, DamageClass.Melee, true);
					textHitCooldown[npc.whoAmI] = 20;
					break;
				}
			}
		}

		private void UpdateNPCHand() {
			if (NPCHandTarget < 0)
				return;
			NPC npc = Main.npc[NPCHandTarget];
			bool holding = Player.HeldItem.type == ModContent.ItemType<Content.Items.Tools.NPCHand>() && Player.channel && !Player.dead;
			if (!npc.active || !holding) {
				NPCHandTarget = -1;
				return;
			}
			Vector2 target = Main.MouseWorld + NPCHandOffset - npc.Size * 0.5f;
			npc.position = target;
			npc.velocity = Vector2.Zero;
			npc.netUpdate = true;
			BagofChaos.SendMoveNPC(npc, target);
		}

		// ------------------------------------------------------------------------------------------------------------
		// Extra jumps: Sky in a Bottle replaces every double jump with a cloud platform.
		// ------------------------------------------------------------------------------------------------------------
		public override void ModifyExtraJumpDurationMultiplier(ExtraJump jump, ref float duration) {
			if (SkyInABottle)
				duration = 0f;
		}

		public override void OnExtraJumpStarted(ExtraJump jump, ref bool playSound) {
			if (!SkyInABottle)
				return;
			playSound = false;
			Player.jump = 0;
			Player.velocity.Y = 0f;
			int tileY = (int)((Player.position.Y + Player.height + 1f) / 16f);
			int tileX = (int)(Player.Center.X / 16f);
			Player.position.Y = tileY * 16f - Player.height; // stand exactly on the new cloud
			if (Player.whoAmI == Main.myPlayer)
				BagofChaos.SendPlaceCloud(tileX, tileY, 5);
			SoundEngine.PlaySound(SoundID.Item16 with { Pitch = 0.5f }, Player.Center);
			for (int i = 0; i < 12; i++) {
				Dust dust = Dust.NewDustDirect(new Vector2(tileX * 16f - 40f, tileY * 16f - 4f), 96, 12, DustID.Cloud, 0f, 0f, 100, Color.White, 1.6f);
				dust.noGravity = true;
				dust.velocity *= 0.4f;
			}
		}

		public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot) {
			// Being carried around by a glued zombie is bad enough without it also biting you every frame.
			return GluedToNPC != npc.whoAmI;
		}

		// ------------------------------------------------------------------------------------------------------------
		// Weedy Beer: every player is reflected upside down across the line under the local player's feet.
		// ------------------------------------------------------------------------------------------------------------
		public override void DrawPlayer(Camera camera) {
			if (Main.gameMenu || !Main.LocalPlayer.HasBuff<Weedy>() || Player.dead)
				return;
			float mirrorY = Main.LocalPlayer.Bottom.Y;
			Vector2 center = Player.Center + new Vector2(0f, Player.gfxOffY);
			Vector2 mirroredCenter = new Vector2(center.X, 2f * mirrorY - center.Y);
			Vector2 position = mirroredCenter - Player.Size * 0.5f;
			// A half turn is a vertical flip plus a horizontal flip; close enough to a reflection for a drunk.
			Main.PlayerRenderer.DrawPlayer(camera, Player, position, Player.fullRotation + MathHelper.Pi, Player.Size * 0.5f, 0.45f);
		}
	}

	// Tiles copied by the Block Rod. Tile-frame-important tiles (furniture, trees, etc.) are left where they are.
	public class BlockRodClipboard
	{
		public struct TileSnapshot
		{
			public bool HasTile;
			public ushort TileType;
			public ushort WallType;
			public SlopeType Slope;
			public bool HalfBlock;
			public byte TileColor;
			public byte WallColor;
			public byte LiquidAmount;
			public int LiquidType;
			public bool RedWire, BlueWire, GreenWire, YellowWire;
			public bool Actuator, Actuated;
		}

		public int Width;
		public int Height;
		public Point Origin;
		public TileSnapshot[,] Tiles;

		public static BlockRodClipboard Cut(Point topLeft, int width, int height) {
			var clip = new BlockRodClipboard { Width = width, Height = height, Tiles = new TileSnapshot[width, height] };
			for (int x = 0; x < width; x++) {
				for (int y = 0; y < height; y++) {
					int wx = topLeft.X + x, wy = topLeft.Y + y;
					if (!WorldGen.InWorld(wx, wy, 5))
						continue;
					Tile tile = Main.tile[wx, wy];
					bool movable = tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileFrameImportant[tile.TileType] && !TileID.Sets.BasicChest[tile.TileType];
					var snap = new TileSnapshot {
						HasTile = movable,
						TileType = movable ? tile.TileType : (ushort)0,
						WallType = tile.WallType,
						Slope = tile.Slope,
						HalfBlock = tile.IsHalfBlock,
						TileColor = tile.TileColor,
						WallColor = tile.WallColor,
						LiquidAmount = tile.LiquidAmount,
						LiquidType = tile.LiquidType,
						RedWire = tile.RedWire, BlueWire = tile.BlueWire, GreenWire = tile.GreenWire, YellowWire = tile.YellowWire,
						Actuator = tile.HasActuator, Actuated = tile.IsActuated,
					};
					clip.Tiles[x, y] = snap;
					if (movable) {
						tile.HasTile = false;
						tile.Slope = SlopeType.Solid;
						tile.IsHalfBlock = false;
						tile.TileColor = 0;
						tile.HasActuator = false;
						tile.IsActuated = false;
					}
					tile.WallType = 0;
					tile.WallColor = 0;
					tile.LiquidAmount = 0;
					tile.RedWire = tile.BlueWire = tile.GreenWire = tile.YellowWire = false;
				}
			}
			FrameArea(topLeft, width, height);
			return clip;
		}

		public bool CanPlace(Point topLeft) {
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					int wx = topLeft.X + x, wy = topLeft.Y + y;
					if (!WorldGen.InWorld(wx, wy, 10))
						return false;
					if (Tiles[x, y].HasTile && Main.tile[wx, wy].HasTile)
						return false;
				}
			}
			return true;
		}

		public void Paste(Point topLeft) {
			for (int x = 0; x < Width; x++) {
				for (int y = 0; y < Height; y++) {
					int wx = topLeft.X + x, wy = topLeft.Y + y;
					if (!WorldGen.InWorld(wx, wy, 5))
						continue;
					TileSnapshot snap = Tiles[x, y];
					Tile tile = Main.tile[wx, wy];
					if (snap.HasTile && !tile.HasTile) {
						tile.HasTile = true;
						tile.TileType = snap.TileType;
						tile.Slope = snap.Slope;
						tile.IsHalfBlock = snap.HalfBlock;
						tile.TileColor = snap.TileColor;
						tile.HasActuator = snap.Actuator;
						tile.IsActuated = snap.Actuated;
					}
					if (snap.WallType != 0 && tile.WallType == 0) {
						tile.WallType = snap.WallType;
						tile.WallColor = snap.WallColor;
					}
					if (snap.LiquidAmount > 0 && tile.LiquidAmount == 0 && !tile.HasTile) {
						tile.LiquidAmount = snap.LiquidAmount;
						tile.LiquidType = snap.LiquidType;
					}
					tile.RedWire |= snap.RedWire;
					tile.BlueWire |= snap.BlueWire;
					tile.GreenWire |= snap.GreenWire;
					tile.YellowWire |= snap.YellowWire;
				}
			}
			FrameArea(topLeft, Width, Height);
		}

		public static void FrameArea(Point topLeft, int width, int height) {
			for (int x = topLeft.X - 1; x <= topLeft.X + width; x++) {
				for (int y = topLeft.Y - 1; y <= topLeft.Y + height; y++) {
					if (WorldGen.InWorld(x, y, 5)) {
						WorldGen.SquareTileFrame(x, y);
						WorldGen.SquareWallFrame(x, y);
					}
				}
			}
			if (Main.netMode != NetmodeID.SinglePlayer)
				NetMessage.SendTileSquare(-1, topLeft.X - 1, topLeft.Y - 1, width + 2, height + 2);
		}
	}
}
