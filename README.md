# UnitSelectionFKeys

**This mod is obsolete.** The game has been patched by the publisher so this is no longer a problem.

A library mod for [Phantom Brigade (Alpha)](https://braceyourselfgames.com/phantom-brigade/) that brings back using the function keys F1-F8 to select units in combat. It skips disabled/destroyed units so it matches the unit tab display. If there are more than 8 units in combat, you'll have to use the tab key or the mouse to select units 9 and above.

It is compatible with game patch **0.23.1-b5426**. That is an **EXPERIMENTAL** release. All library mods are fragile and susceptible to breakage whenever a new version is released.

I've packaged up a ready-to-use mod as a release. Just download and extract to your mod folder.  See this [detailed guide](https://github.com/NBKRedSpy/PB_EquipmentTypeRarity#installation) about how to enable and install mods in Phantom Brigade.

The keys can be remapped. Here's a screenshot of the new key remapping screen that ships with patch 0.23.1-b5426 showing the extra unit selection actions added by this mod and their default assignment to the function keys.

![Key remapping screen with new unit selection actions](Screenshots/Target_and_Weapon_Info_Popups.jpg)

## Localization

The text for the labels displayed in the key remapping screen can be changed by the localization system. The label text entries are in the `ui_setting_input_action` sector and use the following text entry keys:

- combat_unit1_name
- combat_unit2_name
- combat_unit3_name
- combat_unit4_name
- combat_unit5_name
- combat_unit6_name
- combat_unit7_name
- combat_unit8_name

The label text entries are also used internally by Rewired as the names of the actions so it is important that each label be unique.

## Technical Notes

Phantom Brigade uses the excellent [Rewired](https://guavaman.com/projects/rewired/) Unity asset to manage controller input. Unfortunately, Rewired isn't expecting actions to be added at runtime outside its Editor interface. I've had to inject the new unit selection actions into an internal data structure and then mash the reset button on Rewired to get the actions to take. It's generally not a good idea to reset Rewired during game play. However, I'm doing it fairly early on and I think I've caught all the places using a Rewired player so it seems to work.

The upside is that the key remapping that BYG just added to the game works for these new actions. You should be able to remap to mouse buttons as well but I don't have a fancy gaming mouse with a bazillion buttons to test it out.

To support localization, this mod has to inject entries for the key remapping labels directly into the default English text library. Localizations can then target those entries the same as the built-in ones.

There is a settings file which pushes most of the linkage between the input actions and the Rewired action names into configuration. The `labelTextEntries` keys follow a standard format which is the key for the input action with a `_name` suffix. The key of the input action is the same as the input action configuration file name without the `.yaml` suffix. This will allow the mod to be easily changed without recompiling if a subsequent release of the game introduces input actions with conflicting names.
