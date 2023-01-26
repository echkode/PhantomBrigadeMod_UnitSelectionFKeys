// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace EchKode.PBMods.UnitSelectionFKeys
{
	partial class ModLink
	{
		internal sealed class ModSettings
		{
			[System.Flags]
			internal enum UnitSelectionCapability
			{
				Standard = 0,
				AllowWhenSimulationPaused = 1,
			}

#pragma warning disable CS0649
			public bool enableLogging;
			public UnitSelectionCapability capabilities;
			public string languageSector;
			public Dictionary<string, string> labelTextEntries;
#pragma warning restore CS0649
		}

		internal static ModSettings Settings;

		internal static void LoadSettings()
		{
			var settingsPath = Path.Combine(modPath, "settings.yaml");
			Settings = UtilitiesYAML.ReadFromFile<ModSettings>(settingsPath, false);
			if (Settings == null)
			{
				Settings = new ModSettings()
				{
					labelTextEntries = new Dictionary<string, string>(),
				};

				Debug.LogWarningFormat(
					"Mod {0} ({1}) unable to load settings | path: {2}",
					modIndex,
					modId,
					settingsPath);
			}
		}
	}
}
