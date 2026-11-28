using System;
using ITKamianets.Engine.Update;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathRunner.Game
{
    /// <summary>
    /// Looks for a newer release on GitHub when the game starts and offers it in a banner. The check, the download and the
    /// install are the engine's <see cref="AutoUpdater"/> (3d-unity-update): on Windows the installer is taken only from this
    /// repository's releases, checked against the size and SHA-256 GitHub reports, and run; elsewhere the release page opens.
    /// The editor skips the check.
    /// </summary>
    public sealed class UpdateChecker : MonoBehaviour
    {
        private const string Repository = "IT-Kamianets/math-runner.itkamianets.com";
        private const string InstallerName = "MathRunner-Setup.exe";
        private static readonly Color Panel = new Color(0.05f, 0.09f, 0.16f, 0.92f);
        private static readonly Color Accent = new Color(0.16f, 0.82f, 0.95f, 1f);

        private AutoUpdater _updater;
        private GameObject _banner;
        private TextMeshProUGUI _message;
        private Button _install, _later;

        private void Start()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            if (PlayerPrefs.GetInt("MathRunner.NoUpdateCheck", 0) == 1) return;
            _updater = gameObject.AddComponent<AutoUpdater>();
            _updater.Repo = Repository;
            _updater.WindowsAssetSuffix = InstallerName;
            _updater.UserAgent = "MathRunner-Updater";
            _updater.Changed += OnUpdaterChanged;
            _updater.Check();
#endif
        }

        private void OnDestroy()
        {
            if (_updater != null) _updater.Changed -= OnUpdaterChanged;
        }

#if UNITY_STANDALONE_WIN
        private const string InstallLabel = "Update now";
#else
        private const string InstallLabel = "Download";
#endif

        private void OnUpdaterChanged()
        {
            switch (_updater.State)
            {
                case UpdateState.Available:
                    if (_banner == null) ShowBanner($"Version {_updater.Latest.Version} is available.");
                    else SetButtons(true, "Version " + _updater.Latest.Version + " is available.");
                    break;
                case UpdateState.Downloading:
                    if (_message != null) _message.text = $"Downloading the update... {Mathf.RoundToInt(_updater.Progress * 100f)}%";
                    break;
                case UpdateState.Installing:
                    if (_message != null) _message.text = "Installing the update. The game will restart.";
                    break;
                case UpdateState.Failed:
                    // Offline before anything was offered says nothing; a failed download says so in the banner.
                    if (_banner != null) SetButtons(true, "The update was not installed. You can download it from the website.");
                    break;
            }
        }

        private void Install()
        {
            if (_updater == null || _updater.State != UpdateState.Available) return;
            SetButtons(false, "Starting the update...");
            _updater.Install();
#if !UNITY_STANDALONE_WIN
            Destroy(_banner);
#endif
        }

        private void SetButtons(bool enabled, string text)
        {
            if (_message != null) _message.text = text;
            if (_install != null) _install.interactable = enabled;
            if (_later != null) _later.interactable = enabled;
        }

        // ---- Banner ----------------------------------------------------------------------------

        private void ShowBanner(string text)
        {
            var canvasObject = new GameObject("Update Canvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            _banner = new GameObject("Update Banner", typeof(RectTransform), typeof(Image));
            _banner.transform.SetParent(canvasObject.transform, false);
            var rect = (RectTransform)_banner.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -24f);
            rect.sizeDelta = new Vector2(980f, 84f);
            _banner.GetComponent<Image>().color = Panel;

            _message = Label(rect, text, 30f, TextAlignmentOptions.MidlineLeft);
            _message.rectTransform.anchorMin = Vector2.zero;
            _message.rectTransform.anchorMax = Vector2.one;
            _message.rectTransform.offsetMin = new Vector2(28f, 0f);
            _message.rectTransform.offsetMax = new Vector2(-350f, 0f);

            _install = MakeButton(rect, InstallLabel, new Vector2(-132f, 0f), new Vector2(190f, 56f), Accent, Color.black,
                Install);
            _later = MakeButton(rect, "Later", new Vector2(-28f, 0f), new Vector2(90f, 56f), new Color(0.18f, 0.25f, 0.38f), Color.white,
                () => Destroy(_banner));
        }

        private static TextMeshProUGUI Label(RectTransform parent, string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = align;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = size;
            return label;
        }

        private static Button MakeButton(RectTransform parent, string text, Vector2 position, Vector2 size, Color fill, Color textColor,
            Action onClick)
        {
            var go = new GameObject(text + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = fill;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick());
            var label = Label(rect, text, 26f, TextAlignmentOptions.Center);
            label.color = textColor;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
