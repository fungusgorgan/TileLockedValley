# A helper script to package the mod for release.
# Usage: ./package.sh <version> [--release]
#   --release  also creates a GitHub Release (tag v<version> on main) with the zip attached,
#              which triggers the workflow that uploads it to Nexus Mods and CurseForge.
set -e

if [ -z "$1" ]; then
  echo "Usage: ./package.sh <version> [--release]" >&2
  exit 1
fi

if ! grep -q "\"Version\": \"$1\"" TileLocked/manifest.json; then
  echo "TileLocked/manifest.json does not have \"Version\": \"$1\". Update it first." >&2
  exit 1
fi

ZIP="TileLocked/bin/Debug/net6.0/TileLocked $1.zip"

dotnet build
rm -rf tmp
mkdir tmp
unzip "$ZIP" -d tmp
rm "$ZIP"
cd tmp/TileLocked
zip -r "../../$ZIP" *
cd ../..
rm -r tmp

if [ "$2" = "--release" ]; then
  gh release create "v$1" "$ZIP" --target main --title "v$1"
fi
