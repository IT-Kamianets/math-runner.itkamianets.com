# Math Runner: plan

Math Runner is the ITKP 3D game closest to production, so it becomes the reference game: the setup other games and the constructor starters copy. This is roadmap 7 of the framework plan (`3d.itkamianets.com/docs/improvement-plan.md`). Findings come from a code review in November 2026; none was reproduced in Unity yet.

## 1. Branches and versions

- [ ] Merge `origin/master` into `dev`: `master` has the 0.3.1 commit (new runner model) that `dev` lacks, and `dev` has 7 commits `master` lacks.
- [ ] `dev` still has `bundleVersion: 0.3.0` while `v0.3.1` is released, so a build from `dev` offers to "update" to 0.3.1. Take the version from the git tag in the build scripts (`Assets/MathRunner/Editor/Build*.cs`).
- [ ] Release only from `master`; `dev` reaches `master` through a pull request.

## 2. Packages

- [ ] Pin every engine package to a tag (`...git#vX.Y.Z`) once the packages are tagged, and pin `com.coplaydev.unity-mcp` (now `#main`).
- [ ] Remove the packages the game does not use: it uses only `3d-unity-core` and `3d-unity-input`; character, character-view camera and controls, scene, assets and audio are installed but unused. Keep `3d-unity-assetra` if the Assetra sync stays.
- [ ] `packages-lock.json` lists `3d-unity-assetra` as `file:3d-unity-assetra`, a folder that does not exist, while the manifest uses its git URL. Let Unity regenerate the lock and commit it.

## 3. Models: follow the engine's model rule

The code looks up prefabs by keys such as `23_striped_barrier` (`GameController.cs`, `TrackEnvironment.cs`, `Editor/MeshyArt.cs`), not by the `assetra.json` ids such as `obstacles/striped-barrier`. Three model sets live side by side: `Assets/MathRunner/Art/Models` (OBJ starters), `Assets/MathRunner/Art/Meshy` (FBX) and `Assets/Models` (GLB, the rule's location).

- [ ] Add `com.unity.cloud.gltfast`, so the GLB files in `Assets/Models` import.
- [ ] Key prefabs by asset id; the scene builder (`Editor/GameSceneBuilder.cs`) maps `assetra.json` ids to `Assets/Models/<group>/<name>.glb`.
- [ ] Remove the OBJ and FBX copies and their editor importers once every slot is filled by a GLB; working files go to `Art/Models/<group>/<name>/` at the repository root.
- [ ] Bring the 13 Meshy GLBs that fail `itkp-models check` (triangle budget or size) within their slots, and run the check in CI.

## 4. Updater

- [ ] `Game/UpdateChecker.cs` duplicates `3d-unity-update`, and is the safer of the two (it checks the URL and the SHA-256 digest). Move those checks into the package (framework roadmap 1), then replace `UpdateChecker` with the package.
- [ ] Until then: when GitHub gives no digest the installer runs unchecked (`HashMatches` returns true); require a digest, and check the downloaded size.

## 5. Game bugs

- [ ] Coin pickup depends on frame rate (`GameController.cs` `CheckSpawns`: `|z - coinZ| < 0.6`). At 20 m/s below about 15 fps a frame step is longer than the window and coins are skipped. Check whether the runner passed the coin between the previous and the current z instead.
- [ ] `EngineInput.Default.SchemeChanged` is subscribed in `Awake` with a lambda and never removed: unsubscribe in `OnDestroy`.
- [ ] The best score lives only in memory (`_best`): save it (for example through `3d-unity-save` or PlayerPrefs).
- [ ] Remove the empty `OnAnswerResolved` handler or use it.

## 6. CI and releases

- [ ] `.github/workflows/build.yml` only dispatches the mobile build on push to `master`. Add a workflow that runs the EditMode tests (`Assets/MathRunner/Tests/EditMode`) on every push and pull request.
- [ ] On a `v*` tag: Windows build, Inno Setup (`Tools/installer/MathRunner.iss`), Linux and macOS packages (`Tools/installer/package-unix.sh`), and upload to the GitHub release, replacing the manual steps in `RELEASING.md`. Needs a Unity license as a repository secret.
- [ ] Update `RELEASING.md` to the automated flow.

## 7. Then: the starters

- [ ] Copy what worked here (pinned manifest, glTFast, asset-id prefabs, CI, release workflow) into the constructor starter repositories and their AGENTS.md.
