using System.Collections.Generic;

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

			// Force language loading to bring in the names of the extra input actions.
			// Rewired requires that each action be uniquely named.
			PBSettingUtility.ApplyOption("game_language", true);

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
			RefreshPlayerForGameCursorSystem();

			mungedReInput = true;

			Debug.LogWarningFormat(
				"Mod {0} ({1}) replaced existing actions on ReInput | count: {2}",
				ModLink.modIndex,
				ModLink.modId,
				inputActions.Count);
		}

		private static void RefreshPlayerForInputHelper()
		{
			var playerID = Traverse.Create(typeof(InputHelper)).Field<int>("playerID").Value;
			var player = ReInput.players.GetPlayer(playerID);
			InputHelper.player = player;

		}

		private static void RefreshPlayerForGameCursorSystem()
		{
			foreach (var rgo in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				var gcs = rgo.GetComponentInChildren<GameCursorSystem>();
				if (gcs == null)
				{
					continue;
				}

				if (ModLink.Settings.enableLogging)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) found GameCursorSystem component | game object: {2}",
						ModLink.modIndex,
						ModLink.modId,
						rgo.name);
				}

				var t = Traverse.Create(gcs);
				var playerID = t.Field<int>("playerID").Value;
				var player = ReInput.players.GetPlayer(playerID);
				t.Field<Player>("player").Value = player;

				return;
			}

			Debug.LogWarningFormat(
				"Mod {0} ({1}) didn't find a game object with GameCursorSystem component",
				ModLink.modIndex,
				ModLink.modId);
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
