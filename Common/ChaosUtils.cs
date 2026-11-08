using BagofChaos.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Common
{
	public static class ChaosUtils
	{
		public static Rectangle ScreenWorldRect => new Rectangle((int)Main.screenPosition.X - 64, (int)Main.screenPosition.Y - 64, Main.screenWidth + 128, Main.screenHeight + 128);

		public static bool OnScreen(Entity entity) => ScreenWorldRect.Intersects(entity.Hitbox);

		/// <summary>True for enemies that are actually fighting the player.</summary>
		public static bool IsHostile(NPC npc) => npc.active && !npc.friendly && !npc.townNPC && npc.lifeMax > 5 && !npc.dontTakeDamage && !NPCID.Sets.ProjectileNPC[npc.type];

		/// <summary>"Non-hostile" for the lawnmower: town folk, critters, anything friendly. Pets are projectiles, so the mower leaves them alone (lucky them).</summary>
		public static bool IsMowable(NPC npc) => npc.active && (npc.townNPC || npc.friendly || npc.CountsAsACritter) && !npc.boss;

		public static void InstantKillNPC(NPC npc) {
			if (!npc.active)
				return;
			npc.life = 0;
			npc.HitEffect();
			npc.checkDead();
			npc.active = false;
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
		}

		public static int SpawnNPCDirect(int type, Vector2 position, int lifeOverride = -1, Vector2 velocity = default) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return -1;
			int index = NPC.NewNPC(new EntitySource_Misc("BagofChaos"), (int)position.X, (int)position.Y, type);
			if (index >= 0 && index < Main.maxNPCs) {
				NPC npc = Main.npc[index];
				if (lifeOverride > 0)
					npc.life = System.Math.Min(lifeOverride, npc.lifeMax);
				if (velocity != default)
					npc.velocity = velocity;
				npc.CanBeReplacedByOtherNPCs = true;
				if (Main.netMode == NetmodeID.Server)
					NetMessage.SendData(MessageID.SyncNPC, number: index);
			}
			return index;
		}

		public static void FartShockwave(Vector2 position, int sourcePlayer, bool affectNPCs, bool affectLocalPlayer) {
			const float Radius = 900f;
			const float Power = 70f; // "cross map levels of knockback"

			if (Main.netMode != NetmodeID.Server) {
				SoundEngine.PlaySound(SoundID.Item16 with { Volume = 1.2f, PitchVariance = 0.3f }, position); // the vanilla fart
				for (int i = 0; i < 40; i++) {
					Dust dust = Dust.NewDustPerfect(position, DustID.Smoke, Main.rand.NextVector2Circular(12f, 12f), 100, new Color(150, 200, 60), 2f);
					dust.noGravity = true;
				}
			}

			if (affectNPCs) {
				foreach (NPC npc in Main.ActiveNPCs) {
					if (npc.dontTakeDamage && npc.boss)
						continue;
					Vector2 delta = npc.Center - position;
					float distance = delta.Length();
					if (distance > Radius)
						continue;
					Vector2 dir = distance < 1f ? -Vector2.UnitY : delta / distance;
					float falloff = 1f - distance / Radius * 0.5f;
					npc.velocity = dir * Power * falloff + new Vector2(0f, -30f); // more upward: into the stratosphere
					npc.netUpdate = true;
					if (Main.netMode != NetmodeID.MultiplayerClient && !npc.friendly) {
						// Does 1 damage. One. That's the joke.
						npc.SimpleStrikeNPC(1, npc.Center.X < position.X ? -1 : 1, noPlayerInteraction: true);
					}
				}
			}

			if (affectLocalPlayer && Main.netMode != NetmodeID.Server && ModContent.GetInstance<ChaosConfig>().FartAffectsPlayers) {
				Player player = Main.LocalPlayer;
				if (player.active && !player.dead) {
					Vector2 delta = player.Center - position;
					float distance = delta.Length();
					if (distance <= Radius) {
						Vector2 dir = distance < 1f ? -Vector2.UnitY : delta / distance;
						if (player.whoAmI == sourcePlayer)
							player.velocity = new Vector2(0f, -7f); // about three tiles. you launched yourself. a little.
						else
							player.velocity = dir * Power * 0.6f + new Vector2(0f, -24f);
						player.fallStart = (int)(player.position.Y / 16f);
						player.noFallDmg = true;
					}
				}
			}
		}

		public const float SuccStrength = 24f;

		public static void PushNPC(NPC npc, Vector2 impulse) {
			npc.velocity += impulse;
			npc.netUpdate = true;
		}

		/// <summary>
		/// One Succ. click: a constant-strength impulse toward the user. Far targets are lobbed (aimed above the user) so gravity
		/// and ground friction don't stop them halfway; the strength is the same at any distance.
		/// </summary>
		public static Vector2 SuccImpulse(Vector2 from, Vector2 to) {
			Vector2 delta = to - from;
			Vector2 aim = delta + new Vector2(0f, -System.Math.Abs(delta.X) * 0.35f);
			return aim.SafeNormalize(-Vector2.UnitY) * SuccStrength;
		}

		// Succ.: the local player gets shoved toward the user if they are on the user's screen.
		public static void SuccLocalPlayer(int sourcePlayer, Vector2 position) {
			Player player = Main.LocalPlayer;
			if (player.whoAmI == sourcePlayer || player.dead || !player.active)
				return;
			if (Vector2.Distance(player.Center, position) > 1400f)
				return;
			player.velocity += SuccImpulse(player.Center, position);
			player.fallStart = (int)(player.position.Y / 16f);
			SoundEngine.PlaySound(SoundID.Item24, player.Center);
		}

		// Team Tether: pulled toward the tether owner. Healing is done by the owner (SpiritHeal).
		public static void TetherPullLocalPlayer(int sourcePlayer) {
			Player player = Main.LocalPlayer;
			Player source = Main.player[sourcePlayer];
			if (!source.active || player.dead)
				return;
			Vector2 dir = (source.Center - player.Center).SafeNormalize(Vector2.Zero);
			player.velocity = dir * 18f;
			player.fallStart = (int)(player.position.Y / 16f);
			SoundEngine.PlaySound(SoundID.Item9, player.Center);
		}

		/// <summary>Draws a solid-colour silhouette of the NPC at four offsets, producing an outline under the normal sprite.</summary>
		public static void DrawOutline(NPC npc, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch, Vector2 screenPos, Color color) {
			if (npc.ModNPC != null && npc.ModNPC.Mod != BagofChaos.Instance)
				return;
			var texture = Terraria.GameContent.TextureAssets.Npc[npc.type].Value;
			Rectangle frame = npc.frame;
			if (frame.Width <= 0 || frame.Height <= 0)
				frame = texture.Frame(1, Main.npcFrameCount[npc.type], 0, 0);
			var effects = npc.spriteDirection == 1 ? Microsoft.Xna.Framework.Graphics.SpriteEffects.FlipHorizontally : Microsoft.Xna.Framework.Graphics.SpriteEffects.None;
			Vector2 center = npc.Center - screenPos + new Vector2(0f, Main.NPCAddHeight(npc) + npc.gfxOffY);
			int shader = ContentSamples.CommonlyUsedContentSamples.ColorOnlyShaderIndex;

			spriteBatch.End();
			spriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Immediate, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, Main.DefaultSamplerState, Microsoft.Xna.Framework.Graphics.DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
			foreach (Vector2 offset in new[] { new Vector2(2f, 0f), new Vector2(-2f, 0f), new Vector2(0f, 2f), new Vector2(0f, -2f) }) {
				var data = new DrawData(texture, center + offset, frame, color, npc.rotation, frame.Size() * 0.5f, npc.scale, effects) { shader = shader };
				Terraria.Graphics.Shaders.GameShaders.Armor.Apply(shader, npc, data);
				data.Draw(spriteBatch);
			}
			spriteBatch.End();
			spriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend, Main.DefaultSamplerState, Microsoft.Xna.Framework.Graphics.DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
		}

		public static void HealNPC(NPC npc, int amount) {
			if (amount <= 0 || npc.life >= npc.lifeMax)
				return;
			npc.life = System.Math.Min(npc.lifeMax, npc.life + amount);
			if (Main.netMode != NetmodeID.Server)
				CombatText.NewText(npc.Hitbox, CombatText.HealLife, amount);
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
		}

		public static void HealPlayer(Player player, int amount) {
			if (amount <= 0)
				return;
			if (player.whoAmI == Main.myPlayer) {
				player.Heal(amount);
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient) {
				NetMessage.SendData(MessageID.SpiritHeal, number: player.whoAmI, number2: amount);
			}
		}
	}
}
