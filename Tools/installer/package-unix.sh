#!/usr/bin/env bash
# Packages the Linux and macOS builds into downloads. Run from the repo root in WSL or Linux:
#   bash Tools/installer/package-unix.sh 0.2.1
# Needs Builds/Linux/ and Builds/macOS/MathRunner.app (see RELEASING.md). Writes to Builds/Installer/:
#   MathRunner-Setup.deb     Debian and Ubuntu installer (installs to /opt/math-runner)
#   MathRunner-Linux.tar.gz  any Linux distro, with install.sh (installs for the current user)
#   MathRunner-macOS.zip     the unsigned app; unzip, drag to Applications
# Names never carry the version, so releases/latest/download/<name> keeps working.
set -euo pipefail

VERSION="${1:?usage: package-unix.sh <version>, for example 0.2.1}"
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/Builds/Installer"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
mkdir -p "$OUT"

ICON="$ROOT/docs/favicon-512.png"
DESKTOP='[Desktop Entry]
Type=Application
Name=Math Runner
Comment=Run to the right answer
Exec=@EXEC@
Icon=@ICON@
Terminal=false
Categories=Game;Education;
'

if [ -x "$ROOT/Builds/Linux/MathRunner.x86_64" ]; then
  LINUX="$ROOT/Builds/Linux"

  # --- .deb -------------------------------------------------------------------------------------
  PKG="$WORK/deb"
  mkdir -p "$PKG/DEBIAN" "$PKG/opt/math-runner" "$PKG/usr/bin" "$PKG/usr/share/applications" \
           "$PKG/usr/share/icons/hicolor/512x512/apps"
  cp -r "$LINUX"/. "$PKG/opt/math-runner/"
  rm -rf "$PKG"/opt/math-runner/*_BurstDebugInformation_DoNotShip "$PKG"/opt/math-runner/*_BackUpThisFolder_ButDontShipItWithYourGame
  cp "$ICON" "$PKG/usr/share/icons/hicolor/512x512/apps/math-runner.png"
  ln -s /opt/math-runner/MathRunner.x86_64 "$PKG/usr/bin/math-runner"
  printf '%s' "$DESKTOP" | sed -e 's|@EXEC@|/opt/math-runner/MathRunner.x86_64|' -e 's|@ICON@|math-runner|' \
    > "$PKG/usr/share/applications/math-runner.desktop"
  SIZE=$(du -sk "$PKG/opt" | cut -f1)
  cat > "$PKG/DEBIAN/control" <<EOF
Package: math-runner
Version: $VERSION
Architecture: amd64
Maintainer: IT Kamianets
Installed-Size: $SIZE
Depends: libc6 (>= 2.31), libgl1
Section: games
Priority: optional
Homepage: https://math-runner.itkamianets.com
Description: Math Runner, a 3D street runner where the lane you run into is your answer
 Run down a street and steer into the lane with the correct answer.
EOF
  find "$PKG" -type d -exec chmod 755 {} +
  find "$PKG/opt" -type f -exec chmod 644 {} +
  chmod 755 "$PKG/opt/math-runner/MathRunner.x86_64"
  chmod 644 "$PKG/usr/share/applications/math-runner.desktop" "$PKG/usr/share/icons/hicolor/512x512/apps/math-runner.png"
  dpkg-deb --root-owner-group -Zxz --build "$PKG" "$OUT/MathRunner-Setup.deb" >/dev/null

  # --- tar.gz with install.sh ---------------------------------------------------------------------
  TAR="$WORK/tar/math-runner"
  mkdir -p "$TAR"
  cp -r "$LINUX"/. "$TAR/"
  rm -rf "$TAR"/*_BurstDebugInformation_DoNotShip "$TAR"/*_BackUpThisFolder_ButDontShipItWithYourGame
  cp "$ICON" "$TAR/math-runner.png"
  cat > "$TAR/install.sh" <<'EOF'
#!/usr/bin/env bash
# Installs Math Runner for the current user (no administrator rights). Remove it with: ./install.sh --uninstall
set -euo pipefail
APP="$HOME/.local/share/math-runner"
ENTRY="$HOME/.local/share/applications/math-runner.desktop"
if [ "${1:-}" = "--uninstall" ]; then
  rm -rf "$APP" "$ENTRY" "$HOME/.local/bin/math-runner"
  echo "Math Runner removed."
  exit 0
fi
HERE="$(cd "$(dirname "$0")" && pwd)"
mkdir -p "$APP" "$HOME/.local/share/applications" "$HOME/.local/bin"
cp -r "$HERE"/. "$APP/"
rm -f "$APP/install.sh"
chmod +x "$APP/MathRunner.x86_64"
ln -sf "$APP/MathRunner.x86_64" "$HOME/.local/bin/math-runner"
cat > "$ENTRY" <<DESK
[Desktop Entry]
Type=Application
Name=Math Runner
Comment=Run to the right answer
Exec=$APP/MathRunner.x86_64
Icon=$APP/math-runner.png
Terminal=false
Categories=Game;Education;
DESK
echo "Math Runner installed. Find it in your applications menu, or run: math-runner"
EOF
  chmod 755 "$TAR/install.sh" "$TAR/MathRunner.x86_64"
  tar -C "$WORK/tar" --owner=0 --group=0 -czf "$OUT/MathRunner-Linux.tar.gz" math-runner
  echo "Linux: $OUT/MathRunner-Setup.deb and MathRunner-Linux.tar.gz"
else
  echo "Skipping Linux: Builds/Linux/MathRunner.x86_64 not found"
fi

# --- macOS -----------------------------------------------------------------------------------------
if [ -d "$ROOT/Builds/macOS/MathRunner.app" ]; then
  MAC="$WORK/mac"
  mkdir -p "$MAC"
  cp -r "$ROOT/Builds/macOS/MathRunner.app" "$MAC/Math Runner.app"
  # A Windows copy loses the executable bit; the app will not start without it.
  find "$MAC/Math Runner.app/Contents/MacOS" -type f -exec chmod 755 {} +
  rm -f "$OUT/MathRunner-macOS.zip"
  (cd "$MAC" && zip -qry "$OUT/MathRunner-macOS.zip" "Math Runner.app")
  echo "macOS: $OUT/MathRunner-macOS.zip"
else
  echo "Skipping macOS: Builds/macOS/MathRunner.app not found"
fi
