# Repository history rewritten — required collaborator actions

Repository geçmişi yeniden yazıldı (gizli dosyalar geçmişten kaldırıldı). Tüm geliştiricilerin yerel kopyalarını senkronize etmeleri gerekiyor.

Hızlı talimatlar (kolay yöntem — temiz yeniden klon):

```bash
# 1) Mevcut kopyayı yedekle (eğer değişiklik varsa)
git status --porcelain
# stash/commit gerektiği durumlarda devam et

# 2) Yeni, temiz klon al
git clone <repo-url>
```

Alternatif: mevcut local repo'yu reset ile güncelle (tüm local değişiklikler kaybolur):

```bash
git fetch origin
git reset --hard origin/main
```

Notlar:
- Eğer `main` dışında bir default branch kullanıyorsan `main` yerine o branch'i kullan.
- Eğer kişisel branch'lerin varsa, önce onları yedeklemelisin (`git branch -m my-branch my-branch-bak`).

Örnek kısa mesaj (Slack / E-posta):

> Dikkat: Repository geçmişi yeniden yazıldı ve hassas `AsistanApp/.env` dosyası geçmişten kaldırıldı. Lütfen yerel kopyanızı yeniden klonlayın veya `git fetch && git reset --hard origin/main` komutlarını çalıştırın. Herhangi bir uncommitted değişikliğiniz varsa önce yedekleyin.
