using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using MathRunner.Core.Update;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace MathRunner.Game
{
    /// <summary>
    /// Looks for a newer release on GitHub when the game starts and offers to install it. Only the Windows
    /// player does this; the editor and other platforms skip it. The installer is only taken from this
    /// repository's releases and, when GitHub reports a SHA-256 for it, only run if the hash matches.
    /// </summary>
    public sealed class UpdateChecker : MonoBehaviour
    {
        private const string Repository = "IT-Kamianets/math-runner.itkamianets.com";
        private const string InstallerName = "MathRunner-Setup.exe";
        private static readonly Color Panel = new Color(0.05f, 0.09f, 0.16f, 0.92f);
        private static readonly Color Accent = new Color(0.16f, 0.82f, 0.95f, 1f);

        [Serializable] private class ReleaseAsset { public string name; public string browser_download_url; public string digest; }
        [Serializable] private class ReleaseInfo { public string tag_name; public bool draft; public bool prerelease; public ReleaseAsset[] assets; }

        private GameObject _banner;
        private TextMeshProUGUI _message;
        private Button _install, _later;
        private ReleaseAsset _asset;
        private bool _busy;

        private void Start()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (PlayerPrefs.GetInt("MathRunner.NoUpdateCheck", 0) == 1) return;
            StartCoroutine(Check());
#endif
        }

        private IEnumerator Check()
        {
            using (var request = UnityWebRequest.Get($"https://api.github.com/repos/{Repository}/releases/latest"))
            {
                request.SetRequestHeader("Accept", "application/vnd.github+json");
                request.timeout = 10;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) yield break; // offline: say nothing

                ReleaseInfo release;
                try { release = JsonUtility.FromJson<ReleaseInfo>(request.downloadHandler.text); }
                catch (Exception) { yield break; }

                if (release == null || release.draft || release.prerelease || release.assets == null) yield break;
                if (!ReleaseVersion.IsNewer(Application.version, release.tag_name)) yield break;

                foreach (var asset in release.assets)
                {
                    if (asset.name != InstallerName) continue;
                    if (!IsTrustedUrl(asset.browser_download_url)) yield break;
                    _asset = asset;
                    break;
                }

                if (_asset == null) yield break;
                ShowBanner($"Version {release.tag_name.TrimStart('v', 'V')} is available.");
            }
        }

        private static bool IsTrustedUrl(string url)
        {
            return !string.IsNullOrEmpty(url) &&
                   url.StartsWith($"https://github.com/{Repository}/releases/download/", StringComparison.Ordinal);
        }

        private IEnumerator Install()
        {
            _busy = true;
            _install.interactable = false;
            _later.interactable = false;
            var path = Path.Combine(Application.temporaryCachePath, InstallerName);
            if (File.Exists(path)) File.Delete(path);

            using (var request = new UnityWebRequest(_asset.browser_download_url, UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler = new DownloadHandlerFile(path) { removeFileOnAbort = true };
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    _message.text = $"Downloading the update... {Mathf.RoundToInt(request.downloadProgress * 100f)}%";
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Fail("Could not download the update. Try again later.");
                    yield break;
                }
            }

            if (!HashMatches(path, _asset.digest))
            {
                File.Delete(path);
                Fail("The downloaded update did not match its checksum, so it was not installed.");
                yield break;
            }

            _message.text = "Installing the update. The game will restart.";
            yield return null;
            try
            {
                // Windows asks for permission (the installer needs administrator rights), then the installer
                // replaces the game and starts it again.
                Process.Start(new ProcessStartInfo(path, "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS")
                {
                    UseShellExecute = true,
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("Update was not started: " + e.Message);
                Fail("The update was not started. You can download it from the website.");
                yield break;
            }

            Application.Quit();
        }

        private static bool HashMatches(string path, string digest)
        {
            if (string.IsNullOrEmpty(digest) || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) return true;
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
                return string.Equals(actual, digest.Substring(7), StringComparison.OrdinalIgnoreCase);
            }
        }

        private void Fail(string text)
        {
            _busy = false;
            _message.text = text;
            _later.interactable = true;
            _install.interactable = true;
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

            _install = MakeButton(rect, "Update now", new Vector2(-132f, 0f), new Vector2(190f, 56f), Accent, Color.black,
                () => { if (!_busy) StartCoroutine(Install()); });
            _later = MakeButton(rect, "Later", new Vector2(-28f, 0f), new Vector2(90f, 56f), new Color(0.18f, 0.25f, 0.38f), Color.white,
                () => { if (!_busy) Destroy(_banner); });
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
