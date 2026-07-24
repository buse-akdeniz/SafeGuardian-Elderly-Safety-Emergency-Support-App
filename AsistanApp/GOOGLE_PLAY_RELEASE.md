# SafeGuardian — Google Play yayını

Paket: `com.buse.safeguardian` (iOS ile aynı)

## 1) Play Console

1. https://play.google.com/console → **Uygulama oluştur**
2. Uygulama adı: **SafeGuardian AI**
3. Gizlilik politikası URL (zorunlu) — web sitende yayında olmalı
4. **app-ads.txt** — sitende: `google.com, pub-8847006171122424, DIRECT, f08c47fec0942fa0`

## 2) AdMob Android

1. AdMob → Uygulama ekle → **Android** → `com.buse.safeguardian`
2. Banner + Rewarded birim ID’lerini al
3. `android/app/src/main/res/values/strings.xml` → `admob_app_id`
4. `wwwroot/js/admob.js` → `ANDROID_ADS` içine gerçek birimler
5. `isTesting: false` yap (prod için)

## 3) İmzalama (keystore)

```bash
cd android
keytool -genkey -v -keystore safeguardian-release.keystore -alias safeguardian -keyalg RSA -keysize 2048 -validity 10000
cp keystore.properties.example keystore.properties
# keystore.properties içini doldur
```

Play Console → **App integrity** → Upload key / App signing (Google yönetimini önerir).

## 4) AAB üret

```bash
cd "/Users/busenurakdeniz/Desktop/ilk projem/AsistanApp"
npm run cap:sync:android
npm run android:bundle
```

Çıktı: `android/app/build/outputs/bundle/release/app-release.aab`

## 5) Play’e yükle

Play Console → **Production** veya **Internal testing** → **Create release** → AAB yükle.

## 6) Store listesi

- Ekran görüntüleri (telefon)
- Kısa / uzun açıklama (TR + EN)
- İçerik derecelendirmesi anketi
- Veri güvenliği formu (konum, kamera, reklam)
- Hedef kitle / aile politikası (çocuk verisi varsa dikkat)

## Notlar

- **Android’de Apple IAP yok** — uygulama reklam + ücretsiz özelliklerle çıkar; abonelik sonra Google Play Billing ile eklenebilir.
- İlk yayın için **Internal testing** ile 12 testçi ekle, sonra Production.
