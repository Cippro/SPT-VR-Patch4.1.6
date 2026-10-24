# Legacy optional integrations

The four `*Support.cs` files are retained as porting source for EFT API,
AmandsGraphics, DynamicMaps, and WeaponCustomizer. They are excluded from the
standalone 4.1.6 build, and their libraries are not compile/runtime references.

Their typed fields, method signatures, and Harmony attributes in the old single
assembly caused native JSON converter discovery to fail when the corresponding
mod was absent. Checking `File.Exists` before applying the patches did not
isolate these types from the game's assembly scanning.

These integrations need separate dependency isolation and testing before they
can be included in a later build. The current release targets SPT + SteamVR
without optional mods. Core VR controls, native PIP, and headset optic patches
remain compiled in the standalone project. FIKA remains excluded.
