using System.Collections.Generic;
using BagofChaos.Common.Players;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BagofChaos.Common.GlobalItems
{
	/// <summary>Items that switch their own damage class at runtime opt out of Triforce conversion.</summary>
	public interface IManagesOwnDamageClass
	{
	}

	// Remembers the real damage class of a weapon while a Triforce has it converted, so taking the Triforce off restores it
	// and nothing is ever converted twice.
	public class TriforceGlobalItem : GlobalItem
	{
		public override bool InstancePerEntity => true;

		public DamageClass ConvertedFrom;

		public override bool AppliesToEntity(Item entity, bool lateInstantiation) => lateInstantiation && (entity.damage > 0 || entity.ammo > 0);

		/// <summary>Sets this item's class to whatever the player's Triforces say it should be right now.</summary>
		public void Apply(Item item, ChaosPlayer chaos) {
			if (item.ModItem is IManagesOwnDamageClass) {
				ConvertedFrom = null;
				return;
			}
			DamageClass original = ConvertedFrom ?? item.DamageType;
			DamageClass effective = chaos.EffectiveDamageClass(original);
			if (effective == original) {
				if (ConvertedFrom != null) {
					item.DamageType = original;
					ConvertedFrom = null;
				}
				return;
			}
			ConvertedFrom = original;
			item.DamageType = effective;
		}

		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
			if (ConvertedFrom == null)
				return;
			TooltipLine name = tooltips.Find(t => t.Mod == "Terraria" && t.Name == "ItemName");
			if (name != null)
				name.Text += " " + Language.GetTextValue("Mods.BagofChaos.Triforce.Converted", Language.GetTextValue("Mods.BagofChaos.Triforce." + ChaosPlayer.ConversionOf(item.DamageType)));
		}
	}
}
