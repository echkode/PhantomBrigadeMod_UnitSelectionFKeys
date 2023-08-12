// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Linq;

using HarmonyLib;

using PhantomBrigade.Data;
using PBSettingUtility = PhantomBrigade.Data.SettingUtility;

using Rewired;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchKode.PBMods.UnitSelectionFKeys
{
	static class SettingUtility
	{
		private static bool mungedReInput;

		internal static void LoadData()
		{
			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) Attempting to fill missing input mappings using defaults from defined input actions",
					ModLink.modIndex,
					ModLink.modId);
			}

			AddExtraActionsToReInput();

			var inputMappings = PBSettingUtility.GetInputMappings();
			var data = DataMultiLinker<DataContainerInputAction>.data;
			var missing = new List<string>();
			foreach (var kvp in data)
			{
				var key = kvp.Key;
				if (inputMappings.ContainsKey(key))
				{
					continue;
				}

				var inputAction = kvp.Value;
				var inputMap = new InputMap()
				{
					keyboard = inputAction.defaultValueKeyboard,
					mouse = inputAction.defaultValueMouse,
					gamepad = inputAction.defaultValueGamepad
				};
				inputMappings.Add(key, inputMap);
				missing.Add(key);
			}

			if (missing.Count != 0)
			{
				missing.Sort();
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Filled in missing input mappings with defaults | count: {2} | actions:\n  {3}",
					ModLink.modIndex,
					ModLink.modId,
					missing.Count,
					string.Join("\n  ", missing));
			}

			if (ModLink.Settings.enableLogging)
			{
				DumpRewiredUserDataActions();
			}
		}

		private static void AddExtraActionsToReInput()
		{
			if (mungedReInput)
			{
				return;
			}

			var t = Traverse.Create(typeof(ReInput));
			var im = t.Property<InputManager_Base>("rewiredInputManager").Value;
			if (im == null)
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) input manager is not ready",
						ModLink.modIndex,
						ModLink.modId);
				}
				return;
			}

			var inputActions = im.userData.GetActions_Copy();
			var actionIDs = new HashSet<int>();
			foreach (var action in inputActions)
			{
				actionIDs.Add(action.id);
			}

			if (ModLink.Settings.enableLogging)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) found input manager | existing action count: {2}",
					ModLink.modIndex,
					ModLink.modId,
					inputActions.Count);
			}

			// Rewired requires that each action be uniquely named.
			InjectActionNames();

			var found = false;
			var data = DataMultiLinker<DataContainerInputAction>.data;
			foreach (var action in data.Values)
			{
				if (actionIDs.Contains(action.actionID))
				{
					continue;
				}

				var ria = new Rewired.InputAction();
				var ta = Traverse.Create(ria);
				ta.Field<int>("_id").Value = action.actionID;
				ta.Field<string>("_name").Value = action.textName;
				ta.Field<string>("_descriptiveName").Value = action.textName;
				ta.Field<Rewired.InputActionType>("_type").Value = Rewired.InputActionType.Button;
				ta.Field<bool>("_userAssignable").Value = true;
				inputActions.Add(ria);
				actionIDs.Add(action.actionID);
				found = true;

				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) adding action | id: {2} | name: {3}",
						ModLink.modIndex,
						ModLink.modId,
						ria.id,
						ria.name);
				}
			}

			if (!found)
			{
				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) no extra input actions found",
						ModLink.modIndex,
						ModLink.modId);
				}
				return;
			}

			Traverse.Create(im).Field("_userData").Field<List<Rewired.InputAction>>("actions").Value = inputActions;
			ReInput.Reset();

			RefreshPlayerForInputHelper();
			RefreshPlayerForGameObject<GameCameraSystem>();
			RefreshPlayerForGameObject<GameCursorSystem>();
			RefreshPlayerForCIViews();

			mungedReInput = true;

			Debug.LogWarningFormat(
				"Mod {0} ({1}) replaced existing actions on ReInput | count: {2}",
				ModLink.modIndex,
				ModLink.modId,
				inputActions.Count);
		}

		private static bool InjectActionNames()
		{
			var sectorKey = ModLink.Settings.languageSector;
			if (string.IsNullOrEmpty(sectorKey))
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) settings should contain a text library sector key in the languageSector attribute",
					ModLink.modIndex,
					ModLink.modId);
				return false;
			}

			var library = DataManagerText.libraryData;
			if (!library.sectors.ContainsKey(sectorKey))
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) sector should already exist in text library | sector: {2}",
					ModLink.modIndex,
					ModLink.modId,
					sectorKey);
				return false;
			}

			var entries = ModLink.Settings.labelTextEntries;
			if (entries == null || entries.Count == 0)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) settings should contain a list of action name/label entries",
					ModLink.modIndex,
					ModLink.modId,
					sectorKey);
				return false;
			}

			var sector = library.sectors[sectorKey];
			Debug.LogFormat(
				"Mod {0} ({1}) injecting entries into text library for action names/labels | sector: {2} | entries ({3}):\n  {4}",
				ModLink.modIndex,
				ModLink.modId,
				sectorKey,
				entries.Count,
				string.Join("\n  ", entries.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
			foreach (var entry in entries)
			{
				if (string.IsNullOrWhiteSpace(entry.Value))
				{
					Debug.LogWarningFormat(
						"Mod {0} ({1}) text for entry should not be empty | key: {2}",
						ModLink.modIndex,
						ModLink.modId,
						entry.Key);
					continue;
				}

				if (sector.entries.ContainsKey(entry.Key))
				{
					continue;
				}
				sector.entries[entry.Key] = new DataBlockTextEntryMain()
				{
					text = entry.Value,
					textProcessed = entry.Value,
				};
			}

			return true;
		}

		private static void RefreshPlayerForInputHelper()
		{
			var playerID = Traverse.Create(typeof(InputHelper)).Field<int>("playerID").Value;
			var player = ReInput.players.GetPlayer(playerID);
			InputHelper.player = player;

		}

		private static void RefreshPlayerForCIViews()
		{
			foreach (var rgo in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				var c = rgo.GetComponentInChildren<CIView>();
				if (c == null)
				{
					continue;
				}
				c.player = InputHelper.player;
			}
		}

		private static void RefreshPlayerForGameObject<T>()
		{
			foreach (var rgo in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				var c = rgo.GetComponentInChildren<T>();
				if (c == null)
				{
					continue;
				}

				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) found {2} component | game object: {2}",
						ModLink.modIndex,
						ModLink.modId,
						typeof(T).Name,
						rgo.name);
				}

				var t = Traverse.Create(c);
				var playerID = t.Field<int>("playerID").Value;
				var player = ReInput.players.GetPlayer(playerID);
				t.Field<Player>("player").Value = player;

				return;
			}

			Debug.LogWarningFormat(
				"Mod {0} ({1}) didn't find a game object with {2} component",
				ModLink.modIndex,
				ModLink.modId,
				typeof(T).Name);
		}

		private static void DumpRewiredUserDataActions()
		{
			Debug.LogFormat(
				"Mod {0} ({1}) checking for input manager",
				ModLink.modIndex,
				ModLink.modId);

			var t = Traverse.Create(typeof(ReInput));
			var im = t.Property<InputManager_Base>("rewiredInputManager").Value;
			if (im == null)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) input manager is not ready",
					ModLink.modIndex,
					ModLink.modId);
				return;
			}

			var stringified = new List<string>();
			foreach (var action in im.userData.GetActions_Copy())
			{
				var s = $"action | id: {action.id} | name: {action.name} | descriptive: {action.descriptiveName} | type: {action.type} | user assignable: {action.userAssignable} | category ID: {action.categoryId} | behavior ID {action.behaviorId}";
				stringified.Add(s);
			}

			Debug.LogFormat(
				"Mod {0} ({1}) rewired user data\n  {2}",
				ModLink.modIndex,
				ModLink.modId,
				string.Join("\n  ", stringified));
		}
	}
}
