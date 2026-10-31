# Releasing the Windows installer

This guide takes you from a Unity build to a download link on the website. The installer is never committed to git. It lives on GitHub Releases.

```text
Unity build  ->  Inno Setup  ->  MathRunner-Setup.exe  ->  GitHub Release  ->  website button
Builds/Windows/             Builds/Installer/                              releases/latest/download/...
```

## One-time setup

1. **Unity:** open the project with Unity 6000.6 and set Project Settings > Player > Product Name to `MathRunner`. Unity names the player after it, and the installer script expects `MathRunner.exe`.
2. **Inno Setup:** install it from https://jrsoftware.org/isdl.php (free). Use version 6 or newer.
3. **Check access:** you need write access to the GitHub repo to create releases.

## Make a release

### 1. Pick a version

Use semantic versions: `0.1.0` for the first public build, then `0.1.1` for fixes and `0.2.0` for new features. The Git tag is the version with a `v` in front: `v0.1.0`.

### 2. Build the game in Unity

Set Project Settings > Player > Version to the release version first (for example `0.2.0`). The in-game updater compares it with the latest GitHub release tag.

1. Run **Math Runner > Build Windows** in Unity (or `Unity.exe -batchmode -quit -projectPath . -executeMethod BuildWindows.Run`). It writes `Builds/Windows/` for you.
2. Or build by hand: File > Build Profiles, choose **Windows**, architecture **x86-64**, and build into `Builds/Windows/`.
3. Check that `Builds/Windows/MathRunner.exe` exists next to `MathRunner_Data/` and `UnityPlayer.dll`.
4. Run it once by double-clicking. If it does not start, do not release it.

`Builds/` is git-ignored, so none of this is committed.

### 3. Build the installer

From the repo root, in a terminal:

```bash
ISCC.exe /DAppVersion=0.1.0 Tools/installer/MathRunner.iss
```

If `ISCC.exe` is not found, use its full path, usually `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`. You can also open `Tools/installer/MathRunner.iss` in the Inno Setup app and press Ctrl+F9.

The result is `Builds/Installer/MathRunner-Setup.exe`.

### 4. Test the installer

Do this before publishing, ideally on a different PC or a clean Windows user:

- [ ] The installer opens and shows the MIT license.
- [ ] It installs to `C:\Program Files\Math Runner`.
- [ ] A Start menu entry appears and the game launches from it.
- [ ] The desktop shortcut works when you tick it.
- [ ] Uninstall (Settings > Apps) removes the game completely.

### 5. Zip the build (optional fallback)

A zip is useful if SmartScreen blocks the installer. In PowerShell:

```powershell
Compress-Archive -Path Builds/Windows/* -DestinationPath Builds/MathRunner-Windows.zip -Force
```

### 6. Publish on GitHub

1. Open https://github.com/IT-Kamianets/math-runner.itkamianets.com/releases/new
2. **Tag:** type `v0.1.0` and choose "Create new tag on publish".
3. **Title:** `Math Runner 0.1.0`.
4. **Notes:** a few lines about what is new. "Generate release notes" fills in the commit list for you.
5. **Attach files:** drag in `MathRunner-Setup.exe` and, if you made it, `MathRunner-Windows.zip`.
6. Click **Publish release**. Do not tick "pre-release" for a normal release, because `releases/latest` ignores pre-releases.

With the GitHub CLI you can do the same in one command:

```bash
gh release create v0.1.0 Builds/Installer/MathRunner-Setup.exe Builds/MathRunner-Windows.zip --title "Math Runner 0.1.0" --generate-notes
```

### 7. Check the website

The Download button on the site points at:

```text
https://github.com/IT-Kamianets/math-runner.itkamianets.com/releases/latest/download/MathRunner-Setup.exe
```

Open it in a private browser window. It must start the download. If it shows a 404, check that the release is published (not a draft) and that the attached file is named exactly `MathRunner-Setup.exe`.

## Automatic updates

On start the Windows game asks GitHub for the latest release. If its tag is higher than the game's version, a banner offers **Update now**. The game downloads `MathRunner-Setup.exe` from this repository's release, checks its SHA-256 when GitHub provides one, then runs it with `/SILENT`. Windows asks for administrator permission, the installer replaces the game in place (same `AppId`) and starts it again. Offline, drafts and pre-releases are ignored. Players can turn the check off by setting the `MathRunner.NoUpdateCheck` PlayerPrefs key to 1.

This only works while the release tag is a plain version like `v0.2.0`, the file keeps the name `MathRunner-Setup.exe`, and the tag is higher than the version inside the build.

## Rules to keep the link working

- **Never rename `MathRunner-Setup.exe`.** The website link depends on this exact name. The version is in the tag and inside the installer, not in the file name.
- **Do not commit installers or builds** to git.
- **Do not delete the latest release.** Without one, the website button returns 404.
- Publish new versions as new releases. Do not replace the file in an old release.

## SmartScreen warning

The installer is not code signed, so Windows shows "Windows protected your PC" on first run. Users click **More info**, then **Run anyway**. The website already says this.

Signing removes the warning, but a certificate costs money and is not needed for a showcase game. If you add one later, sign `MathRunner-Setup.exe` with `signtool` after step 3.

## Troubleshooting

| Problem | Fix |
|---|---|
| Inno Setup fails with "no files found" | `Builds/Windows/` is empty or missing. Redo step 2. |
| The installed game does not start | Run `MathRunner.exe` from `Builds/Windows/` directly. If that also fails, the problem is in the Unity build, not the installer. |
| The shortcut points to a missing file | Product Name in Unity is not `MathRunner`. Change it and rebuild, or edit `AppExe` in `Tools/installer/MathRunner.iss`. |
| Website button gives a 404 | The release is a draft or a pre-release, or the file name differs from `MathRunner-Setup.exe`. |
| Antivirus flags the installer | Common for unsigned installers. Upload the file to https://www.virustotal.com to check it, and mention the zip as an alternative. |

## Later: automate it

Once the game builds reliably, a GitHub Actions workflow can build, package and attach the installer on every tag. That needs a Unity license stored as a repo secret, so it is worth doing only after the manual flow works.
