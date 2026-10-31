using System;
using ITKamianets.Engine.Input;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MathRunner.Game
{
    /// <summary>Score, lives, the question and the menu screens. Built in code, drawn with uGUI.</summary>
    public sealed class Hud : MonoBehaviour
    {
        private static readonly Color Panel = new Color(0.05f, 0.09f, 0.16f, 0.82f);
        private static readonly Color Accent = new Color(0.16f, 0.82f, 0.95f, 1f);
        private static readonly Color Good = new Color(0.25f, 0.85f, 0.45f, 1f);
        private static readonly Color Bad = new Color(0.95f, 0.3f, 0.3f, 1f);
        private static readonly Color Idle = new Color(0.18f, 0.25f, 0.38f, 1f);

        public event Action PlayClicked;
        public event Action RetryClicked;
        public event Action MenuClicked;
        public event Action ResumeClicked;
        public event Action QuitClicked;
        public event Action<int> TierClicked;

        private GameObject _hudGroup, _menu, _countdown, _paused, _gameOver;
        private TextMeshProUGUI _score, _lives, _streak, _question, _feedback, _countdownText, _overStats, _hint;
        private Button[] _tierButtons;
        private float _feedbackLeft;

        public void Build(string[] tierNames)
        {
            EnsureEventSystem();

            var canvasObject = new GameObject("HUD Canvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasObject.transform;

            BuildRunningHud(root);
            BuildMenu(root, tierNames);
            BuildCountdown(root);
            BuildPaused(root);
            BuildGameOver(root);
            ShowMenu(0);
        }

        private void Update()
        {
            if (_feedbackLeft <= 0f) return;
            _feedbackLeft -= Time.unscaledDeltaTime;
            var alpha = Mathf.Clamp01(_feedbackLeft / 0.4f);
            var c = _feedback.color;
            c.a = alpha;
            _feedback.color = c;
        }

        // ---- State screens --------------------------------------------------------------

        public void ShowMenu(int selectedTier)
        {
            SetOnly(_menu);
            HighlightTier(selectedTier);
        }

        public void ShowCountdown(int number)
        {
            SetOnly(_countdown, _hudGroup);
            _countdownText.text = number > 0 ? number.ToString() : "GO!";
        }

        public void ShowRunning() => SetOnly(_hudGroup);

        public void ShowPaused() => SetOnly(_paused, _hudGroup);

        public void ShowGameOver(int score, int best, int correct, int wrong, int coins, int meters)
        {
            SetOnly(_gameOver);
            _overStats.text = "Score  " + score + "\nBest  " + best + "\nCorrect  " + correct + "   Wrong  " + wrong +
                              "\nCoins  " + coins + "   Distance  " + meters + " m";
        }

        public void HighlightTier(int selected)
        {
            for (var i = 0; i < _tierButtons.Length; i++)
                _tierButtons[i].GetComponent<Image>().color = i == selected ? Accent : Idle;
        }

        public void SetControlsHint(string text) => _hint.text = text;

        // ---- Live values ---------------------------------------------------------------

        public void SetScore(int value) => _score.text = "SCORE  " + value;
        public void SetLives(int lives, int max) => _lives.text = "LIVES  " + lives + " / " + max;
        public void SetStreak(int streak) => _streak.text = streak > 1 ? "STREAK  x" + streak : string.Empty;
        public void SetQuestion(string text, bool asQuestion = true)
        {
            var full = asQuestion ? text + "  = ?" : text;
            if (_question.text != full) _question.text = full;
        }

        public void ShowFeedback(string text, bool good)
        {
            _feedback.text = text;
            _feedback.color = good ? Good : Bad;
            _feedbackLeft = 1.1f;
        }

        // ---- Building the screens -------------------------------------------------------

        private void BuildRunningHud(RectTransform root)
        {
            _hudGroup = Group("Running HUD", root);
            var rect = (RectTransform)_hudGroup.transform;

            _score = Label("Score", rect, "SCORE  0", 54, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(_score.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(700, 80));
            _streak = Label("Streak", rect, string.Empty, 40, Accent, TextAlignmentOptions.TopLeft);
            Anchor(_streak.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -100), new Vector2(700, 60));
            _lives = Label("Lives", rect, "LIVES  3 / 3", 54, Color.white, TextAlignmentOptions.TopRight);
            Anchor(_lives.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(700, 80));

            var panel = Box("Question Panel", rect, Panel);
            Anchor(panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(760, 150));
            _question = Label("Question", panel, "? + ?  = ?", 96, Color.white, TextAlignmentOptions.Center);
            Stretch(_question.rectTransform);

            _feedback = Label("Feedback", rect, string.Empty, 110, Good, TextAlignmentOptions.Center);
            Anchor(_feedback.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1200, 160));
        }

        private void BuildMenu(RectTransform root, string[] tierNames)
        {
            _menu = Group("Menu", root);
            var rect = (RectTransform)_menu.transform;
            Stretch(Box("Dim", rect, new Color(0.02f, 0.04f, 0.09f, 0.7f)));

            var title = Label("Title", rect, "MATH RUNNER", 150, Accent, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 270), new Vector2(1400, 190));
            var sub = Label("Subtitle", rect, "Run into the lane with the right answer", 48, Color.white, TextAlignmentOptions.Center);
            Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1400, 70));

            _tierButtons = new Button[tierNames.Length];
            var step = 300f;
            var startX = -step * (tierNames.Length - 1) * 0.5f;
            for (var i = 0; i < tierNames.Length; i++)
            {
                var index = i;
                _tierButtons[i] = MakeButton(rect, tierNames[i], new Vector2(260, 90), 44, () => TierClicked?.Invoke(index));
                Anchor((RectTransform)_tierButtons[i].transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(startX + step * i, 20), new Vector2(260, 90));
            }

            var play = MakeButton(rect, "PLAY", new Vector2(460, 120), 70, () => PlayClicked?.Invoke());
            play.GetComponent<Image>().color = Good;
            Anchor((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -130), new Vector2(460, 120));

            // A browser tab cannot be closed by the game, so WebGL builds have no Exit button.
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                var exit = MakeButton(rect, "EXIT", new Vector2(300, 90), 46, () => QuitClicked?.Invoke());
                exit.GetComponent<Image>().color = Bad;
                Anchor((RectTransform)exit.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -270), new Vector2(300, 90));
            }

            _hint = Label("Hint", rect, string.Empty, 34, new Color(0.8f, 0.88f, 1f), TextAlignmentOptions.Center);
            Anchor(_hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(1700, 120));
        }

        private void BuildCountdown(RectTransform root)
        {
            _countdown = Group("Countdown", root);
            _countdownText = Label("Number", (RectTransform)_countdown.transform, "3", 300, Color.white, TextAlignmentOptions.Center);
            Anchor(_countdownText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 360));
        }

        private void BuildPaused(RectTransform root)
        {
            _paused = Group("Paused", root);
            var rect = (RectTransform)_paused.transform;
            Stretch(Box("Dim", rect, new Color(0.02f, 0.04f, 0.09f, 0.7f)));
            var title = Label("Title", rect, "PAUSED", 140, Color.white, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 180), new Vector2(1200, 180));
            var resume = MakeButton(rect, "RESUME", new Vector2(420, 110), 60, () => ResumeClicked?.Invoke());
            resume.GetComponent<Image>().color = Good;
            Anchor((RectTransform)resume.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(420, 110));
            var menu = MakeButton(rect, "MENU", new Vector2(420, 110), 60, () => MenuClicked?.Invoke());
            Anchor((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(420, 110));
        }

        private void BuildGameOver(RectTransform root)
        {
            _gameOver = Group("Game Over", root);
            var rect = (RectTransform)_gameOver.transform;
            Stretch(Box("Dim", rect, new Color(0.02f, 0.04f, 0.09f, 0.78f)));
            var title = Label("Title", rect, "GAME OVER", 140, Bad, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 290), new Vector2(1400, 180));
            _overStats = Label("Stats", rect, string.Empty, 56, Color.white, TextAlignmentOptions.Center);
            Anchor(_overStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1400, 320));
            var retry = MakeButton(rect, "RETRY", new Vector2(420, 110), 60, () => RetryClicked?.Invoke());
            retry.GetComponent<Image>().color = Good;
            Anchor((RectTransform)retry.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-240, -190), new Vector2(420, 110));
            var menu = MakeButton(rect, "MENU", new Vector2(420, 110), 60, () => MenuClicked?.Invoke());
            Anchor((RectTransform)menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(240, -190), new Vector2(420, 110));
        }

        // ---- Widgets --------------------------------------------------------------------

        private void SetOnly(params GameObject[] visible)
        {
            foreach (var group in new[] { _hudGroup, _menu, _countdown, _paused, _gameOver })
                group.SetActive(Array.IndexOf(visible, group) >= 0);
        }

        private static GameObject Group(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            return go;
        }

        private static RectTransform Box(string name, RectTransform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        private static TextMeshProUGUI Label(string name, RectTransform parent, string text, float size, Color color,
            TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static Button MakeButton(RectTransform parent, string text, Vector2 size, float fontSize, Action onClick)
        {
            var go = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = Idle;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());
            var label = Label("Label", (RectTransform)go.transform, text, fontSize, Color.white, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            return button;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        /// <summary>Menus take clicks and taps through the engine's UI actions, not a project input asset.</summary>
        private static void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            var host = existing != null ? existing.gameObject : new GameObject("EventSystem", typeof(EventSystem));
            var module = host.GetComponent<InputSystemUIInputModule>();
            if (module == null) module = host.AddComponent<InputSystemUIInputModule>();

            var ui = EngineInput.Default.UI;
            module.point = InputActionReference.Create(ui.Point);
            module.leftClick = InputActionReference.Create(ui.Click);
            module.scrollWheel = InputActionReference.Create(ui.ScrollWheel);
            module.move = InputActionReference.Create(ui.Navigate);
            module.submit = InputActionReference.Create(ui.Submit);
            module.cancel = InputActionReference.Create(ui.Cancel);
            module.trackedDevicePosition = InputActionReference.Create(ui.TrackedDevicePosition);
            module.trackedDeviceOrientation = InputActionReference.Create(ui.TrackedDeviceOrientation);
            module.middleClick = null;
            module.rightClick = null;
        }
    }
}
