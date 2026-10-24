# SPT-VR port to SPT 4.1.6 — project checkpoint

## Goal
Port SPT-VR 1.3.0, originally built for SPT 4.0.13, to SPT 4.1.6. Keep FIKA out of scope. Preserve VR controller changes and headset-specific optic behavior.

## Optics requirement
EFT/SPT uses PIP for optic rendering. Keep the native `OpticCameraManager` camera and its own render target as the scene render path. VR changes must continue to adapt optic resolution, zoom/FOV, and reticle alignment/sharpness for the headset without replacing EFT's PIP update loop or redirecting the PIP camera's target texture.

## Work completed in the previous working copy
- Updated movement API references in `MovementPatches.cs`.
- Migrated several loot, door, container, magazine, and interaction references in `HandsInteractionController.cs`.
- Updated hand/weapon interaction references in `GunInteractionController.cs`.
- Kept `OpticComponentUpdater.LateUpdate` running natively; post-update VR code applies far-clip and headset zoom behavior.
- Synchronized VR zoom through 4.1.6's `ScopeZoomHandler` state and `SightComponent.ScopeZoomValue`, because a direct camera FOV write is overwritten by the native PIP updater.
- The optic reticle alignment patch was changed to run after EFT's native reticle update and only adjust position/rotation; it no longer overrides the whole update or writes the removed `float_1` field.
- Native PIP camera target is not redirected. Optional reticle decoupling uses a separate lens-sampling texture after the PIP scene render.

## Build status
The last full build in the previous working copy reported 441 errors across 25 files, down from about 530 at the start. There is no functional DLL or installable package yet. The latest reticle patch change was not rebuilt. Main remaining areas include weapon/optic API updates, UI/inventory, VR settings, menus, and old EFT hooks.

## Current workspace note
The previous source checkout was reported at `/tmp/spt-vr-audit`, but it is not available in the current workspace. Reopen or restore the source project before continuing; then create a dedicated Git branch for the 4.1.6 port and rebuild against the SPT 4.1.6 Managed assemblies.

## User preferences and constraints
- Exclude FIKA.
- Keep the VR-specific controller/movement/menu/interactions behavior in scope.
- Preserve SPT/EFT PIP optic rendering while keeping headset-specific optic adjustments.
