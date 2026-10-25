using BagofChaos.Common.Systems;
using BagofChaos.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Content.NPCs
{
	// Shared base for the Corporate Zombie Invasion. They only spawn during the invasion (see ChaosGlobalNPC.EditSpawnPool).
	public abstract class CorporateZombie : ModNPC
	{
		public override float SpawnChance(NPCSpawnInfo spawnInfo) => 0f;

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.Common(ItemID.SilverCoin, 1, 5, 20)); // severance package
			npcLoot.Add(ItemDropRule.Common(ItemID.Shackle, 50));
		}
	}

	// Replaces every item icon in your UI with a walking zombie while he's around. Also hits you with the 3D debuff. He sells TVs.
	public class ZombieSalesman : CorporateZombie
	{
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Velocity = 1f });
		}

		public override void SetDefaults() {
			NPC.width = 22;
			NPC.height = 44;
			NPC.damage = 18;
			NPC.defense = 8;
			NPC.lifeMax = 90;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath2;
			NPC.value = 120f;
			NPC.knockBackResist = 0.5f;
			NPC.aiStyle = NPCAIStyleID.Fighter;
			AIType = NPCID.Zombie;
		}

		public override void FindFrame(int frameHeight) {
			NPC.frameCounter += System.Math.Abs(NPC.velocity.X) > 0.1f ? 1 : 0.3;
			NPC.frame.Y = ((int)(NPC.frameCounter / 7) % 4) * frameHeight;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
			target.AddBuff(ModContent.BuffType<ThreeDDebuff>(), 60 * 12);
		}

		public override void AI() {
			if (Main.rand.NextBool(600) && Main.netMode != NetmodeID.Server)
				CombatText.NewText(NPC.Hitbox, Color.LightGreen, Language.GetTextValue("Mods.BagofChaos.NPCs.ZombieSalesman.Pitch" + Main.rand.Next(4)), false, true);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.ZombieSalesman"));
		}
	}

	// Sits around and does nothing. Counts toward the invasion. Someone has to.
	public class ZombieIntern : CorporateZombie
	{
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 2;
		}

		public override void SetDefaults() {
			NPC.width = 22;
			NPC.height = 40;
			NPC.damage = 0;
			NPC.defense = 2;
			NPC.lifeMax = 40;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath2;
			NPC.value = 5f;
			NPC.knockBackResist = 0.8f;
			NPC.aiStyle = -1;
		}

		public override void AI() {
			NPC.velocity.X *= 0.8f;
			NPC.TargetClosest();
			if (Main.rand.NextBool(900) && Main.netMode != NetmodeID.Server)
				CombatText.NewText(NPC.Hitbox, Color.Gray, Language.GetTextValue("Mods.BagofChaos.NPCs.ZombieIntern.Idle" + Main.rand.Next(4)), false, true);
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

		public override void FindFrame(int frameHeight) {
			NPC.frameCounter++;
			NPC.frame.Y = ((int)(NPC.frameCounter / 40) % 2) * frameHeight;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.ZombieIntern"));
		}
	}

	// Tanky, loud, obnoxious. +100% damage for every other zombie on screen. Synergy.
	[AutoloadBossHead]
	public class ZombieCEO : CorporateZombie
	{
		private int shoutTimer;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 4;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Velocity = 1f, Scale = 0.8f });
		}

		public override void SetDefaults() {
			NPC.width = 44;
			NPC.height = 88;
			NPC.damage = 30;
			NPC.defense = 20;
			NPC.lifeMax = 6000;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath2;
			NPC.value = Item.buyPrice(gold: 10);
			NPC.knockBackResist = 0f;
			NPC.boss = true;
			NPC.npcSlots = 10f;
			NPC.aiStyle = NPCAIStyleID.Fighter;
			AIType = NPCID.Zombie;
			if (!Main.dedServ)
				Music = MusicID.Boss1;
		}

		public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment) {
			NPC.lifeMax = (int)(NPC.lifeMax * balance * bossAdjustment);
		}

		public override void AI() {
			// Loud and obnoxious.
			shoutTimer++;
			if (shoutTimer >= 240) {
				shoutTimer = 0;
				if (Main.netMode != NetmodeID.Server) {
					SoundEngine.PlaySound(new SoundStyle($"{BagofChaos.AssetPath}Sounds/CEOShout") { Volume = 1.3f, PitchVariance = 0.4f }, NPC.Center);
					CombatText.NewText(NPC.Hitbox, Color.Gold, Language.GetTextValue("Mods.BagofChaos.NPCs.ZombieCEO.Shout" + Main.rand.Next(6)), true, false);
					if (Vector2.Distance(Main.LocalPlayer.Center, NPC.Center) < 1200f)
						Main.instance.CameraModifiers.Add(new Terraria.Graphics.CameraModifiers.PunchCameraModifier(NPC.Center, Main.rand.NextVector2Unit(), 8f, 6f, 20, 1000f, "ZombieCEO"));
				}
			}

			// Synergy: +100% damage for each other zombie on screen.
			int zombies = 0;
			foreach (NPC other in Main.ActiveNPCs) {
				if (other.whoAmI != NPC.whoAmI && other.ModNPC is CorporateZombie && Common.ChaosUtils.OnScreen(other))
					zombies++;
			}
			NPC.damage = (int)(30 * (1f + zombies));

			// Despawn if the invasion ended without him (he got a golden parachute).
			if (!ChaosEvents.InvasionActive && NPC.timeLeft > 60)
				NPC.timeLeft = 60;
		}

		public override void FindFrame(int frameHeight) {
			NPC.frameCounter += System.Math.Abs(NPC.velocity.X) > 0.1f ? 1 : 0.3;
			NPC.frame.Y = ((int)(NPC.frameCounter / 8) % 4) * frameHeight;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 20, 50));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.CreditCannon>(), 1));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Consumables.PinkSlip>(), 3));
			npcLoot.Add(ItemDropRule.Common(ItemID.Toilet, 1)); // the executive washroom
		}

		public override void OnKill() {
			if (Main.netMode != NetmodeID.MultiplayerClient)
				ChaosEvents.Announce("CEODown", Color.Gold);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.ZombieCEO"));
		}
	}
}
