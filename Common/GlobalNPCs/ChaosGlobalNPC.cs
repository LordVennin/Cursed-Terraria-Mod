using System.Collections.Generic;
using BagofChaos.Common.Players;
using BagofChaos.Common.Systems;
using BagofChaos.Content.Buffs;
using BagofChaos.Content.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.Utilities;

namespace BagofChaos.Common.GlobalNPCs
{
	// One GlobalNPC to rule all the nonsense: cosmetic sprite abuse, glue, life link, null gravity, events and silhouettes.
	public class ChaosGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		// --- cosmetic flags (synced via NPCVisual packet) ---
		public bool Googly;      // Googly Eye ammo
		public bool Slashed;     // Sprite Slasher: bottom half drawn on top of the top half
		public bool Stretched;   // "You're Long?" ammo: 4x wide

		// --- glue ---
		public int GluedToNPC = -1;
		public Vector2 GlueOffset;
		public int GlueBurnCooldown;

		// --- null gravity bookkeeping ---
		private bool nullGravityCaptured;
		private bool originalNoGravity;

		// --- delirium silhouette ---
		public bool Silhouette;

		// --- pupils for the googly eyes ---
		private Vector2 pupilA;
		private Vector2 pupilB;
		private Vector2 lastVelocity;

		public byte GetVisualFlags() => (byte)((Googly ? 1 : 0) | (Slashed ? 2 : 0) | (Stretched ? 4 : 0) | (Silhouette ? 8 : 0));

		public void ApplyVisualFlags(byte flags) {
			Googly = (flags & 1) != 0;
			Slashed = (flags & 2) != 0;
			Stretched = (flags & 4) != 0;
			Silhouette = (flags & 8) != 0;
		}

		public override void OnSpawn(NPC npc, IEntitySource source) {
			if (Main.netMode != NetmodeID.MultiplayerClient && ChaosEvents.DeliriumActive && ChaosUtils.IsHostile(npc) && !npc.boss && npc.ModNPC == null) {
				Silhouette = true;
				npc.netUpdate = true;
			}
		}

		// Silhouettes need to reach new clients too; piggyback on the NPC sync.
		public override void SendExtraAI(NPC npc, BitWriter bitWriter, System.IO.BinaryWriter binaryWriter) {
			binaryWriter.Write(GetVisualFlags());
		}

		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, System.IO.BinaryReader binaryReader) {
			ApplyVisualFlags(binaryReader.ReadByte());
		}

		// ------------------------------------------------------------------------------------------------------------
		// AI hooks
		// ------------------------------------------------------------------------------------------------------------
		public override bool PreAI(NPC npc) {
			// Night of Moon Shoes: everything is floaty.
			if (ChaosEvents.MoonShoesActive)
				npc.GravityMultiplier *= 0.5f;

			// Null Gravity Beacon.
			bool inField = NullGravitySystem.InField(npc.Center);
			if (inField) {
				if (!nullGravityCaptured) {
					originalNoGravity = npc.noGravity;
					nullGravityCaptured = true;
				}
				npc.noGravity = true;
				if (originalNoGravity && !npc.boss && !npc.townNPC) {
					// Flyers lose their steering and just drift on whatever momentum they had.
					npc.velocity *= 0.995f;
					npc.rotation += npc.velocity.X * 0.01f;
					return false;
				}
			}
			else if (nullGravityCaptured) {
				npc.noGravity = originalNoGravity;
				nullGravityCaptured = false;
			}

			return true;
		}

		public override void PostAI(NPC npc) {
			UpdateGlue(npc);

			if (ChaosEvents.JellyStormActive)
				UpdateJellyBounce(npc);

			if (GlueBurnCooldown > 0)
				GlueBurnCooldown--;

			if (Googly)
				UpdateGooglyEyes(npc);
		}

		// ------------------------------------------------------------------------------------------------------------
		// Glue: anything with the debuff sticks to anything it touches. No allegiance checks. No detachment mechanic.
		// ------------------------------------------------------------------------------------------------------------
		private void UpdateGlue(NPC npc) {
			bool hasGlue = npc.HasBuff<GlueDebuff>();

			// Fire burns the glue away (and whatever was stuck to it comes loose, screaming).
			if (hasGlue && IsOnFire(npc)) {
				npc.RequestBuffRemoval(ModContent.BuffType<GlueDebuff>());
				DetachEverythingFrom(npc);
				if (GlueBurnCooldown == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
					GlueBurnCooldown = 120; // prevents fire/glue damage loops
					npc.SimpleStrikeNPC(20, 0, noPlayerInteraction: true);
				}
				hasGlue = false;
			}

			// Only the authority decides new attachments; positions get synced through netUpdate.
			if (hasGlue && Main.netMode != NetmodeID.MultiplayerClient) {
				foreach (NPC other in Main.ActiveNPCs) {
					if (other.whoAmI == npc.whoAmI || other.boss && npc.boss)
						continue;
					ChaosGlobalNPC otherGlobal = other.GetGlobalNPC<ChaosGlobalNPC>();
					if (otherGlobal.GluedToNPC != -1 || GluedToNPC == other.whoAmI || IsInChain(other, npc))
						continue;
					if (!npc.Hitbox.Intersects(other.Hitbox))
						continue;
					otherGlobal.GluedToNPC = npc.whoAmI;
					otherGlobal.GlueOffset = other.position - npc.position;
					other.netUpdate = true;
				}
			}

			if (GluedToNPC >= 0) {
				NPC host = Main.npc[GluedToNPC];
				if (!host.active) {
					GluedToNPC = -1;
				}
				else {
					npc.position = host.position + GlueOffset;
					npc.velocity = Vector2.Zero; // the host does the moving; we just get re-snapped every frame
					if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 20 == 0)
						npc.netUpdate = true;
				}
			}
		}

		private static bool IsInChain(NPC candidateHost, NPC npc) {
			// Walks up from npc to make sure candidateHost isn't already dragging us (no cycles, please).
			int guard = 0;
			int current = npc.GetGlobalNPC<ChaosGlobalNPC>().GluedToNPC;
			while (current >= 0 && guard++ < 200) {
				if (current == candidateHost.whoAmI)
					return true;
				current = Main.npc[current].GetGlobalNPC<ChaosGlobalNPC>().GluedToNPC;
			}
			return false;
		}

		public static void DetachEverythingFrom(NPC host) {
			foreach (NPC other in Main.ActiveNPCs) {
				ChaosGlobalNPC g = other.GetGlobalNPC<ChaosGlobalNPC>();
				if (g.GluedToNPC == host.whoAmI) {
					g.GluedToNPC = -1;
					other.velocity = Main.rand.NextVector2Circular(6f, 6f) - Vector2.UnitY * 4f;
					other.netUpdate = true;
				}
			}
			host.GetGlobalNPC<ChaosGlobalNPC>().GluedToNPC = -1;
		}

		public static bool IsOnFire(NPC npc) {
			return npc.HasBuff(BuffID.OnFire) || npc.HasBuff(BuffID.OnFire3) || npc.HasBuff(BuffID.CursedInferno) || npc.HasBuff(BuffID.ShadowFlame)
				|| npc.HasBuff(BuffID.Burning) || npc.lavaWet;
		}

		// ------------------------------------------------------------------------------------------------------------
		// Bouncing Jelly Storm: no contact damage, everybody bounces off everybody.
		// ------------------------------------------------------------------------------------------------------------
		private void UpdateJellyBounce(NPC npc) {
			foreach (NPC other in Main.ActiveNPCs) {
				if (other.whoAmI <= npc.whoAmI)
					continue; // each pair once
				if (!npc.Hitbox.Intersects(other.Hitbox))
					continue;
				Vector2 delta = other.Center - npc.Center;
				if (delta == Vector2.Zero)
					delta = Vector2.UnitX;
				delta.Normalize();
				float speed = System.Math.Max(5f, (npc.velocity.Length() + other.velocity.Length()) * 0.5f + 1f);
				npc.velocity = -delta * speed;
				other.velocity = delta * speed;
				npc.netUpdate = true;
				other.netUpdate = true;
			}
		}

		public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) {
			if (ChaosEvents.JellyStormActive)
				return false;
			return true;
		}

		// ------------------------------------------------------------------------------------------------------------
		// Life Link: everything on screen with the debuff shares every hit (it is not split, it is copied).
		// ------------------------------------------------------------------------------------------------------------
		private static bool spreadingLifeLink;

		public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone) => SpreadLifeLink(npc, hit, damageDone);

		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) => SpreadLifeLink(npc, hit, damageDone);

		private static void SpreadLifeLink(NPC npc, NPC.HitInfo hit, int damageDone) {
			if (spreadingLifeLink || damageDone <= 0 || !npc.HasBuff<LifeLinkDebuff>())
				return;
			spreadingLifeLink = true;
			try {
				foreach (NPC other in Main.ActiveNPCs) {
					if (other.whoAmI == npc.whoAmI || !other.HasBuff<LifeLinkDebuff>() || !ChaosUtils.OnScreen(other))
						continue;
					other.SimpleStrikeNPC(damageDone, hit.HitDirection, hit.Crit, 0f, hit.DamageType, false, 0f, noPlayerInteraction: true);
				}
			}
			finally {
				spreadingLifeLink = false;
			}
		}

		// ------------------------------------------------------------------------------------------------------------
		// Spawning: Corporate Zombie Invasion and Delirium
		// ------------------------------------------------------------------------------------------------------------
		public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo) {
			if (ChaosEvents.InvasionActive && ChaosEvents.NearSpawn(spawnInfo.Player)) {
				pool.Clear();
				pool[ModContent.NPCType<ZombieSalesman>()] = 1f;
				pool[ModContent.NPCType<ZombieIntern>()] = 0.8f;
				if (!ChaosEvents.CEODefeated && !NPC.AnyNPCs(ModContent.NPCType<ZombieCEO>()) && ChaosEvents.InvasionKills >= ChaosEvents.InvasionKillsForCEO)
					pool[ModContent.NPCType<ZombieCEO>()] = 0.3f;
			}
		}

		public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns) {
			if ((ChaosEvents.DeliriumActive || ChaosEvents.InvasionActive) && ChaosEvents.NearSpawn(player)) {
				spawnRate = System.Math.Max(1, spawnRate / 4);
				maxSpawns *= 2;
			}
		}

		public override void OnKill(NPC npc) {
			if (ChaosEvents.InvasionActive && npc.ModNPC is CorporateZombie)
				ChaosEvents.RegisterInvasionKill(npc);
		}

		// ------------------------------------------------------------------------------------------------------------
		// Drawing: silhouettes, slashed sprites, stretched sprites, googly eyes.
		// ------------------------------------------------------------------------------------------------------------
		public override void DrawEffects(NPC npc, ref Color drawColor) {
			if (Silhouette)
				drawColor = new Color(10, 5, 20, drawColor.A);
		}

		public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (!Slashed && !Stretched)
				return true;
			if (npc.ModNPC != null && npc.ModNPC.Mod != Mod)
				return true; // other mods may draw themselves; don't fight them

			Texture2D texture = TextureAssets.Npc[npc.type].Value;
			Rectangle frame = npc.frame;
			if (frame.Width <= 0 || frame.Height <= 0)
				frame = texture.Frame(1, Main.npcFrameCount[npc.type], 0, 0);

			SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Color color = npc.GetAlpha(drawColor);
			float scaleX = npc.scale * (Stretched ? 4f : 1f);
			float scaleY = npc.scale;
			Vector2 center = npc.Center - screenPos + new Vector2(0f, Main.NPCAddHeight(npc) + npc.gfxOffY);

			if (!Slashed) {
				Main.EntitySpriteDraw(texture, center, frame, color, npc.rotation, frame.Size() * 0.5f, new Vector2(scaleX, scaleY), effects, 0);
				return false;
			}

			int topHeight = frame.Height / 2;
			int bottomHeight = frame.Height - topHeight;
			Rectangle topRect = new Rectangle(frame.X, frame.Y, frame.Width, topHeight);
			Rectangle bottomRect = new Rectangle(frame.X, frame.Y + topHeight, frame.Width, bottomHeight);

			// Legs go on top, head goes on the bottom. Anatomy is a suggestion.
			Vector2 topLeft = center - new Vector2(frame.Width * scaleX, frame.Height * scaleY) * 0.5f;
			Vector2 bottomHalfPos = topLeft;
			Vector2 topHalfPos = topLeft + new Vector2(0f, bottomHeight * scaleY);
			Main.EntitySpriteDraw(texture, bottomHalfPos, bottomRect, color, 0f, Vector2.Zero, new Vector2(scaleX, scaleY), effects, 0);
			Main.EntitySpriteDraw(texture, topHalfPos, topRect, color, 0f, Vector2.Zero, new Vector2(scaleX, scaleY), effects, 0);
			return false;
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (!Googly)
				return;
			Texture2D eye = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/GooglyEye").Value;
			Texture2D pupil = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/GooglyPupil").Value;
			float eyeScale = MathHelper.Clamp(npc.width / 24f, 0.8f, 3f);
			Vector2 head = npc.Top - screenPos + new Vector2(0f, npc.height * 0.22f + npc.gfxOffY);
			float spacing = System.Math.Max(8f, npc.width * 0.22f) * (Stretched ? 3f : 1f);
			Vector2 leftEye = head - new Vector2(spacing, 0f);
			Vector2 rightEye = head + new Vector2(spacing, 0f);
			Vector2 origin = eye.Size() * 0.5f;
			spriteBatch.Draw(eye, leftEye, null, Color.White, 0f, origin, eyeScale, SpriteEffects.None, 0f);
			spriteBatch.Draw(eye, rightEye, null, Color.White, 0f, origin, eyeScale, SpriteEffects.None, 0f);
			Vector2 pupilOrigin = pupil.Size() * 0.5f;
			spriteBatch.Draw(pupil, leftEye + pupilA * eyeScale, null, Color.White, 0f, pupilOrigin, eyeScale, SpriteEffects.None, 0f);
			spriteBatch.Draw(pupil, rightEye + pupilB * eyeScale, null, Color.White, 0f, pupilOrigin, eyeScale, SpriteEffects.None, 0f);
		}

		private void UpdateGooglyEyes(NPC npc) {
			// Pupils slosh around opposite to acceleration, like real googly eyes, then settle at the bottom.
			Vector2 accel = npc.velocity - lastVelocity;
			lastVelocity = npc.velocity;
			const float Limit = 3.5f;
			pupilA += -accel * 1.5f + new Vector2(0f, 0.35f) + Main.rand.NextVector2Circular(0.3f, 0.3f);
			pupilB += -accel * 1.2f + new Vector2(0f, 0.35f) + Main.rand.NextVector2Circular(0.3f, 0.3f);
			if (pupilA.Length() > Limit)
				pupilA = Vector2.Normalize(pupilA) * Limit;
			if (pupilB.Length() > Limit)
				pupilB = Vector2.Normalize(pupilB) * Limit;
		}
	}
}
