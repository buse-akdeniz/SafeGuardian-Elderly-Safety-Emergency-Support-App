using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

string dosyaYolu = "ajanda.txt";
var rutinler = new List<(string Saat, string Mesaj)>();

// 1. Verileri Yükle (Load Data)
if (File.Exists(dosyaYolu))
{
    string[] satirlar = File.ReadAllLines(dosyaYolu);
    foreach (var satir in satirlar)
    {
        var parcalar = satir.Split('|');
        if (parcalar.Length == 2) rutinler.Add((parcalar[0], parcalar[1]));
    }
}

Console.WriteLine("--- GÖRSEL VE SESLİ ASİSTAN ---");

// 2. Yeni Rutin Ekleme (Add Section)
while (true)
{
    Console.WriteLine("\nEkle: 'e' | Takibi Başlat: 'h'");
    string cevap = Console.ReadLine().ToLower();
    if (cevap == "h") break;

    Console.Write("Saat (13:00): ");
    string saat = Console.ReadLine();
    Console.Write("Mesaj (İlaç vakti): ");
    string mesaj = Console.ReadLine();

    rutinler.Add((saat, mesaj));
    File.AppendAllLines(dosyaYolu, new[] { $"{saat}|{mesaj}" });
}

// 3. Takip ve Bildirim (Tracking and Notification)
Console.WriteLine("\nAsistanınız arka planda çalışıyor...");

while (true)
{
    string suankiSaat = DateTime.Now.ToString("HH:mm");
    foreach (var rutin in rutinler)
    {
        if (rutin.Saat == suankiSaat)
        {
            // A. Sesli Bildirim (Voice)
            Process.Start("say", rutin.Mesaj);

            // B. Görsel Bildirim (Visual AppleScript Notification)
            string appleScript = $"display notification \"{rutin.Mesaj}\" with title \"Asistan Hatırlatması\" sound name \"Crystal\"";
            Process.Start("osascript", $"-e '{appleScript}'");

            Console.WriteLine($"\nBİLDİRİM GÖNDERİLDİ: {rutin.Mesaj}");
            Thread.Sleep(61000); 
        }
    }
    Thread.Sleep(1000);
}