using BagofChaos.Common.Players;
using BagofChaos.Content.Buffs;
using BagofChaos.Content.NPCs;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common.GlobalProjectiles
{
	public class ChaosGlobalProjectile : GlobalProjectile
	{
		public override bool InstancePerEntity => true;

		public bool Venomous;      // Venom Snake
		public bool Poisonous;     // Rattling Gun / Pocket Rats clouds
		public bool FromChainCan;  // Chain Can: summons tin cans on hit
		public bool HomingLocked;  // TM Flash: no homing underground
		public bool Replica;       // Arrow of Replication duplicates can't duplicate
		public int BanditBounces;  // Rubber Banding Bandit set bonus: tile bounces so far
		public bool BanditEligible;
		public bool BanditHoming;  // armed by hitting an enemy, disarmed by hitting a tile
		public int BanditBaseDamage;
		public int BanditLastHit = -1;
		public bool Reflected;     // captured by the Warpaint magnet: belongs to the player now and never hurts players
		public bool BossCopy;      // fired by the Slot-bar Caterpillar from a copied hotbar weapon: hostile, never hurts NPCs
		public const int BanditMaxBounces = 5;

		private Vector2 preAIVelocity;
		private bool capturedPreAI;

		private static readonly int[] HomingTypes = {
			ProjectileID.ChlorophyteBullet, ProjectileID.ChlorophyteArrow, ProjectileID.MagicMissile, ProjectileID.Flamelash, ProjectileID.RainbowRodBullet,
			ProjectileID.Bat, ProjectileID.NebulaArcanum, ProjectileID.NebulaBlaze1, ProjectileID.NebulaBlaze2, ProjectileID.SpectreWrath, ProjectileID.LostSoulFriendly,
			ProjectileID.VenomArrow, ProjectileID.HallowStar, ProjectileID.MoonlordTurretLaser, ProjectileID.CrystalVileShardHead,
			ProjectileID.LunarFlare, ProjectileID.MagnetSphereBolt, ProjectileID.NebulaLaser, ProjectileID.FrostBlastFriendly, ProjectileID.SpiritFlame,
			ProjectileID.PhantasmArrow, ProjectileID.MoonlordArrow, ProjectileID.MoonlordBullet, ProjectileID.SmokeBomb, ProjectileID.Bee, ProjectileID.GiantBee,
			ProjectileID.Wasp, ProjectileID.PoisonFang, ProjectileID.DeathLaser, ProjectileID.EmeraldBolt, ProjectileID.ShadowFlame, ProjectileID.ShadowFlameArrow,
		};

		public static bool IsHoming(int type) => System.Array.IndexOf(HomingTypes, type) >= 0;

		/// <summary>Thrown or fired projectiles only: no minions, sentries, pets, whips, yoyos, flails, spears or hooks.</summary>
		public static bool IsBanditEligible(Projectile projectile) {
			if (!projectile.friendly || projectile.damage <= 0 || projectile.minion || projectile.sentry || Main.projPet[projectile.type] || !projectile.tileCollide)
				return false;
			if (ProjectileID.Sets.IsAWhip[projectile.type] || projectile.ownerHitCheck)
				return false;
			int ai = projectile.aiStyle;
			return ai != ProjAIStyleID.Hook && ai != ProjAIStyleID.Yoyo && ai != ProjAIStyleID.Flail && ai != ProjAIStyleID.Spear && ai != ProjAIStyleID.Drill
				&& ai != ProjAIStyleID.HeldProjectile && ai != ProjAIStyleID.ShortSword;
		}

		public static bool IsExplosive(Projectile projectile) => ProjectileID.Sets.Explosive[projectile.type] || projectile.aiStyle == ProjAIStyleID.Explosive;

		private static ChaosPlayer Owner(Projectile projectile) {
			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || !Main.player[projectile.owner].active)
				return null;
			return Main.player[projectile.owner].GetModPlayer<ChaosPlayer>();
		}

		private static bool OwnerHasHealingKit(Projectile projectile) {
			return projectile.friendly && !projectile.npcProj && !projectile.trap && Owner(projectile)?.ExplosiveHealingKit == true;
		}

		public override void OnSpawn(Projectile projectile, IEntitySource source) {
			// Anything a boss-copied shot spawns (stars, shards, splits) is the boss's too.
			if (source is EntitySource_Parent { Entity: Projectile parent } && parent.TryGetGlobalProjectile(out ChaosGlobalProjectile parentGlobal) && parentGlobal.BossCopy) {
				BossCopy = true;
				projectile.friendly = false;
				projectile.hostile = true;
				return;
			}
			ChaosPlayer owner = Owner(projectile);
			if (owner == null || projectile.npcProj || projectile.trap)
				return;
			// Triforces: whatever your weapon, ammo or minion fires takes the converted class too.
			if (projectile.friendly || projectile.minion || projectile.sentry)
				projectile.DamageType = owner.EffectiveDamageClass(projectile.DamageType);
			if (owner.FlashActive && IsHoming(projectile.type))
				HomingLocked = true;
			if (owner.BanditSetBonus && IsBanditEligible(projectile)) {
				BanditEligible = true;
				BanditBaseDamage = projectile.damage;
				if (projectile.penetrate > 0)
					projectile.penetrate += 2; // room to hit, home in, and hit again
			}
		}

		public override bool PreAI(Projectile projectile) {
			preAIVelocity = projectile.velocity;
			capturedPreAI = true;
			if (Reflected) {
				projectile.friendly = true;
				projectile.hostile = false;
			}
			if (BossCopy) {
				projectile.friendly = false;
				projectile.hostile = true;
			}
			if (IsPinball(projectile)) {
				TanningPinball(projectile);
				return false; // the minion's own AI would keep flying it home
			}
			return true;
		}

		// ---- Tanning Mirror pinball ----
		private static readonly int[] PinballExcluded = {
			ProjectileID.StardustDragon1, ProjectileID.StardustDragon2, ProjectileID.StardustDragon3, ProjectileID.StardustDragon4,
		};

		private static bool IsPinball(Projectile projectile) {
			if (!projectile.minion || projectile.sentry || System.Array.IndexOf(PinballExcluded, projectile.type) >= 0)
				return false;
			ChaosPlayer owner = Owner(projectile);
			return owner != null && owner.TanningMirrorHeld && !owner.Player.dead;
		}

		public override bool MinionContactDamage(Projectile projectile) => IsPinball(projectile);

		private static void TanningPinball(Projectile minion) {
			ChaosPlayer owner = Owner(minion);
			Player player = owner.Player;
			minion.timeLeft = System.Math.Max(minion.timeLeft, 2); // its AI normally does this
			minion.tileCollide = false;
			Vector2 apex = owner.TanApex;
			Vector2 v = minion.velocity;
			v.Y += 0.18f; // pinball tables slope down toward the flippers

			// Bumper floor just above the player's head: everything that falls onto it gets kicked back up.
			if (minion.Bottom.Y >= owner.TanFloorY && v.Y > 0f) {
				minion.Bottom = new Vector2(minion.Bottom.X, owner.TanFloorY);
				v.Y = -System.Math.Max(v.Y * 1.05f, 10f);
				v.X += Main.rand.NextFloat(-3f, 3f);
				if (Main.netMode != NetmodeID.Server)
					SoundEngine.PlaySound(SoundID.Item56 with { Volume = 0.4f, Pitch = 0.4f }, minion.Center);
			}

			// Cone walls and the top of the cone reflect the velocity.
			Vector2 d = minion.Center - apex;
			float angle = MathHelper.WrapAngle(d.ToRotation() + MathHelper.PiOver2); // 0 = straight up
			if (System.Math.Abs(angle) > ChaosPlayer.TanHalfAngle && d.Y < 0f) {
				float sign = System.Math.Sign(angle);
				Vector2 wall = (-Vector2.UnitY).RotatedBy(sign * ChaosPlayer.TanHalfAngle);
				Vector2 inward = wall.RotatedBy(-sign * MathHelper.PiOver2);
				if (Vector2.Dot(v, inward) < 0f)
					v -= 2f * Vector2.Dot(v, inward) * inward;
				minion.Center = apex + wall * System.Math.Max(d.Length(), 24f) + inward * 4f;
			}
			if (d.Length() > ChaosPlayer.TanRange) {
				Vector2 inward = -d.SafeNormalize(Vector2.UnitY);
				if (Vector2.Dot(v, inward) < 0f)
					v -= 2f * Vector2.Dot(v, inward) * inward;
				minion.Center = apex + d.SafeNormalize(-Vector2.UnitY) * (ChaosPlayer.TanRange - 4f);
			}
			// Fell out the bottom somehow (the player moved): drop it back onto the table.
			if (d.Y > 0f)
				minion.Center = apex + new Vector2(0f, -40f);

			// Minions bounce off each other.
			foreach (Projectile other in Main.ActiveProjectiles) {
				if (other.whoAmI <= minion.whoAmI || other.owner != minion.owner || !other.minion || !other.Hitbox.Intersects(minion.Hitbox))
					continue;
				(v, other.velocity) = (other.velocity, v);
			}

			if (v.Length() > 24f)
				v = Vector2.Normalize(v) * 24f;
			minion.velocity = v;
			minion.rotation += v.X * 0.04f;
			minion.friendly = true;
		}

		public override void PostAI(Projectile projectile) {
			if (!capturedPreAI)
				return;
			if (HomingLocked) {
				// Keep the speed the AI wants but refuse to let it steer.
				float speed = projectile.velocity.Length();
				if (preAIVelocity != Vector2.Zero && speed > 0.01f)
					projectile.velocity = Vector2.Normalize(preAIVelocity) * speed;
			}

			UpdateBanditHoming(projectile);
			if (Reflected) {
				projectile.friendly = true;
				projectile.hostile = false;
			}
			if (BossCopy) {
				projectile.friendly = false;
				projectile.hostile = true;
			}

			// Rubber Banding Bandit Goggles: grappling hooks latch onto your own minions.
			ChaosPlayer owner = Owner(projectile);
			if (owner != null && owner.BanditGoggles && projectile.aiStyle == ProjAIStyleID.Hook) {
				if (projectile.ai[0] == 2f && projectile.localAI[1] > 0f) {
					Projectile minion = Main.projectile[(int)projectile.localAI[1] - 1];
					if (minion.active && minion.minion && minion.owner == projectile.owner)
						projectile.Center = minion.Center;
					else
						projectile.localAI[1] = 0f;
				}
				else if (projectile.ai[0] == 0f) {
					foreach (Projectile minion in Main.ActiveProjectiles) {
						if (minion.owner != projectile.owner || !minion.minion || !minion.Hitbox.Intersects(projectile.Hitbox))
							continue;
						projectile.ai[0] = 2f; // latched
						projectile.velocity = Vector2.Zero;
						projectile.localAI[1] = minion.whoAmI + 1;
						projectile.netUpdate = true;
						break;
					}
				}
			}

			// Rubber Banding Bandit Bandana: your projectiles bounce off allies in the direction they're facing.
			if (owner != null && owner.BanditBandana && projectile.friendly && !projectile.minion && !projectile.npcProj) {
				foreach (Player ally in Main.ActivePlayers) {
					if (ally.whoAmI == projectile.owner || ally.dead || !ally.Hitbox.Intersects(projectile.Hitbox))
						continue;
					float speed = projectile.velocity.Length();
					projectile.velocity = new Vector2(ally.direction * speed, 0f);
					break;
				}
			}
		}

		// Set bonus: projectiles bounce off tiles up to five times, losing 10% of their damage per bounce (down to -50%).
		// Hitting a tile also switches off any homing, so it goes back to bouncing physically.
		public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity) {
			if (BanditEligible && BanditBounces < BanditMaxBounces) {
				BanditBounces++;
				BanditHoming = false;
				if (projectile.velocity.X != oldVelocity.X)
					projectile.velocity.X = -oldVelocity.X;
				if (projectile.velocity.Y != oldVelocity.Y)
					projectile.velocity.Y = -oldVelocity.Y;
				projectile.damage = System.Math.Max(1, (int)(BanditBaseDamage * (1f - 0.1f * BanditBounces)));
				projectile.netUpdate = true;
				SoundEngine.PlaySound(SoundID.Item10, projectile.position);
				return false;
			}
			return true;
		}

		// After a hit, snap the projectile straight at the next enemy. Re-targets instantly every frame.
		private void UpdateBanditHoming(Projectile projectile) {
			if (!BanditHoming || projectile.owner != Main.myPlayer)
				return;
			NPC target = null;
			float best = 700f;
			foreach (NPC npc in Main.ActiveNPCs) {
				if (npc.whoAmI == BanditLastHit || !npc.CanBeChasedBy(projectile))
					continue;
				float distance = Vector2.Distance(npc.Center, projectile.Center);
				if (distance < best) { best = distance; target = npc; }
			}
			if (target == null)
				return;
			float speed = System.Math.Max(projectile.velocity.Length(), 8f);
			Vector2 snapped = (target.Center - projectile.Center).SafeNormalize(Vector2.UnitX) * speed;
			if (Vector2.Distance(snapped, projectile.velocity) > 1f)
				projectile.netUpdate = true;
			projectile.velocity = snapped;
		}

		public override bool CanHitPlayer(Projectile projectile, Player target) => !Reflected;

		public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter) {
			bitWriter.WriteBit(Reflected);
			bitWriter.WriteBit(BanditHoming);
			bitWriter.WriteBit(BossCopy);
		}

		public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader) {
			Reflected = bitReader.ReadBit();
			BanditHoming = bitReader.ReadBit();
			BossCopy = bitReader.ReadBit();
		}

		public override bool? CanHitNPC(Projectile projectile, NPC target) {
			if (BossCopy)
				return false;
			if (IsExplosive(projectile) && OwnerHasHealingKit(projectile))
				return false;
			if (projectile.friendly && !projectile.npcProj && Owner(projectile)?.Stalemate == true)
				return false;
			return null;
		}

		public override bool CanHitPvp(Projectile projectile, Player target) {
			if (IsExplosive(projectile) && OwnerHasHealingKit(projectile))
				return false;
			return true;
		}

		public override bool? CanCutTiles(Projectile projectile) {
			if (IsExplosive(projectile) && OwnerHasHealingKit(projectile))
				return false;
			return null;
		}

		public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers) {
			// Tanning Mirror tag: minions hit tanned enemies harder.
			if (projectile.IsMinionOrSentryRelated && target.HasBuff<Tanned>())
				modifiers.FlatBonusDamage += 8f * ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
		}

		public override bool PreKill(Projectile projectile, int timeLeft) {
			if (!IsExplosive(projectile) || !OwnerHasHealingKit(projectile))
				return true;

			// Replace the explosion entirely: no damage, no block destruction, keep the spectacle.
			float radius = projectile.aiStyle == ProjAIStyleID.Explosive ? 140f : 90f;
			int heal = System.Math.Clamp(projectile.damage / 2, 5, 100);
			Vector2 center = projectile.Center;

			if (Main.netMode != NetmodeID.Server) {
				SoundEngine.PlaySound(SoundID.Item14, center);
				for (int i = 0; i < 30; i++) {
					Dust dust = Dust.NewDustPerfect(center, DustID.HealingPlus, Main.rand.NextVector2Circular(8f, 8f), 0, default, 1.8f);
					dust.noGravity = true;
				}
				for (int i = 0; i < 20; i++) {
					Dust dust = Dust.NewDustPerfect(center, DustID.Smoke, Main.rand.NextVector2Circular(5f, 5f), 100, Color.LightGreen, 2f);
					dust.noGravity = true;
				}
			}

			if (projectile.owner == Main.myPlayer) {
				foreach (Player player in Main.ActivePlayers) {
					if (!player.dead && Vector2.Distance(player.Center, center) <= radius)
						ChaosUtils.HealPlayer(player, heal);
				}
			}
			if (Main.netMode != NetmodeID.MultiplayerClient || projectile.owner == Main.myPlayer) {
				foreach (NPC npc in Main.ActiveNPCs) {
					if ((npc.friendly || npc.townNPC) && Vector2.Distance(npc.Center, center) <= radius)
						ChaosUtils.HealNPC(npc, heal);
				}
			}
			return false;
		}

		public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone) {
			if (BanditEligible) {
				BanditHoming = true;
				BanditLastHit = target.whoAmI;
			}
			if (Venomous)
				target.AddBuff(BuffID.Venom, 300);
			if (Poisonous)
				target.AddBuff(BuffID.Poisoned, 420);
			if (FromChainCan && !target.friendly && target.type != ModContent.NPCType<TinCan>() && projectile.owner == Main.myPlayer) {
				int cans = NPC.CountNPCS(ModContent.NPCType<TinCan>());
				if (cans < 12)
					BagofChaos.SendSpawnNPC(ModContent.NPCType<TinCan>(), target.Center + new Vector2(Main.rand.NextFloat(-30f, 30f), -20f));
			}
		}

		// Rubber Banding Bandit Goggles: your grappling hooks latch onto your minions.
		public override bool PreDraw(Projectile projectile, ref Color lightColor) => true;
	}
}
