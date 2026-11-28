# Math Runner: plan to become the reference game

Math Runner is the engine game closest to production, so new games copy from it. This is roadmap 7 of the engine's improvement plan (`3d.itkamianets.com/docs/improvement-plan.md`).

## Done (28 November 2026)

- `master` merged into `dev`. Work happens on `dev`; releases are still built from `master`.
- Engine packages pinned to release tags in `Packages/manifest.json` and `Packages/packages-lock.json`, and trimmed to what the game uses: `3d-unity-core`, `3d-unity-input`, `3d-unity-update` and `3d-unity-assetra`. The Character View camera and controls, `3d-unity-character`, `-scene`, `-assets` and `-audio` were listed but never used by the game's code, scenes or prefabs. `com.coplaydev.unity-mcp` is pinned to `v10.3.0` instead of `#main`.
- glTFast (`com.unity.cloud.gltfast` 6.14.1) added, so the GLBs in `Assets/Models` import.
- The update banner runs on the engine's `AutoUpdater` (`3d-unity-update`): the installer comes only from this repository's releases and must match the size and SHA-256 GitHub reports. The game's own download and hash code is gone.

## Open

- **One model set.** `Assets/MathRunner/Art/Models` (OBJ and GLB per prop, used by the prefabs in `Art/Prefabs`) duplicates `Assets/Models/<group>/<name>.glb` (the `assetra.json` ids). Point each prefab at the GLB of its asset id, check every prefab in the scene, then delete `Art/Models`. This needs the Unity editor (the prefabs reference the models by GUID), so it is done by hand, one prefab group at a time.
- **CI.** Tests on push and the Windows release on a tag (engine roadmap 3).
- **Starters.** When the two items above are done, copy into the constructor starters: the pinned manifest with the pin script, the `assetra.json` model layout, the updater banner and the AGENTS.md rules.

## Moving the pins later

From the 3d.itkamianets.com repository: `node tools/engine/pin-packages.mjs --workspace .. --games math-runner.itkamianets.com --latest`, then open the project once in Unity and commit the manifest and the lock file together.
