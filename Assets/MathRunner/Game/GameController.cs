using System;
using System.Collections.Generic;
using ITKamianets.Engine.Input;
using MathRunner.Core.Math;
using MathRunner.Core.Run;
using MathRunner.Core.Track;
using TMPro;
using UnityEngine;

namespace MathRunner.Game
{
    /// <summary>
    /// Runs the game in the scene: reads input from the engine, moves the runner, places what
    /// <see cref="TrackPlanner"/> plans, and reports hits and answers to <see cref="RunSession"/>.
    /// All rules live in MathRunner.Core; this class only turns them into movement and visuals.
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        [Serializable]
        public struct PrefabEntry
        {
            public string key;
            public GameObject prefab;
        }

        private const int LaneCount = 3;
        private const float LaneWidth = 2f;
        private const float Gravity = -32f;
        private const float JumpVelocity = 10.5f;
        private const float SlideTime = 0.8f;
        private const float InvulnerableTime = 1.5f;
        private const float LaneSpeed = 14f;
        private const float SpawnAhead = 150f;
        private const float KeepBehind = 25f;
        private const float CoinSpacing = 2.5f;
        private const float SwipePixels = 70f;

        [SerializeField] private PrefabEntry[] prefabs = Array.Empty<PrefabEntry>();
        [SerializeField] private Camera gameCamera;
        [SerializeField] private float runnerYaw;
        [SerializeField] private float leftBuildingYaw = 90f;
        [SerializeField] private float rightBuildingYaw = -90f;

        private static readonly DifficultyTier[] Tiers =
            { DifficultyTier.Easy, DifficultyTier.Medium, DifficultyTier.Hard, DifficultyTier.Expert };

        private static readonly string[] BlockKeys = { "23_striped_barrier", "24_cone_cluster", "25_crate_stack" };

        private sealed class Spawned
        {
            public SpawnKind Kind;
            public ObstacleKind Obstacle;
            public float Z;
            public float X;
            public GameObject Go;
            public bool Done;
        }

        private sealed class GateRow
        {
            public float Z;
            public MathQuestion Question;
            public GameObject[] Boards;
            public GameObject Label;
        }

        private readonly Dictionary<string, GameObject> _prefabLookup = new Dictionary<string, GameObject>();
        private readonly List<Spawned> _spawned = new List<Spawned>();
        private readonly Queue<GateRow> _gates = new Queue<GateRow>();
        private readonly GameStateMachine _fsm = new GameStateMachine();
        private readonly LaneModel _lane = new LaneModel(LaneCount);
        private readonly SpeedCurve _speedCurve = new SpeedCurve(8f, 20f, 1.5f);

        private Hud _hud;
        private TrackEnvironment _environment;
        private Transform _spawnRoot;
        private Transform _runner;
        private Transform _model;
        private Renderer[] _runnerRenderers;

        private RunSession _session;
        private TrackPlanner _planner;
        private QuestionGenerator _questions;
        private int _tierIndex;
        private int _best;

        private float _z, _x, _y, _vy, _slideLeft, _invulnerableLeft, _countdownLeft, _shake, _clock;
        private bool _grounded = true;
        private float _prevMoveX, _prevMoveY;
        private Vector2 _pressPoint;
        private bool _swipeDone;

        private int _shownScore = -1, _shownLives = -1, _shownStreak = -1;

        private void Awake()
        {
            foreach (var entry in prefabs)
                if (entry.prefab != null) _prefabLookup[entry.key] = entry.prefab;

            if (gameCamera == null) gameCamera = Camera.main;
            _spawnRoot = new GameObject("Track").transform;
            _environment = new TrackEnvironment(Prefab, _spawnRoot)
            {
                LeftBuildingYaw = leftBuildingYaw,
                RightBuildingYaw = rightBuildingYaw,
            };

            BuildRunner();

            var tierNames = new string[Tiers.Length];
            for (var i = 0; i < Tiers.Length; i++) tierNames[i] = Tiers[i].Name;
            _hud = new GameObject("HUD").AddComponent<Hud>();
            _hud.Build(tierNames);
            _hud.PlayClicked += StartRun;
            _hud.RetryClicked += StartRun;
            _hud.MenuClicked += () => _fsm.TryMoveTo(GameState.Menu);
            _hud.ResumeClicked += () => _fsm.TryMoveTo(GameState.Running);
            _hud.TierClicked += SelectTier;

            _fsm.Changed += OnStateChanged;
            EngineInput.Default.SchemeChanged += _ => RefreshHint();
            RefreshHint();
            ResetWorld();
            _hud.ShowMenu(_tierIndex);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        // ---- Frame loop ------------------------------------------------------------------

        private void Update()
        {
            _clock += Time.unscaledDeltaTime;
            ReadInput(out var shift, out var jump, out var slide, out var submit, out var pause);

            switch (_fsm.State)
            {
                case GameState.Menu:
                    if (shift != 0) SelectTier(Mathf.Clamp(_tierIndex + shift, 0, Tiers.Length - 1));
                    if (submit) StartRun();
                    _environment.Update(_z, SpawnAhead, KeepBehind);
                    break;

                case GameState.Countdown:
                    _countdownLeft -= Time.unscaledDeltaTime;
                    _hud.ShowCountdown(Mathf.CeilToInt(Mathf.Max(0f, _countdownLeft)));
                    if (_countdownLeft <= 0f) _fsm.TryMoveTo(GameState.Running);
                    _environment.Update(_z, SpawnAhead, KeepBehind);
                    break;

                case GameState.Running:
                    if (pause) { _fsm.TryMoveTo(GameState.Paused); break; }
                    Tick(Time.deltaTime, shift, jump, slide);
                    break;

                case GameState.Paused:
                    if (pause) _fsm.TryMoveTo(GameState.Running);
                    break;

                case GameState.GameOver:
                    if (submit) StartRun();
                    break;
            }

            UpdateRunnerVisual();
        }

        private void LateUpdate()
        {
            if (gameCamera == null) return;
            var dt = Time.unscaledDeltaTime;
            var anchorX = _x * 0.6f;
            var desired = new Vector3(anchorX, 3.3f + _y * 0.25f, _z - 6.4f);
            var cam = gameCamera.transform;
            cam.position = Vector3.Lerp(cam.position, desired, 1f - Mathf.Exp(-12f * dt));
            if (_shake > 0f)
            {
                _shake = Mathf.Max(0f, _shake - dt);
                cam.position += UnityEngine.Random.insideUnitSphere * (_shake * 0.5f);
            }

            cam.rotation = Quaternion.LookRotation(new Vector3(anchorX, 1.2f, _z + 6f) - cam.position);
        }

        private void Tick(float dt, int shift, bool jump, bool slide)
        {
            var speed = _speedCurve.SpeedAt(_z);
            var step = speed * dt;
            _z += step;
            _session.AddDistance(step);

            if (shift != 0) _lane.Shift(shift);
            if (jump && _grounded)
            {
                _vy = JumpVelocity;
                _grounded = false;
                _slideLeft = 0f;
            }

            if (slide)
            {
                if (!_grounded) _vy = -18f; // drop fast, then slide
                _slideLeft = SlideTime;
            }

            if (!_grounded)
            {
                _vy += Gravity * dt;
                _y += _vy * dt;
                if (_y <= 0f)
                {
                    _y = 0f;
                    _vy = 0f;
                    _grounded = true;
                }
            }

            _slideLeft = Mathf.Max(0f, _slideLeft - dt);
            _invulnerableLeft = Mathf.Max(0f, _invulnerableLeft - dt);
            _x = Mathf.MoveTowards(_x, _lane.LaneToX(_lane.Current, LaneWidth), LaneSpeed * dt);

            _environment.Update(_z, SpawnAhead, KeepBehind);
            while (_planner.NextGateZ < _z + SpawnAhead) SpawnSection(_planner.PlanNextSection());
            CheckSpawns();
            CheckGates();
            Cleanup();
            RefreshHud();
        }

        // ---- Input (only through the engine) --------------------------------------------

        private void ReadInput(out int shift, out bool jump, out bool slide, out bool submit, out bool pause)
        {
            var input = EngineInput.Default;
            var move = input.Player.Move.ReadValue<Vector2>();
            shift = Edge(move.x, ref _prevMoveX);
            var vertical = Edge(move.y, ref _prevMoveY);
            jump = vertical > 0 || input.Player.Jump.WasPressedThisFrame();
            slide = vertical < 0 || input.Player.Crouch.WasPressedThisFrame();
            submit = input.UI.Submit.WasPressedThisFrame();
            pause = input.UI.Pause.WasPressedThisFrame();

            // Swipe with a finger or the mouse: the engine's UI pointer actions.
            if (input.UI.Click.WasPressedThisFrame())
            {
                _pressPoint = input.UI.Point.ReadValue<Vector2>();
                _swipeDone = false;
            }
            else if (input.UI.Click.IsPressed() && !_swipeDone)
            {
                var delta = input.UI.Point.ReadValue<Vector2>() - _pressPoint;
                if (delta.magnitude > SwipePixels)
                {
                    _swipeDone = true;
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) shift = delta.x > 0 ? 1 : -1;
                    else if (delta.y > 0) jump = true;
                    else slide = true;
                }
            }
        }

        private static int Edge(float value, ref float previous)
        {
            var result = 0;
            if (value > 0.5f && previous <= 0.5f) result = 1;
            else if (value < -0.5f && previous >= -0.5f) result = -1;
            previous = value;
            return result;
        }

        private void RefreshHint()
        {
            switch (EngineInput.Default.Scheme)
            {
                case InputScheme.Touch:
                    _hud.SetControlsHint("Swipe left or right: change lane      Swipe up: jump      Swipe down: slide");
                    break;
                case InputScheme.Gamepad:
                    _hud.SetControlsHint("Left stick: lane, jump, slide      Start: pause");
                    break;
                default:
                    _hud.SetControlsHint("A / D or arrows: change lane      W / Space: jump      S: slide      Esc: pause\nLeft and right on the menu pick the difficulty, Enter starts");
                    break;
            }
        }

        // ---- Game flow -------------------------------------------------------------------

        private void SelectTier(int index)
        {
            _tierIndex = index;
            _hud.HighlightTier(index);
        }

        private void StartRun()
        {
            if (_fsm.State != GameState.Menu && _fsm.State != GameState.GameOver) return;
            ResetWorld();
            var seed = Environment.TickCount;
            _session = new RunSession(3);
            _session.Died += OnDied;
            _session.AnswerResolved += OnAnswerResolved;
            _planner = new TrackPlanner(seed);
            _questions = new QuestionGenerator(seed + 1);
            _countdownLeft = 3f;
            _fsm.TryMoveTo(GameState.Countdown);
            RefreshHud(true);
            _hud.SetQuestion("Get ready", false);
        }

        private void ResetWorld()
        {
            foreach (var item in _spawned)
                if (item.Go != null) Destroy(item.Go);
            _spawned.Clear();
            foreach (var row in _gates) DestroyRow(row, 0f);
            _gates.Clear();
            _environment.Clear();
            _lane.Reset();
            _z = 0f;
            _x = 0f;
            _y = 0f;
            _vy = 0f;
            _grounded = true;
            _slideLeft = 0f;
            _invulnerableLeft = 0f;
            _prevMoveX = _prevMoveY = 0f;
            _environment.Update(0f, SpawnAhead, KeepBehind);
            if (gameCamera != null)
                gameCamera.transform.position = new Vector3(0f, 3.3f, -6.4f);
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            Time.timeScale = to == GameState.Paused ? 0f : 1f;
            switch (to)
            {
                case GameState.Menu:
                    ResetWorld();
                    _hud.ShowMenu(_tierIndex);
                    break;
                case GameState.Countdown:
                    _hud.ShowCountdown(3);
                    break;
                case GameState.Running:
                    _hud.ShowRunning();
                    break;
                case GameState.Paused:
                    _hud.ShowPaused();
                    break;
                case GameState.GameOver:
                    _best = Mathf.Max(_best, _session.Score);
                    _hud.ShowGameOver(_session.Score, _best, _session.CorrectCount, _session.WrongCount,
                        _session.Coins, Mathf.RoundToInt(_session.Distance));
                    break;
            }
        }

        private void OnDied() => _fsm.TryMoveTo(GameState.GameOver);

        private void OnAnswerResolved(AnswerResult result)
        {
            // The popup text is set by CheckGates, which knows the points gained.
        }

        private void RefreshHud(bool force = false)
        {
            if (_session == null) return;
            if (force || _session.Score != _shownScore) { _shownScore = _session.Score; _hud.SetScore(_shownScore); }
            if (force || _session.Lives != _shownLives) { _shownLives = _session.Lives; _hud.SetLives(_shownLives, _session.MaxLives); }
            if (force || _session.Streak != _shownStreak) { _shownStreak = _session.Streak; _hud.SetStreak(_shownStreak); }
            if (_gates.Count > 0) _hud.SetQuestion(_gates.Peek().Question.Text);
        }

        // ---- Spawning --------------------------------------------------------------------

        private void SpawnSection(List<SpawnItem> items)
        {
            var question = _questions.Next(Tiers[_tierIndex], LaneCount);
            foreach (var item in items)
            {
                switch (item.Kind)
                {
                    case SpawnKind.Gate: SpawnGate(item, question); break;
                    case SpawnKind.Obstacle: SpawnObstacle(item); break;
                    case SpawnKind.Coins: SpawnCoins(item); break;
                }
            }
        }

        private GateRow _building;

        private void SpawnGate(SpawnItem item, MathQuestion question)
        {
            if (_building == null || !Mathf.Approximately(_building.Z, item.Z))
            {
                _building = new GateRow { Z = item.Z, Question = question, Boards = new GameObject[LaneCount] };
                _building.Label = MakeText(_spawnRoot, new Vector3(0f, 3.8f, item.Z), question.Text, 12f, Color.white, 8f, 1.6f);
                _gates.Enqueue(_building);
            }

            var x = _lane.LaneToX(item.Lane, LaneWidth);
            var board = Make("answer_board", _spawnRoot, new Vector3(x, 0.45f, item.Z), 0f);
            if (board == null) return;
            board.transform.localScale = Vector3.one * 1.15f;
            MakeText(board.transform, new Vector3(0f, 0.5f, -0.09f), question.Answers[item.Lane].ToString(), 9f,
                new Color(0.03f, 0.08f, 0.18f), 1.5f, 0.9f);
            _building.Boards[item.Lane] = board;
        }

        private void SpawnObstacle(SpawnItem item)
        {
            string key;
            var x = _lane.LaneToX(item.Lane, LaneWidth);
            var scaleX = 1f;
            switch (item.Obstacle)
            {
                case ObstacleKind.Low:
                    key = "26_low_fence";
                    x = 0f;
                    scaleX = 3.3f;
                    break;
                case ObstacleKind.Overhead:
                    key = "27_overhead_sign";
                    x = 0f;
                    scaleX = 2.6f;
                    break;
                default:
                    key = BlockKeys[Mathf.Abs(Mathf.RoundToInt(item.Z * 7f)) % BlockKeys.Length];
                    break;
            }

            var go = Make(key, _spawnRoot, new Vector3(x, 0.1f, item.Z), 0f);
            if (go == null) return;
            go.transform.localScale = new Vector3(scaleX, 1f, 1f);
            _spawned.Add(new Spawned { Kind = SpawnKind.Obstacle, Obstacle = item.Obstacle, Z = item.Z, X = x, Go = go });
        }

        private void SpawnCoins(SpawnItem item)
        {
            var x = _lane.LaneToX(item.Lane, LaneWidth);
            for (var k = 0; k < item.Count; k++)
            {
                var z = item.Z + k * CoinSpacing;
                var go = Make("28_star_coin", _spawnRoot, new Vector3(x, 1.1f, z), 0f);
                if (go == null) continue;
                _spawned.Add(new Spawned { Kind = SpawnKind.Coins, Z = z, X = x, Go = go });
            }
        }

        private void CheckSpawns()
        {
            foreach (var s in _spawned)
            {
                if (s.Done || s.Go == null) continue;

                if (s.Kind == SpawnKind.Coins)
                {
                    s.Go.transform.rotation = Quaternion.Euler(0f, _clock * 220f, 0f);
                    if (Mathf.Abs(_z - s.Z) < 0.6f && Mathf.Abs(_x - s.X) < 0.9f)
                    {
                        s.Done = true;
                        _session.CollectCoin();
                        Destroy(s.Go);
                    }
                    continue;
                }

                if (_z < s.Z - 0.3f) continue;
                s.Done = true;
                if (_invulnerableLeft > 0f) continue;

                bool hit;
                switch (s.Obstacle)
                {
                    case ObstacleKind.Low: hit = _y < 0.65f; break;
                    case ObstacleKind.Overhead: hit = _slideLeft <= 0f; break;
                    default: hit = Mathf.Abs(_x - s.X) < 1.05f; break;
                }

                if (!hit) continue;
                _session.HitObstacle();
                _invulnerableLeft = InvulnerableTime;
                _shake = 0.35f;
                _hud.ShowFeedback("OUCH!", false);
            }
        }

        private void CheckGates()
        {
            while (_gates.Count > 0 && _z >= _gates.Peek().Z)
            {
                var row = _gates.Dequeue();
                var lane = Mathf.Clamp(Mathf.RoundToInt(_x / LaneWidth) + 1, 0, LaneCount - 1);
                var correct = row.Question.IsCorrect(lane);
                var gained = _session.ResolveAnswer(correct);
                _hud.ShowFeedback(correct ? "+" + gained : "WRONG  " + row.Question.CorrectAnswer, correct);
                if (!correct) _shake = 0.35f;
                if (row.Boards[lane] != null) Tint(row.Boards[lane], correct ? new Color(0.25f, 0.85f, 0.45f) : new Color(0.95f, 0.3f, 0.3f));
                DestroyRow(row, 0.35f);
            }
        }

        private void Cleanup()
        {
            for (var i = _spawned.Count - 1; i >= 0; i--)
            {
                if (_spawned[i].Z >= _z - KeepBehind) continue;
                if (_spawned[i].Go != null) Destroy(_spawned[i].Go);
                _spawned.RemoveAt(i);
            }
        }

        private static void DestroyRow(GateRow row, float delay)
        {
            foreach (var board in row.Boards)
                if (board != null) Destroy(board, delay);
            if (row.Label != null) Destroy(row.Label, delay);
        }

        // ---- Runner ----------------------------------------------------------------------

        private void BuildRunner()
        {
            _runner = new GameObject("Runner").transform;
            var prefab = Prefab("01_runner_kid");
            if (prefab == null) return;
            var model = Instantiate(prefab, _runner);
            model.name = "Model";
            foreach (var controller in model.GetComponentsInChildren<CharacterController>()) controller.enabled = false;
            foreach (var collider in model.GetComponentsInChildren<Collider>()) collider.enabled = false;
            _model = model.transform;
            _model.localRotation = Quaternion.Euler(0f, runnerYaw, 0f);
            _runnerRenderers = model.GetComponentsInChildren<Renderer>();
        }

        private void UpdateRunnerVisual()
        {
            if (_runner == null) return;
            var running = _fsm.State == GameState.Running || _fsm.State == GameState.Countdown;
            var bob = _grounded && _fsm.State == GameState.Running ? Mathf.Abs(Mathf.Sin(_clock * 13f)) * 0.07f : 0f;
            _runner.position = new Vector3(_x, _y + bob, _z);

            if (_model == null) return;
            var targetX = _lane.LaneToX(_lane.Current, LaneWidth);
            var lean = Mathf.Clamp((_x - targetX) * 14f, -18f, 18f);
            var sliding = _slideLeft > 0f;
            var pitch = sliding ? 28f : (running ? 7f : 0f);
            _model.localRotation = Quaternion.Euler(pitch, runnerYaw, lean);
            _model.localScale = new Vector3(1f, sliding ? 0.55f : 1f, 1f);

            if (_runnerRenderers == null) return;
            var visible = _invulnerableLeft <= 0f || Mathf.FloorToInt(_clock * 14f) % 2 == 0;
            foreach (var r in _runnerRenderers) r.enabled = visible;
        }

        // ---- Helpers ---------------------------------------------------------------------

        private GameObject Prefab(string key)
        {
            _prefabLookup.TryGetValue(key, out var prefab);
            return prefab;
        }

        private GameObject Make(string key, Transform parent, Vector3 position, float yaw)
        {
            var prefab = Prefab(key);
            if (prefab == null) return null;
            var go = Instantiate(prefab, parent);
            go.name = key;
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            foreach (var collider in go.GetComponentsInChildren<Collider>()) collider.enabled = false;
            return go;
        }

        private static GameObject MakeText(Transform parent, Vector3 position, string text, float size, Color color,
            float width, float height)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.sizeDelta = new Vector2(width, height);
            label.enableAutoSizing = true;
            label.fontSizeMin = 1f;
            label.fontSizeMax = size;
            return go;
        }

        private static void Tint(GameObject go, Color color)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r.GetComponent<TextMeshPro>() != null) continue;
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                r.SetPropertyBlock(block);
            }
        }
    }
}
