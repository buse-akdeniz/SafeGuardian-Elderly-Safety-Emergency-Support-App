# AdMob Yayın Kontrol Listesi

## 1. Policy Center
- AdMob panelinde `Policy center` bölümünü aç.
- `Issues found`, `Ad serving restriction`, `Account issue` veya `App issue` uyarısı olmadığını doğrula.
- Herhangi bir ihlal varsa uygulamayı göndermeden önce çöz.

## 2. Ad Serving
- AdMob panelinde hesabın reklam sunum durumunu kontrol et.
- Durum `Limited` olmamalı.
- Hedef durum: reklam sunumu aktif ve hesapta kısıtlama olmaması.

## 3. Uygulama Durumu
- Apple onayı geldikten sonra AdMob içindeki uygulamaya gerçek App Store linkini ekle.
- Uygulama durumu `Ready` olana kadar reklam geliri tam sağlıklı ilerlemeyebilir.

## 4. Gerçek Reklam Kimlikleri
Aşağıdaki yerlerde test reklam kimlikleri var; yayına çıkmadan önce gerçek kimliklerle değiştir:
- `AsistanApp/wwwroot/js/admob.js`
- `AsistanApp/ios/App/App/Info.plist`

## 5. app-ads.txt
- Dosya hazır: `AsistanApp/wwwroot/app-ads.txt`
- `pub-XXXXXXXXXXXXXXXX` kısmını gerçek yayıncı kimliğinle değiştir.
- Support URL olarak verdiğin domainde şu adreste erişilebilir olmalı:
  - `https://senin-domainin.com/app-ads.txt`
- Yayına almadan sonra tarayıcıdan açılıp açılmadığını kontrol et.

## 6. Son Kontrol
- iOS build al
- Gerçek cihazda test reklam + ödüllü reklam akışını dene
- 7 günlük trial bitince abonelik/reklam kilidi mantığını tekrar kontrol et
- Privacy Policy içinde reklam ve ATT açıklamasının durduğunu doğrula

## Not
Bu repo tarafında reklam altyapısı hazırlandı; AdMob paneli, politika durumu ve gelir aktivasyonu yalnızca AdMob hesabından manuel kontrol edilebilir.
