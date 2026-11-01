using BagofChaos.Common.Players;
using BagofChaos.Content.Buffs;
using BagofChaos.Content.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
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
		public int BanditBounces;  // Rubber Banding Bandit set bonus bounce budget

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
			ChaosPlayer owner = Owner(projectile);
			if (owner == null || projectile.npcProj || projectile.trap)
				return;
			if (owner.FlashActive && IsHoming(projectile.type))
				HomingLocked = true;
			if (owner.BanditSetBonus && projectile.friendly && projectile.damage > 0) {
				BanditBounces = 1; // +1 bounce
				if (projectile.penetrate > 0)
					projectile.penetrate++;
			}
		}

		public override bool PreAI(Projectile projectile) {
			preAIVelocity = projectile.velocity;
			capturedPreAI = true;
			return true;
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

		// Set bonus: 50% of your projectiles bounce off tiles (once).
		public override bool OnTileCollide(Projectile projectile, Vector2 oldVelocity) {
			if (BanditBounces > 0 && projectile.friendly && !projectile.minion && Main.rand.NextBool()) {
				BanditBounces--;
				if (projectile.velocity.X != oldVelocity.X)
					projectile.velocity.X = -oldVelocity.X;
				if (projectile.velocity.Y != oldVelocity.Y)
					projectile.velocity.Y = -oldVelocity.Y;
				SoundEngine.PlaySound(SoundID.Item10, projectile.position);
				return false;
			}
			return true;
		}

		public override bool? CanHitNPC(Projectile projectile, NPC target) {
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
