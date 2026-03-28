using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

await RunAssistantAsync();

async Task RunAssistantAsync()
{
    string dosyaYolu = "ajanda.txt";
    var rutinler = new List<(string Saat, string Mesaj)>();
    var processedTimes = new HashSet<string>(); // Aynı saatte tekrar gönderme engeli

    try
    {
        // 1. Verileri Yükle (Load Data)
        if (File.Exists(dosyaYolu))
        {
            try
            {
                string[] satirlar = File.ReadAllLines(dosyaYolu);
                foreach (var satir in satirlar)
                {
                    if (string.IsNullOrWhiteSpace(satir)) continue;

                    var parcalar = satir.Split('|');
                    if (parcalar.Length == 2)
                    {
                        string saat = parcalar[0].Trim();
                        string mesaj = parcalar[1].Trim();

                        // Saat formatını valide et
                        if (ValidateTimeFormat(saat))
                        {
                            rutinler.Add((saat, mesaj));
                            Console.WriteLine($"✓ Yüklendi: {saat} - {mesaj}");
                        }
                        else
                        {
                            Console.WriteLine($"⚠ Geçersiz saat: {saat} (HH:mm formatı bekleniyor)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Dosya okuma hatası: {ex.Message}");
            }
        }

        Console.WriteLine("\n--- GÖRSEL VE SESLİ ASİSTAN (ASENKRON) ---");

        // 2. Yeni Rutin Ekleme (Add Section)
        while (true)
        {
            Console.WriteLine("\nEkle: 'e' | Takibi Başlat: 'h' | Çıkış: 'q'");
            string cevap = Console.ReadLine()?.ToLower() ?? "";

            if (cevap == "q")
            {
                Console.WriteLine("Programdan çıkılıyor...");
                return;
            }

            if (cevap == "h") break;

            if (cevap == "e")
            {
                Console.Write("Saat (HH:mm formatı, örn: 13:00): ");
                string saat = Console.ReadLine()?.Trim() ?? "";

                if (!ValidateTimeFormat(saat))
                {
                    Console.WriteLine("✗ Geçersiz saat formatı! HH:mm formatını kullanınız (13:00)");
                    continue;
                }

                Console.Write("Mesaj (İlaç vakti): ");
                string mesaj = Console.ReadLine()?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(mesaj))
                {
                    Console.WriteLine("✗ Mesaj boş olamaz!");
                    continue;
                }

                try
                {
                    rutinler.Add((saat, mesaj));
                    File.AppendAllLines(dosyaYolu, new[] { $"{saat}|{mesaj}" });
                    Console.WriteLine($"✓ Rutin eklendi: {saat} - {mesaj}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Dosya yazma hatası: {ex.Message}");
                }
            }
        }

        // 3. Takip ve Bildirim (Tracking and Notification - Asenkron)
        Console.WriteLine("\n🟢 Asistanınız arka planda çalışıyor...");
        Console.WriteLine("Durma için CTRL+C basınız.\n");

        var lastCheckTime = DateTime.Now;

        while (true)
        {
            try
            {
                string suankiSaat = DateTime.Now.ToString("HH:mm");

                // Her dakikada bir kontrol yap
                if ((DateTime.Now - lastCheckTime).TotalSeconds >= 1)
                {
                    lastCheckTime = DateTime.Now;

                    foreach (var rutin in rutinler)
                    {
                        // Aynı saatte tekrar gönderme engeli
                        if (rutin.Saat == suankiSaat && !processedTimes.Contains(suankiSaat))
                        {
                            Console.WriteLine($"\n⏰ [{DateTime.Now:HH:mm:ss}] BİLDİRİM GÖNDERİLİYOR: {rutin.Mesaj}");

                            // Asenkron görevleri paralel çalıştır
                            await Task.WhenAll(
                                SendVoiceNotificationAsync(rutin.Mesaj),
                                SendVisualNotificationAsync(rutin.Mesaj)
                            );

                            processedTimes.Add(suankiSaat);
                            Console.WriteLine($"✓ Bildirim tamamlandı.\n");

                            // 61 saniye bekle (asenkron - program dondurmuyor)
                            await Task.Delay(61000);
                        }
                    }

                    // Saat değişirse processedTimes'ı sıfırla
                    if (DateTime.Now.Minute == 0 && DateTime.Now.Second < 2)
                    {
                        processedTimes.Clear();
                    }
                }

                // CPU'yu aşırı kullanmamak için kısa bekle
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Bildirim gönderi hatası: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Kritik hata: {ex.Message}");
    }
}

// Saat formatını valide et (HH:mm)
bool ValidateTimeFormat(string timeString)
{
    return DateTime.TryParseExact(timeString, "HH:mm", null, System.Globalization.DateTimeStyles.None, out _);
}

// Sesli Bildirim (Asenkron)
async Task SendVoiceNotificationAsync(string message)
{
    try
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "say",
            Arguments = message,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            if (process != null)
            {
                await Task.Run(() => process.WaitForExit(10000)); // Max 10 saniye
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Ses bildirim hatası: {ex.Message}");
    }
}

// Görsel Bildirim - AppleScript (Asenkron)
async Task SendVisualNotificationAsync(string message)
{
    try
    {
        // Tırnak işaretlerini escape et
        string escapedMessage = message.Replace("\"", "\\\"");

        string appleScript = $"display notification \"{escapedMessage}\" with title \"🏥 Asistan Hatırlatması\" sound name \"Crystal\"";

        var processInfo = new ProcessStartInfo
        {
            FileName = "osascript",
            Arguments = $"-e '{appleScript}'",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (var process = Process.Start(processInfo))
        {
            if (process != null)
            {
                await Task.Run(() => process.WaitForExit(5000)); // Max 5 saniye
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Görsel bildirim hatası: {ex.Message}");
    }
}