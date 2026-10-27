# Math Runner

An endless 3D runner built on the [IT Kamianets 3D Engine](https://3d.itkamianets.com). A kid runs down a city street. Every few seconds a math question floats above the track and three answer boards block the three lanes. Run into the lane with the right answer to score. Pick a wrong one, or hit an obstacle, and you lose a life.

Unity 6000.6, Universal 3D (URP). Third-person camera behind the runner.

## How the game works

1. **Run.** The runner moves forward on its own. Speed grows with distance up to a cap.
2. **Switch lanes.** There are 3 lanes. Left and right move one lane at a time (keys or gamepad). Jump goes over low obstacles, slide goes under overhead ones.
3. **Answer.** A gate row has one answer board per lane. The lane you are in when you pass the row is your answer.
   - Correct: +10 points, plus a streak bonus (+1 for every 5 correct in a row).
   - Wrong: streak resets and you lose a life.
4. **Avoid.** Obstacles sit between gates. Hitting one costs a life and resets the streak.
5. **Collect.** Coins give 1 point each. Power-ups: slow-motion, magnet, shield, extra life.
6. **Game over** when lives reach zero. Then retry or return to the menu.

### Difficulty

| Tier | Operations | Numbers |
|---|---|---|
| Easy | + and - | 1 to 20, no negative results |
| Medium | + and - | 5 to 50, negatives allowed |
| Hard | + - x | 2 to 50, factors up to 10 |
| Expert | + - x / | 5 to 99, factors up to 12, whole-number division |

Wrong answers are always close to the right one (so they look plausible) and all three answers are distinct.

## Code layout

```text
Assets/MathRunner/
  Core/                 pure C#, no Unity references (assembly MathRunner.Core)
    Math/               MathQuestion, DifficultyTier, QuestionGenerator
    Run/                RunSession, LaneModel, SpeedCurve, GameStateMachine
    Track/              TrackPlanner
  Tests/EditMode/       NUnit tests for everything in Core
```

`Core` holds all game rules and has no `UnityEngine` dependency, so it is fast to test and independent of scenes. Unity-side scripts (MonoBehaviours, prefabs, UI) will read from it and never duplicate its rules.

| Class | Job |
|---|---|
| `QuestionGenerator` | Builds a question and one answer per lane. Seeded, so runs are reproducible. |
| `DifficultyTier` | Which operations and number ranges a level uses. |
| `RunSession` | Score, streak, lives, coins, distance. Raises `AnswerResolved` and `Died`. |
| `LaneModel` | Current lane, shifting left and right, lane to world X. |
| `SpeedCurve` | Forward speed as a function of distance. |
| `GameStateMachine` | Menu, Countdown, Running, Paused, GameOver and the allowed moves between them. |
| `TrackPlanner` | Plans each stretch ahead of the runner: obstacles, a coin line, then the gate row. Seeded. |

### Flow of one gate

1. `TrackPlanner.PlanNextSection()` returns the items for the next stretch, ending in a gate row (one `Gate` item per lane).
2. `QuestionGenerator.Next(tier)` makes the question for that row. Answer `i` goes on the board in lane `i`.
3. When the runner crosses the row, the game calls `session.ResolveAnswer(question.IsCorrect(lane.Current))`.
4. `RunSession` updates score, streak and lives. HUD and audio listen to its events.

## Packages

Engine packages (from `Packages/manifest.json`): `3d-unity-core`, `3d-unity-scene`, `3d-unity-input`, `3d-unity-character`, `3d-unity-assets`, `3d-unity-audio`, `3d-constructor-character-view-camera`, `3d-constructor-character-view-controls`.

Input comes only from `3d-unity-input`. Every package with its purpose: https://3d.itkamianets.com/packages.md

## Art

3D models are generated in Meshy or built in Unity. The full list, budgets and prompts are in [models.md](models.md).

## Run the project

1. Open the folder in Unity Hub with Unity 6000.6. Git must be installed so Unity can fetch the packages.
2. Run the tests: Window > General > Test Runner > EditMode.
3. Open `Assets/Scenes/Main.unity`.

## Releasing (Windows)

The game ships as a Windows installer on [GitHub Releases](https://github.com/IT-Kamianets/math-runner.itkamianets.com/releases). Installers are never committed to the repo.

1. In Unity, build for Windows x86-64 into `Builds/Windows/`. The player must be named `MathRunner.exe` (Project Settings > Player > Product Name).
2. Install [Inno Setup](https://jrsoftware.org/isinfo.php), then run:
   ```bash
   ISCC.exe /DAppVersion=0.1.0 Tools/installer/MathRunner.iss
   ```
   The output is `Builds/Installer/MathRunner-Setup.exe`.
3. Create a GitHub Release with a tag such as `v0.1.0`, and attach `MathRunner-Setup.exe`. Keep the file name exactly the same on every release: the website downloads `releases/latest/download/MathRunner-Setup.exe`.

The installer is not code signed, so Windows SmartScreen may warn on first run.

Full step-by-step guide, with a test checklist and troubleshooting: [RELEASING.md](RELEASING.md).

## Roadmap

- [x] Core logic and tests
- [x] Scene, runner, camera, lane controller (`Assets/MathRunner/Game`)
- [x] Track spawner, gates, obstacles, coins
- [x] HUD and menus
- [ ] Power-ups (slow-motion, magnet, shield, extra life)
- [ ] Animated runner and final models (starter models are static)
- [ ] Audio and effects
- [ ] High scores
- [ ] First Windows release on GitHub

## License

[MIT](LICENSE). Math Runner is a showcase game built with the IT Kamianets 3D Engine, so you are free to use and adapt it. Third-party assets, such as generated 3D models and fonts, may carry their own terms.

## Changing an engine package

The `3d-*` packages are shared by every game. To change one, clone it into `Packages/` (Unity uses that copy instead of the git URL; git ignores it), work on a branch or your fork, and open a pull request to its `master` branch. [AGENTS.md](AGENTS.md) has the steps.

## AI agents

[AGENTS.md](AGENTS.md) is the guide for any AI agent; [CLAUDE.md](CLAUDE.md) points Claude Code to it.
