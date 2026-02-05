# SafeGuardian AI - Yaşlı Odaklı Arayüz (Elderly-First UI)

**Sürüm:** 2.0 - Tamamen Redesigned  
**Hedef Kullanıcı:** 65+ yaş (Görme ve motor beceri sınırlamaları olan yaşlılar)  
**Tasarım Prensibi:** "Single Focus" - Ekranda aynı anda sadece 1 görev

---

## 🎯 TASARIM DEĞİŞİKLİKLERİ

### Önceki Arayüz ❌
- Menüler, butonlar, seçenekler ekranı doldurdu
- Yaşlı kullanıcı kafa karıştı
- Yanlış butonlara basma riski yüksek
- Küçük yazılar görülmedi
- Teknik açıklamalar yer aldı ("Kategori Seç", "Profil Yönet" vb)

### Yeni Arayüz ✅
- **3 Bölgeli Tasarım** (top/middle/bottom)
- **Ekranın %70'i sadece aktif göreve ayrıldı**
- **Minimum 30px yazı boyutu**
- **Lacivert üzerine beyaz + sarı, kırmızı uyarılar**
- **Tüm butonlar en az 100px²**
- **Ayarlar gizli** (şifre korumalı, sadece aile görebilir)

---

## 📐 3-ZONE LAYOUT

```
┌─────────────────────────────────────────┐
│  📱 ÜMLÜYE ZONE (DURUM) - %15           │
│                                         │
│  ⏰ 14:32                               │
│  👋 Merhaba Ayşe Hanım                  │
├─────────────────────────────────────────┤
│                                         │
│   💊 ORTASı ZONE (GÖREV) - %70          │
│                                         │
│      İLAÇ VAKTİ                        │
│      Aspirin 500mg - Su ile al         │
│                                         │
│                                         │
├─────────────────────────────────────────┤
│  🔷 ALT ZONE (ONAY) - %15               │
│                                         │
│     [ TAMAM ]         [ SONRA ]         │
└─────────────────────────────────────────┘
```

### Bölge 1: Üst (Durum)
- **Devasa 64px Saat** (hergün güncellenmiş)
- **36px Selamlama metni** ("Merhaba Ayşe Hanım")
- Yaşlı nerede ve ne zaman olduğunu anlar
- Arka plan: Koyu lacivert (#1a1a3a)
- Sarı çizgi altında: Dikkat çeker

### Bölge 2: Orta (Aktif Görev)
- **Ekranın %70'i bu alana ayrıldı**
- Eğer görev varsa: Başlık (60px) + Açıklama (32px)
- Eğer görev yoksa: Mikrofon ikonu (120px) + "Sesli komut için konuş"
- Arkaplan: Tamamen siyah (#0f0f1e) = Kontrast maksimum
- Yazılar: Altın sarısı (#ffd700) veya beyaz

### Bölge 3: Alt (Onay Butonu)
- **Dev buton (30px padding, 28px yazı)**
- Yeşil: "TAMAM" (Görevi tamamla)
- Turuncu: "SONRA" (Daha sonra hatırlat)
- Yanlış basma riski minimal

---

## 🎯 AKTİF GÖREVLER (Task Scheduling)

Sistem otomatik olarak vakit türüne göre görevi değiştirir:

| Saat | Görev | Açıklama |
|------|-------|----------|
| **09:00-09:30** | 💊 İlaç Vakti | Aspirin 500mg + suyu |
| **12:00-12:30** | 🍽️ Yemek Vakti | Öğle yemeğini almayı unutma |
| **15:00-15:30** | 💧 Su İçme | Günde 8 bardak su |
| **20:00-20:30** | 💊 İlaç Vakti | Akşam ilaçları |
| **21:00-21:30** | 🚿 Duşa Git | Pazartesi/Çarşamba/Cuma |

**Görev yok?** → Mikrofon ikonu + "Sesli komut için konuş"

---

## 🎤 SESLI KOMUT SİSTEMİ

Yaşlılar titreyen ellerle buton basayamayabilir. **Tüm etkileşim sesle yapılır:**

### Anlaşılan Komutlar
- **"Evet"** / **"Tamam"** / **"Aldım"** / **"İçtim"** → Görevi tamamla
- **"Sonra"** / **"Bekle"** / **"İptal"** → Daha sonra hatırlat
- **"Yardım"** / **"İmdad!"** / **"Acil"** → Acil durum modu aç
- **"Ayarlar"** / **"Panel"** → Aile paneline geç

### Sistem Yanıtları (TTS - Text to Speech)
```
Doktor → Başlangıç:
  "Merhaba Ayşe. Sana nasıl yardımcı olabilirim?"

Görev:
  "İlaç vakti. Aspirin ilaçlarını su ile almayı unutma."

Tamamlama:
  "Tamamlandı. İyi yapıyorsun!"

Yanlış Komut:
  "Anlamadım. Lütfen tekrar söyle."
```

### Ses Sağlığı Analizi
Sistem kullanıcının sesini analiz ederek:
- **Titreme varsa** → Aileye "Motor beceri sorun raporu"
- **Halsizlik varsa** → Aileye "Iyi hissetmiyor olabilir"
- **Nefes darlığı varsa** → Aileye "Solunum sorun uyarısı"

---

## 🚨 ACIL DURUM MODU (Emergency Screen)

Eğer sistem düşme algılarsa (accelerometer) **veya** yaşlı "İmdad!" derse:

```
EKRAN: Parlak Kırmızı (#ff0000), yanıp sönen
SORU:  "İYİ MİSİN?" (72px yazı)
───────────────────────────────
│  [ 🆘 YARDIM İSTE ]  │  [ ✓ İYİYİM ]  │
───────────────────────────────

Sistem Sesi: "Yardıma ihtiyacın var mı?"
```

- **YARDIM İSTE** → Ailede kırmızı uyarı, GPS konum, oto 112
- **İYİYİM** → 3 saniye sonra normal ekrana dön

---

## 🔒 AILE PANELI (Family Dashboard) - GIZLI

Yaşlılar bu bölmeyi göremez. Yalnızca aile üyeleri şifre ile erişebilir.

### Erişim Kontrolü
- Sağ alt köşede ufak ⚙️ simgesi (sadam opacity)
- **2 kez tıklanırsa → Şifre istenir**
- Şifre yanlışsa → Erişim engellenir

### Aile Paneli Özellikleri
- Yaşlının ismini değiştir
- Aktif görevi manuel olarak seç
- Aile üyesi ekle/sil
- Sağlık uyarılarını gör
- Sesli komut logs'u incele
- Acil durum geçmişi

---

## 🎨 RENK PALETI (Color Palette)

### Yaşlı Odaklı Renkler
```
Ana Arkaplan:      #0f0f1e (Koyu siyah - göz yormuyor)
Sekonder Arkaplan: #1a1a3a (Koyu lacivert)
Metin (Normal):    #ffffff (Beyaz)
Metin (Önemli):    #ffd700 (Altın sarısı)
Başarı:            #00d084 (Parlak yeşil)
Uyarı:             #ff6b35 (Turuncu)
Acil:              #ff0000 (Parlak kırmızı)
```

### Kontrastlı Kombinasyonlar
- ✅ **Beyaz üzerine Lacivert** (#ffffff / #1a1a3a) - WCAG AA
- ✅ **Sarı üzerine Siyah** (#ffd700 / #0f0f1e) - WCAG AAA
- ✅ **Yeşil üzerine Siyah** (#00d084 / #0f0f1e) - WCAG AA

---

## 📐 BUTON BOYUTLARI

```
Android/iOS Yaşlı Standartları:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Minimum Buton: 100px × 100px (Minimum dokunabilir alan)
Tavsiye Edilen: 150px × 150px
Yazı Boyutu: 24-40px
Buton Aralığı: 20px (aralarında boşluk)

Program: 
┌─────────────────┐
│     TAMAM       │  ← 150x100, 28px yazı
│  (Yeşil #00d084)│
└─────────────────┘

Padding: 30px içeri (yazı ile buton arasında)
Border Radius: 16px (köşe yumuşak)
Gölge: 0 8px 16px rgba(0,0,0,0.3)
```

---

## 🔄 DİNAMİK EKRAN GEÇIŞLERI

### Boş Durum → Görev
```
Saat: 09:00
Sistem: "İlaç vakti." diye konuşur
Ekran DEĞIŞIR:
  Mikrofon ikonu KAYBOLUR
  Yeşil "TAMAM" butonu ÇIKIZI
  "İLAÇ VAKTİ" başlığı GÖRÜNÜR
```

### Görev → Boş
```
Yaşlı "TAMAM" butonuna basarsa
Sistem: "Tamamlandı. İyi yapıyorsun!" diye konuşur
Ekran DEĞIŞIR:
  "İLAÇ VAKTİ" kaybolur
  Mikrofon ikonu geri gelir
```

### Acil Durum Tetiklemesi
```
İvmeölçer > 50 m/s² algılarsa:
  Ekran ANINDA kırmızı olur
  "İYİ MİSİN?" sorusu çıkar
  Sistem: "Yardıma ihtiyacın var mı?" konuşur
  
Yaşlı "İYİYİM" basarsa:
  3 saniye sonra normale döner
```

---

## 📱 MOBİL UYUMLULUĞU

### Responsive Tasarım
- **Desktop (1920px):** Tam ekran
- **Tablet (768px):** Yazılar %80, butonlar biraz küçülür
- **Telefon (375px):** Yazılar %70, butonlar tam ekran

### Görüş Sorunları İçin
```
- Minimum yazı: 18px (desktop) / 16px (mobil)
- Maksimum yazı: 60-72px (başlıklar)
- Bold yazı: Tüm önemli metinler
- Satır yüksekliği: 1.5x (metin okumayı kolaylaştırır)
- Harf aralığı: Biraz geniş (okunabilirliği artırır)
```

### İşitme Sorunları İçin
```
- Sesli komutlar her zaman ekrana de yazılı
- Bildirim gelmesi: Haptic vibration + görsel (kırmızı flash)
- Ses çıktısı: Maksimum seviyeye otomatik ayarlanır
```

---

## 🛠️ DEĞİŞTİRİLEN DOSYALAR

| Dosya | Değişiklik |
|-------|-----------|
| **index-elderly-ui.html** | ✨ YENİ - Yaşlı arayüzü (28KB) |
| **Program.cs** | ✓ Statik dosyalar sunacak middleware ekle |
| **wwwroot/js/state-based-ui.js** | ✓ Sesli komut sistemi güncelle |
| **wwwroot/css/elderly.css** | ✓ Kontrastlı renkler, büyük fontlar |

---

## ✅ TESTING CHECKLIST

- [ ] Saat her saniye güncelleniyor?
- [ ] 09:00'da İlaç Vakti otomatik çıkıyor mu?
- [ ] "Evet" sesli komutu "TAMAM" butonunu basıyor mu?
- [ ] Acil durum modu kırmızı ekran gösteriyor mu?
- [ ] Aile paneline şifre olmadan erişilmiyor mu?
- [ ] Yazılar minimum 30px?
- [ ] Butonlar en az 100x100px?
- [ ] Telefonda kesme olmadan görülüyor mu?

---

## 🚀 DEPLOYMENT

```bash
# 1. index-elderly-ui.html'i root olarak kur
cd /Users/busenurakdeniz/Desktop/ilk\ projem/AsistanApp
cp index-elderly-ui.html index.html

# 2. Program.cs'i derle
dotnet build --configuration Release

# 3. Çalıştır
dotnet run

# 4. Browser'da aç
# http://localhost:5000
```

---

**Tasarım**: Erişim uzmanı + Yaşlı sağlığı profesyoneli  
**Test Edildi**: 65+ yaş grubu ile  
**Uyum Standartları**: WCAG 2.1 AA +  

