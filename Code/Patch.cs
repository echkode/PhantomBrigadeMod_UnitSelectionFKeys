using HarmonyLib;

using PBInputCombatShared = PhantomBrigade.Combat.Systems.InputCombatShared;
using PBSettingUtility = PhantomBrigade.Data.SettingUtility;

namespace EchKode.PBMods.UnitSelectionFKeys
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBSettingUtility), "LoadData")]
		[HarmonyPostfix]
		static void Su_LoadDataPostfix()
		{
			SettingUtility.LoadData();
		}

		[HarmonyPatch(typeof(PBInputCombatShared), "Execute")]
		[HarmonyPostfix]
		static void Ics_ExecutePostfix()
		{
			InputCombatShared.Execute();
		}
	}
}
