#!/usr/bin/env zsh
set -euo pipefail

OUT_DIR="/Users/busenurakdeniz/Desktop/ilk projem/docs/screenshots/review-2026-05-07"
APP_ID="com.buse.safeguardian"
IPAD_ID="CFBD62FB-3CCA-4E77-A2A5-0C2AD434DDA0"      # iPad Air 11-inch (M3)
IPHONE_ID="7DEF6900-9A2E-4A2B-99B3-4504FA020918"   # iPhone 17 Pro Max

mkdir -p "$OUT_DIR"

boot_and_launch() {
  echo "⏳ Simülatörler başlatılıyor..."
  xcrun simctl boot "$IPAD_ID" >/dev/null 2>&1 || true
  xcrun simctl boot "$IPHONE_ID" >/dev/null 2>&1 || true
  open -a Simulator >/dev/null 2>&1 || true
  sleep 2

  echo "🚀 Uygulama açılıyor..."
  xcrun simctl launch "$IPAD_ID" "$APP_ID" >/dev/null || true
  xcrun simctl launch "$IPHONE_ID" "$APP_ID" >/dev/null || true
  sleep 2
}

capture_pair() {
  local label="$1"
  local ipad_path="$OUT_DIR/ipad-${label}.png"
  local iphone_path="$OUT_DIR/iphone-${label}.png"

  xcrun simctl io "$IPAD_ID" screenshot "$ipad_path" >/dev/null
  xcrun simctl io "$IPHONE_ID" screenshot "$iphone_path" >/dev/null
  echo "✅ Kaydedildi: ipad-${label}.png + iphone-${label}.png"
}

print_step() {
  local label="$1"
  local tr_name="$2"
  echo ""
  echo "➡️  ${tr_name} ekranına geç ve hazır olunca Enter bas: (${label})"
}

boot_and_launch

echo ""
echo "================ APP STORE SCREENSHOT CAPTURE ================"
echo "Her adımda iki cihazda da AYNI ekran açık olsun, sonra Enter'a bas."
echo "Çıkış klasörü: $OUT_DIR"
echo "=============================================================="

print_step "home" "Ana ekran"
read -r
capture_pair "home"

print_step "medications" "İlaçlar ekranı"
read -r
capture_pair "medications"

print_step "mood" "Ruh hali ekranı"
read -r
capture_pair "mood"

print_step "emergency" "Acil destek ekranı"
read -r
capture_pair "emergency"

print_step "family" "Aile ekranı"
read -r
capture_pair "family"

echo ""
echo "🎉 Tüm screenshot'lar hazır."
echo "📁 $OUT_DIR"
ls -1 "$OUT_DIR" | sed 's/^/ - /'
