// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Input.Components;
using PhantomBrigade.Overworld;

using UnityEngine;

namespace EchKode.PBMods.UnitSelectionFKeys
{
	static class InputCombatShared
	{
		internal static class InputActions
		{
			internal const int CombatUnit1 = 1001;
			internal const int CombatUnit2 = 1002;
			internal const int CombatUnit3 = 1003;
			internal const int CombatUnit4 = 1004;
			internal const int CombatUnit5 = 1005;
			internal const int CombatUnit6 = 1006;
			internal const int CombatUnit7 = 1007;
			internal const int CombatUnit8 = 1008;

			internal static readonly List<int> CombatUnits = new List<int>()
			{
				CombatUnit1,
				CombatUnit2,
				CombatUnit3,
				CombatUnit4,
				CombatUnit5,
				CombatUnit6,
				CombatUnit7,
				CombatUnit8,
			};
		}

		internal static void Execute()
		{
			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) extending keyboard check",
					ModLink.modIndex,
					ModLink.modId);
			}

			if (!InputHelper.IsInputAvailableInContext("CombatMain"))
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) early exit because not in CombatMain context",
						ModLink.modIndex,
						ModLink.modId);
				}
				return;
			}

			var i = 0;
			foreach (var actionID in InputActions.CombatUnits)
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) checking action | action ID: {2}",
						ModLink.modIndex,
						ModLink.modId,
						actionID);
				}
				if (InputHelper.CheckAndConsumeAction(actionID))
				{
					if (ModLink.Settings.enableLogging)
					{
						Debug.LogFormat(
							"Mod {0} ({1}) found action | action ID: {2}",
							ModLink.modIndex,
							ModLink.modId,
							actionID);
					}
					SelectUnit(i);
					break;
				}
				i += 1;
			}
		}

		private static void SelectUnit(int selectIndex)
		{
			if (!CombatUIUtility.IsCombatUISafe())
			{
				return;
			}
			if (!OverworldUtility.IsFeatureUnlocked("feature_combat_unit_selection"))
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) feature_combat_unit_selection is not unlocked",
						ModLink.modIndex,
						ModLink.modId);
				}
				return;
			}

			var combat = Contexts.sharedInstance.combat;
			if (!combat.Simulating)
			{
				var e = Contexts.sharedInstance.input.combatUIMode.e;
				if (e != CombatUIModes.Unit_Selection && e != CombatUIModes.Replay)
				{
					return;
				}
			}

			var unitSortedList = CIViewCombatMode.ins.GetUnitSortedList();
			var unitCount = unitSortedList.Count;
			if (unitCount == 0)
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) No units on the field, nothing to select",
						ModLink.modIndex,
						ModLink.modId);
				}
				return;
			}

			if (selectIndex >= unitCount)
			{
				return;
			}

			var (index, selectedCombatEntity) = FindCombatEntity(selectIndex, unitSortedList);
			if (selectedCombatEntity == null)
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) No active units to select | index: {2}",
						ModLink.modIndex,
						ModLink.modId,
						selectIndex);
				}
				return;
			}

			combat.ReplaceUnitSelected(selectedCombatEntity.id.id);
			MoveCamera(combat, selectedCombatEntity);

			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) Selected unit | unit count: {2} | index: {3} | unit: {4}",
					ModLink.modIndex,
					ModLink.modId,
					unitSortedList,
					index,
					selectedCombatEntity.ToLog());
			}
		}

		private static (int, CombatEntity) FindCombatEntity(int selectIndex, List<PersistentEntity> unitSortedList)
		{
			System.Func<PersistentEntity, bool> isSelected = null;

			var selectedCombatEntity = IDUtility.GetSelectedCombatEntity();
			if (selectedCombatEntity != null)
			{
				var selectedUnit = IDUtility.GetLinkedPersistentEntity(selectedCombatEntity);
				if (selectedUnit != null)
				{
					isSelected = u => u == selectedUnit;
				}
			}

			for ( ; selectIndex < unitSortedList.Count; selectIndex += 1)
			{
				var unit = unitSortedList[selectIndex];
				var combatEntity = IDUtility.GetLinkedCombatEntity(unit);
				if (!ScenarioUtility.IsUnitActive(unit, combatEntity))
				{
					continue;
				}
				if (isSelected != null && isSelected(unit))
				{
					return (-1, null);
				}
				return (selectIndex, combatEntity);
			}

			return (-1, null);
		}

		private static void MoveCamera(CombatContext combat, CombatEntity entity)
		{
			if (combat.Simulating)
			{
				GameCameraSystem.MoveToLocation(entity.position.v, true);
			}
			else if (Contexts.sharedInstance.input.combatUIMode.e == CombatUIModes.Replay)
			{
				GameCameraSystem.MoveToLocation(entity.combatView.view.transform.position);
				if (CombatReplayHelper.playbackActive)
				{
					CombatReplayHelper.playbackActive = false;
					CIViewCombatTimeControl.ins.RedrawStatusForReplay();
				}
			}
			else
			{
				GameCameraSystem.MoveToLocation(entity.projectedPosition.v);
			}
			GameCameraSystem.ClearTarget();
		}
	}
}
