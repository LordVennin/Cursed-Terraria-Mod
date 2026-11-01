using BagofChaos.Common;
using BagofChaos.Common.Systems;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace BagofChaos.Content.NPCs.Worms
{
	// A vanilla Digger, slightly faster, white, and sticky.
	public class GlueDiggerHead : WormHead
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.DiggerHead;
		public override int BodyType => ModContent.NPCType<GlueDiggerBody>();
		public override int TailType => ModContent.NPCType<GlueDiggerTail>();

		public override void SetStaticDefaults() {
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { CustomTexturePath = "Terraria/Images/NPC_" + NPCID.DiggerHead, Position = new Vector2(40f, 24f), PortraitPositionXOverride = 0f, PortraitPositionYOverride = 12f });
		}

		public override void SetDefaults() {
			NPC.CloneDefaults(NPCID.DiggerHead);
			NPC.aiStyle = -1;
			NPC.lifeMax = 180;
			NPC.damage = 22;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) => SpawnCondition.Cavern.Chance * 0.06f;

		public override void Init() {
			MinSegmentLength = 6;
			MaxSegmentLength = 10;
			CommonWormInit(this);
		}

		internal static void CommonWormInit(Worm worm) {
			worm.MoveSpeed = 6.5f;   // vanilla digger: 5.5
			worm.Acceleration = 0.08f;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) => target.AddBuff(ModContent.BuffType<GlueDebuff>(), 60 * 5);

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			GlueDiggerBody.DrawWhite(NPC, Texture, spriteBatch, screenPos, drawColor);
			return false;
		}

		public override void OnKill() => ChaosEvents.RegisterGlueKill();

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange([BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns, new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.GlueDigger")]);
		}
	}

	public class GlueDiggerBody : WormBody
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.DiggerBody;

		public override void SetStaticDefaults() {
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
		}

		public override void SetDefaults() {
			NPC.CloneDefaults(NPCID.DiggerBody);
			NPC.aiStyle = -1;
		}

		public override void Init() => GlueDiggerHead.CommonWormInit(this);

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) => target.AddBuff(ModContent.BuffType<GlueDebuff>(), 60 * 5);

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			DrawWhite(NPC, Texture, spriteBatch, screenPos, drawColor);
			return false;
		}

		internal static void DrawWhite(NPC npc, string texturePath, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D texture = WhitenedTextures.Get(texturePath);
			Vector2 center = npc.Center - screenPos;
			spriteBatch.Draw(texture, center, null, npc.GetAlpha(drawColor), npc.rotation, texture.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
		}
	}

	public class GlueDiggerTail : WormTail
	{
		public override string Texture => "Terraria/Images/NPC_" + NPCID.DiggerTail;

		public override void SetStaticDefaults() {
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
		}

		public override void SetDefaults() {
			NPC.CloneDefaults(NPCID.DiggerTail);
			NPC.aiStyle = -1;
		}

		public override void Init() => GlueDiggerHead.CommonWormInit(this);

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) => target.AddBuff(ModContent.BuffType<GlueDebuff>(), 60 * 5);

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			GlueDiggerBody.DrawWhite(NPC, Texture, spriteBatch, screenPos, drawColor);
			return false;
		}
	}
}
