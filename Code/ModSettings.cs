// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

namespace EchKode.PBMods.UnitSelectionFKeys
{
	partial class ModLink
	{
		internal sealed class ModSettings
		{
#pragma warning disable CS0649
			internal bool enableLogging;
#pragma warning restore CS0649
		}

		internal static ModSettings Settings;

		internal static void LoadSettings()
		{
			Settings = UtilitiesYAML.LoadDataFromFile<ModSettings>(modPath, "settings.yaml", false, false);
			if (Settings == null)
			{
				Settings = new ModSettings();
			}
		}
	}
}
