# Changelog

## [0.9.3] - 2026-08-10

### Fixed

- Use Unity's editor label style for FX state names such as `Idle` and `Gun`, keeping their text readable with the active editor skin.

## [0.9.2] - 2026-08-10

### Fixed

- Instantiate the editable avatar monitor copy directly in its preview scene instead of moving an enabled hierarchy between scenes, preventing additional `MAMoveIndependently` `TransformAccessArray` index errors.

## [0.9.1] - 2026-08-10

### Fixed

- Keep the editable avatar in its original preview scene and instantiate the synchronized source directly in the NDMF scene.
- Defer synchronized-source cleanup until Modular Avatar has refreshed its `TransformAccessArray`, preventing `MAMoveIndependently` index errors.
- Keep the synchronized NDMF source hidden outside the avatar monitor render.

## [0.9.0] - 2026-08-10

### Fixed

- Clear the AvatarMask from the first layer of newly created FX controllers so hand animations work correctly.

### Changed

- Audited and incorporated applicable upstream changes from v0.7.1 through v0.8.0.
- Removed legacy VRCSDK2 branches and classes; this package now targets VRCSDK3 only.
- Updated the original author's social link label from Twitter to X.

### Removed

- Removed the obsolete built-in update checker because updates are managed by VCC/VPM.

### Compatibility

- Requires Unity 2022.3.22f1.
- Requires `com.vrchat.avatars` 3.10.x.
- Requires `nadena.dev.ndmf` 1.14.4 or newer and earlier than 2.0.0 prereleases.

## [0.7.0] - 2026-08-10

### Added

- Added NDMF 1.14.4 preview support to the avatar monitor.
- Added VPM package metadata and GitHub release/listing automation.
- Added an editor-only assembly definition for Unity Package Manager compatibility.

### Changed

- Targeted Unity 2022.3.22f1 and VRChat SDK - Avatars 3.10.x.
- Animations created after pressing Edit are assigned to the state that was actually edited.
- User settings are stored under `UserSettings` instead of inside the package.
- VRChat SDK assets are located correctly when the SDK is installed under `Packages`.
- The update link now points to the maintained fork releases.

### Compatibility

- Requires `com.vrchat.avatars` 3.10.x.
- Requires `nadena.dev.ndmf` 1.14.4 or newer and earlier than 2.0.0 prereleases.
