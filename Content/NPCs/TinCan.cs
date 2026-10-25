using BagofChaos.Content.Projectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.NPCs
{
	// Chain Can summons these next to whatever it hits. Shoot one and it goes off.
	public class TinCan : ModNPC
	{
		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 2;
			NPCID.Sets.ImmuneToAllBuffs[Type] = true;
			NPCID.Sets.CantTakeLunchMoney[Type] = true;
			NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
		}

		public override void SetDefaults() {
			NPC.width = 14;
			NPC.height = 22;
			NPC.damage = 0;
			NPC.defense = 0;
			NPC.lifeMax = 1;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.Item14;
			NPC.knockBackResist = 0.9f;
			NPC.aiStyle = -1;
			NPC.noGravity = false;
			NPC.value = 0;
			NPC.npcSlots = 0.1f;
			NPC.friendly = false;
			NPC.dontCountMe = true;
			NPC.timeLeft = NPC.activeTime;
		}

		public override void AI() {
			// Just sit there, wobble a bit, and get shot.
			NPC.velocity.X *= 0.9f;
			NPC.rotation = NPC.velocity.X * 0.1f;
			NPC.ai[0]++;
			if (NPC.ai[0] > 60 * 45)
				NPC.StrikeInstantKill(); // old cans get recycled
		}

		public override void FindFrame(int frameHeight) {
			NPC.frameCounter++;
			NPC.frame.Y = ((int)(NPC.frameCounter / 20) % 2) * frameHeight;
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

		public override bool? CanBeHitByItem(Player player, Item item) => true;

		public override bool? CanBeHitByProjectile(Projectile projectile) => projectile.type != ModContent.ProjectileType<CanExplosion>();

		public override void OnKill() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			int damage = 70;
			if (NPC.lastInteraction >= 0 && NPC.lastInteraction < Main.maxPlayers) {
				Player player = Main.player[NPC.lastInteraction];
				damage = (int)player.GetTotalDamage(DamageClass.Ranged).ApplyTo(70);
				Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CanExplosion>(), damage, 8f, player.whoAmI);
			}
			else {
				Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CanExplosion>(), damage, 8f, Main.myPlayer);
			}
		}

		public override void HitEffect(NPC.HitInfo hit) {
			if (Main.netMode == NetmodeID.Server)
				return;
			for (int i = 0; i < 12; i++) {
				Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Iron, 0f, -2f);
				dust.velocity *= 2f;
			}
			if (NPC.life <= 0)
				SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.TinCan"));
		}
	}
}
