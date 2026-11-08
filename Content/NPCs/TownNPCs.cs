using System;
using System.Collections.Generic;
using System.Linq;
using BagofChaos.Common.Players;
using BagofChaos.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Content.NPCs
{
	public abstract class ChaosTownNPC : ModNPC
	{
		protected abstract int VanillaLook { get; }
		protected virtual Color Tint => Color.White;
		protected abstract string FixedName { get; }

		public override string Texture => "Terraria/Images/NPC_" + VanillaLook;
		public override string HeadTexture => "BagofChaos/Content/NPCs/" + Name + "_Head";

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = Main.npcFrameCount[VanillaLook];
			NPCID.Sets.ExtraFramesCount[Type] = NPCID.Sets.ExtraFramesCount[VanillaLook];
			NPCID.Sets.AttackFrameCount[Type] = NPCID.Sets.AttackFrameCount[VanillaLook];
			NPCID.Sets.DangerDetectRange[Type] = 500;
			NPCID.Sets.AttackType[Type] = 0;
			NPCID.Sets.AttackTime[Type] = 90;
			NPCID.Sets.AttackAverageChance[Type] = 30;
			NPCID.Sets.HatOffsetY[Type] = 4;
			NPCID.Sets.NoTownNPCHappiness[Type] = true;
		}

		public override void SetDefaults() {
			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = NPCAIStyleID.Passive;
			NPC.damage = 10;
			NPC.defense = 15;
			NPC.lifeMax = 250;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;
			NPC.color = Tint;
			AnimationType = VanillaLook;
		}

		public override List<string> SetNPCNameList() => new() { FixedName };

		public override void TownNPCAttackStrength(ref int damage, ref float knockback) { damage = 15; knockback = 3f; }
		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) { cooldown = 30; randExtraCooldown = 30; }
		public override void TownNPCAttackProj(ref int projType, ref int attackDelay) { projType = ProjectileID.ThrowingKnife; attackDelay = 1; }
		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset) { multiplier = 12f; randomOffset = 2f; }
	}

	// Gremlot: sells 10 random items from your town's other shops at half price. New stock every midnight. Needs the Dryad.
	[AutoloadHead]
	public class Gremlot : ChaosTownNPC
	{
		public const string ShopName = "Shop";
		protected override int VanillaLook => NPCID.GoblinTinkerer;
		protected override Color Tint => new Color(170, 255, 170);
		protected override string FixedName => "Gremlot";

		public override bool CanTownNPCSpawn(int numTownNPCs) => NPC.AnyNPCs(NPCID.Dryad);

		public override string GetChat() => Language.GetTextValue("Mods.BagofChaos.NPCs.Gremlot.Chat" + Main.rand.Next(3));

		public override void SetChatButtons(ref string button, ref string button2) => button = Language.GetTextValue("LegacyInterface.28");

		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			if (firstButton)
				shop = ShopName;
		}

		public override void AddShops() {
			new NPCShop(Type, ShopName).Register(); // filled dynamically below
		}

		public override void ModifyActiveShop(string shopName, Item[] items) {
			// Pool: every entry of every shop belonging to a town NPC that currently lives here (other than Gremlot).
			var pool = new List<int>();
			foreach (AbstractNPCShop shop in NPCShopDatabase.AllShops) {
				if (shop.NpcType == Type || !NPC.AnyNPCs(shop.NpcType))
					continue;
				foreach (AbstractNPCShop.Entry entry in shop.ActiveEntries) {
					if (entry.Item != null && !entry.Item.IsAir && !pool.Contains(entry.Item.type))
						pool.Add(entry.Item.type);
				}
			}
			for (int i = 0; i < items.Length; i++)
				items[i] = new Item();
			if (pool.Count == 0)
				return;
			// Deterministic per day so the stock only changes at midnight.
			var rand = new Random(Main.worldID * 31 + ChaosEvents.DayCount);
			for (int i = 0; i < 10 && pool.Count > 0; i++) {
				int type = pool[rand.Next(pool.Count)];
				pool.Remove(type);
				Item item = new Item(type);
				item.shopCustomPrice = Math.Max(1, item.value / 2);
				items[i] = item;
			}
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.Gremlot"));
		}
	}

	// Rat Merchant: sells whatever you threw in the trash lately. One gold each. Needs 20 different trashed items.
	[AutoloadHead]
	public class RatMerchant : ChaosTownNPC
	{
		public const string ShopName = "Shop";
		protected override int VanillaLook => NPCID.Merchant;
		protected override Color Tint => new Color(200, 180, 170);
		protected override string FixedName => "Rat Merchant";

		public override bool CanTownNPCSpawn(int numTownNPCs) {
			foreach (Player player in Main.ActivePlayers) {
				if (player.GetModPlayer<ChaosPlayer>().TrashedTypes.Count >= 20)
					return true;
			}
			return false;
		}

		public override string GetChat() => Language.GetTextValue("Mods.BagofChaos.NPCs.RatMerchant.Chat" + Main.rand.Next(3));

		public override void SetChatButtons(ref string button, ref string button2) => button = Language.GetTextValue("LegacyInterface.28");

		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			if (firstButton)
				shop = ShopName;
		}

		public override void AddShops() {
			new NPCShop(Type, ShopName).Register();
		}

		public override void ModifyActiveShop(string shopName, Item[] items) {
			for (int i = 0; i < items.Length; i++)
				items[i] = new Item();
			List<int> trashed = Main.LocalPlayer.GetModPlayer<ChaosPlayer>().TrashedTypes;
			for (int i = 0; i < trashed.Count && i < items.Length; i++) {
				Item item = new Item(trashed[trashed.Count - 1 - i]);
				item.shopCustomPrice = Item.buyPrice(gold: 1);
				items[i] = item;
			}
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.RatMerchant"));
		}
	}

	// Chaotic Beholder: sells everything with "Chaos" in the name once the Chaotic Slime is dead. Talking to him is a gamble.
	[AutoloadHead]
	public class ChaoticBeholder : ChaosTownNPC
	{
		public const string ShopName = "Shop";
		protected override int VanillaLook => NPCID.Wizard;
		protected override Color Tint => new Color(255, 170, 255);
		protected override string FixedName => "Chaotic Beholder";

		public override bool CanTownNPCSpawn(int numTownNPCs) => Main.hardMode && ChaosEvents.DownedChaoticSlime;

		public override string GetChat() {
			if (Main.rand.NextBool(20) && Main.netMode != NetmodeID.Server) {
				int[] slimes = { NPCID.BlueSlime, NPCID.GreenSlime, NPCID.PurpleSlime, NPCID.RedSlime, NPCID.YellowSlime, NPCID.MotherSlime, NPCID.IceSlime, NPCID.JungleSlime, NPCID.LavaSlime };
				BagofChaos.SendSpawnNPC(slimes[Main.rand.Next(slimes.Length)], NPC.Center + new Vector2(NPC.direction * 40f, -20f));
				return Language.GetTextValue("Mods.BagofChaos.NPCs.ChaoticBeholder.Slime");
			}
			return Language.GetTextValue("Mods.BagofChaos.NPCs.ChaoticBeholder.Chat" + Main.rand.Next(3));
		}

		public override void SetChatButtons(ref string button, ref string button2) => button = Language.GetTextValue("LegacyInterface.28");

		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			if (firstButton)
				shop = ShopName;
		}

		public override void AddShops() {
			var shop = new NPCShop(Type, ShopName);
			foreach (ModItem item in Mod.GetContent<ModItem>()) {
				string name = item.DisplayName.Value;
				if (!name.Contains("Chaos", StringComparison.OrdinalIgnoreCase) && !name.Contains("Chaotic", StringComparison.OrdinalIgnoreCase))
					continue;
				// "Only after you could normally obtain them": post-Moon Lord things wait for the Moon Lord.
				Condition condition = item.Item.rare >= ItemRarityID.Red ? Condition.DownedMoonLord : Condition.Hardmode;
				shop.Add(item.Type, condition);
			}
			shop.Register();
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.ChaoticBeholder"));
		}
	}

	// Shlue: a town pet made of two glue slimes stacked on top of each other (slightly off-centre). Only the top one got
	// googly eyes. Shows up after 50 glue enemies have died. Drawn from the vanilla slime sprite, glue-white.
	[AutoloadHead]
	public class Shlue : ModNPC
	{
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 2;
			NPCID.Sets.IsTownPet[Type] = true;
			NPCID.Sets.IsPetSmallForPetting[Type] = true;
			NPCID.Sets.ExtraFramesCount[Type] = 0;
			NPCID.Sets.AttackFrameCount[Type] = 0;
			NPCID.Sets.DangerDetectRange[Type] = 250;
			NPCID.Sets.AttackType[Type] = -1;
			NPCID.Sets.AttackTime[Type] = -1;
			NPCID.Sets.AttackAverageChance[Type] = 1;
			NPCID.Sets.HatOffsetY[Type] = 0;
			NPCID.Sets.NoTownNPCHappiness[Type] = true;
		}

		public override void SetDefaults() {
			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 30;
			NPC.height = 44;
			NPC.aiStyle = NPCAIStyleID.Passive;
			NPC.damage = 0;
			NPC.defense = 10;
			NPC.lifeMax = 250;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;
		}

		public override List<string> SetNPCNameList() => new() { "Shlue" };

		public override bool CanTownNPCSpawn(int numTownNPCs) => ChaosEvents.GlueKills >= 50;

		public const int ChatLines = 14;
		private static int lastChat = -1;

		// Glue jokes, slime puns and nonsense, never the same line twice in a row.
		public override string GetChat() {
			int line = Main.rand.Next(ChatLines);
			if (line == lastChat)
				line = (line + 1 + Main.rand.Next(ChatLines - 1)) % ChatLines;
			lastChat = line;
			return Language.GetTextValue("Mods.BagofChaos.NPCs.Shlue.Chat" + line);
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Main.instance.LoadNPC(NPCID.BlueSlime);
			Texture2D slime = TextureAssets.Npc[NPCID.BlueSlime].Value;
			int frames = Main.npcFrameCount[NPCID.BlueSlime];
			int frameIndex = (int)(NPC.frameCounter / 12 % frames);
			Rectangle frame = slime.Frame(1, frames, 0, frameIndex);
			Color glue = drawColor.MultiplyRGB(new Color(250, 250, 240)) * 0.85f;
			SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			Vector2 bottom = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 2f);
			// Bottom slime, then the top one sitting on it a little to one side.
			spriteBatch.Draw(slime, bottom, frame, glue, 0f, new Vector2(frame.Width / 2f, frame.Height), 1f, effects, 0f);
			Vector2 topBottom = bottom + new Vector2(NPC.direction * 4f, -frame.Height * 0.62f);
			spriteBatch.Draw(slime, topBottom, frame, glue, 0f, new Vector2(frame.Width / 2f, frame.Height), 0.95f, effects, 0f);
			// Googly eyes on the top slime only. They look at the nearest player.
			Texture2D eye = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/GooglyEye").Value;
			Texture2D pupil = ModContent.Request<Texture2D>($"{BagofChaos.AssetPath}Textures/GooglyPupil").Value;
			Vector2 face = topBottom - new Vector2(0f, frame.Height * 0.45f);
			Player look = Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
			Vector2 gaze = (look.Center - NPC.Center).SafeNormalize(Vector2.Zero) * 1.5f;
			foreach (float dx in new[] { -5f, 5f }) {
				Vector2 at = face + new Vector2(dx, 0f);
				spriteBatch.Draw(eye, at, null, Color.White, 0f, eye.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);
				spriteBatch.Draw(pupil, at + gaze, null, Color.White, 0f, pupil.Size() * 0.5f, 0.8f, SpriteEffects.None, 0f);
			}
			return false;
		}

		public override void FindFrame(int frameHeight) {
			NPC.frameCounter++;
			NPC.frame.Y = ((int)(NPC.frameCounter / 20) % 2) * frameHeight;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.Shlue"));
		}
	}
}
