using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace BagofChaos.Content.Projectiles
{
	// ai[0]: 1 = hardmode pool, ai[1]: 1 = pooled from the thrower's biome.
	public class BottledEnemiesProjectile : ModProjectile
	{
		public override string Texture => "Terraria/Images/Item_" + ItemID.Bottle;

		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 20;
			Projectile.friendly = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 300;
			Projectile.aiStyle = -1;
			Projectile.tileCollide = true;
		}

		public override void AI() {
			Projectile.velocity.Y += 0.3f;
			Projectile.rotation += 0.2f * Projectile.direction;
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			Projectile.Kill();
			return false;
		}

		public override void OnKill(int timeLeft) {
			SoundEngine.PlaySound(SoundID.Shatter, Projectile.Center);
			for (int i = 0; i < 10; i++)
				Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Glass);
			if (Projectile.owner != Main.myPlayer)
				return;
			Player player = Main.player[Projectile.owner];
			bool hardmode = Projectile.ai[0] == 1f;
			bool biomed = Projectile.ai[1] == 1f;

			// 0.1%: a boss you have already beaten shows up instead.
			if (Main.rand.NextBool(1000)) {
				List<int> bosses = EnemyPools.DownedBosses(hardmode);
				if (bosses.Count > 0) {
					BagofChaos.SendSpawnBoss(player.whoAmI, bosses[Main.rand.Next(bosses.Count)]);
					return;
				}
			}

			List<int> pool = biomed ? EnemyPools.BiomePool(player, hardmode) : EnemyPools.GenericPool(hardmode);
			if (pool.Count == 0)
				return;
			for (int i = 0; i < 10; i++) {
				int type = pool[Main.rand.Next(pool.Count)];
				Vector2 pos = Projectile.Center + new Vector2(Main.rand.NextFloat(-40f, 40f), -Main.rand.NextFloat(0f, 40f));
				BagofChaos.SendSpawnNPC(type, pos);
			}
		}
	}

	public static class EnemyPools
	{
		private static readonly int[] PreHardmode = {
			NPCID.BlueSlime, NPCID.GreenSlime, NPCID.Zombie, NPCID.DemonEye, NPCID.EaterofSouls, NPCID.Crimera, NPCID.FaceMonster, NPCID.Skeleton,
			NPCID.CaveBat, NPCID.GiantWormHead, NPCID.Antlion, NPCID.Vulture, NPCID.JungleBat, NPCID.Piranha, NPCID.Hornet, NPCID.ManEater,
			NPCID.Harpy, NPCID.IceSlime, NPCID.SnowFlinx, NPCID.UndeadViking, NPCID.Demon, NPCID.FireImp, NPCID.LavaSlime, NPCID.Bunny,
			NPCID.GoblinScout, NPCID.MotherSlime, NPCID.Crab, NPCID.Mummy, NPCID.DungeonSlime, NPCID.CursedSkull,
		};

		private static readonly int[] Hardmode = {
			NPCID.Wraith, NPCID.Werewolf, NPCID.PossessedArmor, NPCID.Pixie, NPCID.Unicorn, NPCID.Gastropod, NPCID.Corruptor, NPCID.Herpling,
			NPCID.IchorSticker, NPCID.FloatyGross, NPCID.ArmoredSkeleton, NPCID.GiantBat, NPCID.ToxicSludge, NPCID.Mimic, NPCID.IceElemental,
			NPCID.Wolf, NPCID.GiantTortoise, NPCID.Moth, NPCID.AngryTrapper, NPCID.DesertGhoul, NPCID.DuneSplicerHead, NPCID.Clown,
			NPCID.ChaosElemental, NPCID.Derpling, NPCID.RedDevil, NPCID.Lavabat, NPCID.BlackRecluse, NPCID.AnglerFish, NPCID.IlluminantBat,
		};

		public static List<int> GenericPool(bool hardmode) => new(hardmode ? Hardmode : PreHardmode);

		public static List<int> BiomePool(Player player, bool hardmode) {
			var pool = new List<int>();
			if (player.ZoneDesert || player.ZoneUndergroundDesert) pool.AddRange(hardmode ? new int[] { NPCID.DesertGhoul, NPCID.DuneSplicerHead, NPCID.Mummy, NPCID.DesertBeast } : new int[] { NPCID.Antlion, NPCID.Vulture, NPCID.SandSlime, NPCID.TombCrawlerHead });
			else if (player.ZoneSnow) pool.AddRange(hardmode ? new int[] { NPCID.IceElemental, NPCID.Wolf, NPCID.IcyMerman, NPCID.IceGolem } : new int[] { NPCID.IceSlime, NPCID.SnowFlinx, NPCID.UndeadViking, NPCID.IceBat });
			else if (player.ZoneJungle) pool.AddRange(hardmode ? new int[] { NPCID.Moth, NPCID.AngryTrapper, NPCID.GiantTortoise, NPCID.Derpling } : new int[] { NPCID.JungleBat, NPCID.Piranha, NPCID.Hornet, NPCID.ManEater, NPCID.JungleSlime });
			else if (player.ZoneCorrupt) pool.AddRange(hardmode ? new int[] { NPCID.Corruptor, NPCID.CursedHammer, NPCID.Clinger, NPCID.CorruptSlime } : new int[] { NPCID.EaterofSouls, NPCID.DevourerHead, NPCID.Corruptor });
			else if (player.ZoneCrimson) pool.AddRange(hardmode ? new int[] { NPCID.Herpling, NPCID.IchorSticker, NPCID.FloatyGross, NPCID.CrimsonAxe } : new int[] { NPCID.Crimera, NPCID.FaceMonster, NPCID.BloodCrawler });
			else if (player.ZoneHallow) pool.AddRange(new int[] { NPCID.Pixie, NPCID.Unicorn, NPCID.Gastropod, NPCID.ChaosElemental, NPCID.IlluminantBat });
			else if (player.ZoneDungeon) pool.AddRange(hardmode ? new int[] { NPCID.BlueArmoredBones, NPCID.Necromancer, NPCID.Paladin, NPCID.DiabolistRed } : new int[] { NPCID.AngryBones, NPCID.DarkCaster, NPCID.CursedSkull, NPCID.DungeonSlime });
			else if (player.ZoneBeach) pool.AddRange(hardmode ? new int[] { NPCID.AnglerFish, NPCID.Shark, NPCID.PigronHallow } : new int[] { NPCID.Crab, NPCID.PinkJellyfish, NPCID.Shark, NPCID.Squid });
			else if (player.ZoneUnderworldHeight) pool.AddRange(hardmode ? new int[] { NPCID.RedDevil, NPCID.Lavabat, NPCID.FireImp } : new int[] { NPCID.Demon, NPCID.FireImp, NPCID.LavaSlime, NPCID.BoneSerpentHead });
			else if (player.ZoneRockLayerHeight || player.ZoneDirtLayerHeight) pool.AddRange(hardmode ? new int[] { NPCID.ArmoredSkeleton, NPCID.GiantBat, NPCID.ToxicSludge, NPCID.Mimic, NPCID.SkeletonArcher } : new int[] { NPCID.Skeleton, NPCID.CaveBat, NPCID.GiantWormHead, NPCID.MotherSlime, NPCID.Salamander });
			else pool.AddRange(hardmode ? new int[] { NPCID.Wraith, NPCID.Werewolf, NPCID.PossessedArmor, NPCID.WanderingEye } : new int[] { NPCID.BlueSlime, NPCID.GreenSlime, NPCID.Zombie, NPCID.DemonEye, NPCID.GoblinScout });
			return pool;
		}

		public static List<int> DownedBosses(bool hardmode) {
			var list = new List<int>();
			if (!hardmode) {
				if (NPC.downedSlimeKing) list.Add(NPCID.KingSlime);
				if (NPC.downedBoss1) list.Add(NPCID.EyeofCthulhu);
				if (NPC.downedBoss2) { list.Add(NPCID.EaterofWorldsHead); list.Add(NPCID.BrainofCthulhu); }
				if (NPC.downedQueenBee) list.Add(NPCID.QueenBee);
				if (NPC.downedBoss3) list.Add(NPCID.SkeletronHead);
				if (NPC.downedDeerclops) list.Add(NPCID.Deerclops);
			}
			else {
				if (NPC.downedQueenSlime) list.Add(NPCID.QueenSlimeBoss);
				if (NPC.downedMechBoss1) list.Add(NPCID.TheDestroyer);
				if (NPC.downedMechBoss2) list.Add(NPCID.Retinazer);
				if (NPC.downedMechBoss3) list.Add(NPCID.SkeletronPrime);
				if (NPC.downedPlantBoss) list.Add(NPCID.Plantera);
				if (NPC.downedGolemBoss) list.Add(NPCID.Golem);
				if (NPC.downedFishron) list.Add(NPCID.DukeFishron);
				if (NPC.downedEmpressOfLight) list.Add(NPCID.HallowBoss);
			}
			return list;
		}
	}
}
