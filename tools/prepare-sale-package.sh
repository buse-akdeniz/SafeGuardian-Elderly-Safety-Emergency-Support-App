#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
OUT_DIR="$ROOT_DIR/dist/vitaguard-sale-package"
ZIP_NAME="vitaguard-sale-package-$(date +%Y%m%d).zip"

rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

copy_path() {
  local source="$1"
  local target="$2"
  mkdir -p "$(dirname "$target")"
  cp -R "$source" "$target"
}

# Core backend files
copy_path "$ROOT_DIR/AsistanApp/AsistanApp.csproj" "$OUT_DIR/AsistanApp/AsistanApp.csproj"
copy_path "$ROOT_DIR/AsistanApp/Program.cs" "$OUT_DIR/AsistanApp/Program.cs"
copy_path "$ROOT_DIR/AsistanApp/Controllers" "$OUT_DIR/AsistanApp/Controllers"
copy_path "$ROOT_DIR/AsistanApp/Services" "$OUT_DIR/AsistanApp/Services"
copy_path "$ROOT_DIR/AsistanApp/Data" "$OUT_DIR/AsistanApp/Data"
copy_path "$ROOT_DIR/AsistanApp/Models" "$OUT_DIR/AsistanApp/Models"
copy_path "$ROOT_DIR/AsistanApp/Hubs" "$OUT_DIR/AsistanApp/Hubs"
copy_path "$ROOT_DIR/AsistanApp/wwwroot" "$OUT_DIR/AsistanApp/wwwroot"

if [[ -d "$ROOT_DIR/AsistanApp/Pages" ]]; then
  copy_path "$ROOT_DIR/AsistanApp/Pages" "$OUT_DIR/AsistanApp/Pages"
fi

# Configuration
copy_path "$ROOT_DIR/AsistanApp/appsettings.json" "$OUT_DIR/AsistanApp/appsettings.json"
copy_path "$ROOT_DIR/AsistanApp/appsettings.Development.json" "$OUT_DIR/AsistanApp/appsettings.Development.json"
copy_path "$ROOT_DIR/AsistanApp/appsettings.Staging.json" "$OUT_DIR/AsistanApp/appsettings.Staging.json"
copy_path "$ROOT_DIR/AsistanApp/appsettings.Production.json" "$OUT_DIR/AsistanApp/appsettings.Production.json"
copy_path "$ROOT_DIR/AsistanApp/.env.example" "$OUT_DIR/AsistanApp/.env.example"

# Docs for buyer
copy_path "$ROOT_DIR/README.md" "$OUT_DIR/README.md"
copy_path "$ROOT_DIR/SATIS_PAKETI" "$OUT_DIR/SATIS_PAKETI"

# Safety cleanup (in case copied indirectly)
rm -rf \
  "$OUT_DIR/AsistanApp/bin" \
  "$OUT_DIR/AsistanApp/obj" \
  "$OUT_DIR/AsistanApp/node_modules" \
  "$OUT_DIR/AsistanApp/ios"

rm -f \
  "$OUT_DIR/AsistanApp/asistanapp.db" \
  "$OUT_DIR/AsistanApp/app.log" \
  "$OUT_DIR/AsistanApp/.env"

(
  cd "$ROOT_DIR/dist"
  rm -f "$ZIP_NAME"
  zip -qr "$ZIP_NAME" "vitaguard-sale-package"
)

echo "Sale package hazır: $OUT_DIR"
echo "Zip dosyası hazır: $ROOT_DIR/dist/$ZIP_NAME"
