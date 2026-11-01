using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace BagofChaos.Content.NPCs
{
	// Buckets that hop around aimlessly near their liquid. Uses the vanilla bucket item sprites.
	public abstract class HoppingBucket : ModNPC
	{
		protected abstract int BucketItem { get; }
		protected abstract int LiquidType { get; } // -1 for shimmer
		protected virtual int Damage => 15;

		public override string Texture => "Terraria/Images/Item_" + BucketItem;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 1;
		}

		public override void SetDefaults() {
			NPC.width = 20;
			NPC.height = 20;
			NPC.damage = Damage;
			NPC.defense = 6;
			NPC.lifeMax = 60;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.Shatter;
			NPC.value = 30f;
			NPC.knockBackResist = 0.6f;
			NPC.aiStyle = NPCAIStyleID.Slime;
			AIType = NPCID.BlueSlime;
		}

		public override void FindFrame(int frameHeight) {
			NPC.frame = new Rectangle(0, 0, Terraria.GameContent.TextureAssets.Npc[Type].Width(), frameHeight);
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo) {
			// Only near the right liquid.
			int x0 = spawnInfo.SpawnTileX, y0 = spawnInfo.SpawnTileY;
			for (int x = x0 - 8; x <= x0 + 8; x++) {
				for (int y = y0 - 8; y <= y0 + 8; y++) {
					if (!WorldGen.InWorld(x, y, 5))
						continue;
					Tile tile = Main.tile[x, y];
					if (tile.LiquidAmount == 0)
						continue;
					if ((LiquidType == -1 && tile.LiquidType == LiquidID.Shimmer) || tile.LiquidType == LiquidType)
						return 0.08f;
				}
			}
			return 0f;
		}

		public override void AI() {
			NPC.rotation = NPC.velocity.X * 0.05f;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(ItemID.EmptyBucket, 3));
		}
	}

	public class HoppingLavaBucket : HoppingBucket
	{
		protected override int BucketItem => ItemID.LavaBucket;
		protected override int LiquidType => LiquidID.Lava;
		public override void SetDefaults() { base.SetDefaults(); NPC.lavaImmune = true; }
		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) => target.AddBuff(BuffID.OnFire, 60 * 5);
		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.HoppingLavaBucket"));
	}

	public class DrippyWaterBucket : HoppingBucket
	{
		protected override int BucketItem => ItemID.WaterBucket;
		protected override int LiquidType => LiquidID.Water;
		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) => target.AddBuff(BuffID.Wet, 60 * 5);
		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.DrippyWaterBucket"));
	}

	public class BouncingHoneyBucket : HoppingBucket
	{
		protected override int BucketItem => ItemID.HoneyBucket;
		protected override int LiquidType => LiquidID.Honey;
		protected override int Damage => 1;
		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
			target.AddBuff(BuffID.Honey, 60 * 5);
			if (target.whoAmI == Main.myPlayer)
				target.Heal(5);
		}
		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.BouncingHoneyBucket"));
	}

	public class ChaoticShimmerBucket : HoppingBucket
	{
		protected override int BucketItem => ItemID.BottomlessShimmerBucket;
		protected override int LiquidType => -1;
		protected override int Damage => 0;
		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

		public override void AI() {
			base.AI();
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;
			foreach (NPC other in Main.ActiveNPCs) {
				if (other.whoAmI == NPC.whoAmI || other.type == Type || other.boss || !other.Hitbox.Intersects(NPC.Hitbox))
					continue;
				int into = NPCID.Sets.ShimmerTransformToNPC[other.type];
				if (into >= 0) {
					other.Transform(into);
					for (int i = 0; i < 10; i++)
						Dust.NewDust(other.position, other.width, other.height, DustID.ShimmerSpark);
				}
			}
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) => bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.BagofChaos.Bestiary.ChaoticShimmerBucket"));
	}
}
