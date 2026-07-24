import { chromium } from 'playwright';
import { spawn } from 'node:child_process';
import { mkdirSync } from 'node:fs';
import { setTimeout as delay } from 'node:timers/promises';

const ROOT = '/Users/busenurakdeniz/Desktop/ilk projem/AsistanApp/wwwroot/elderly-ui';
const OUT = '/Users/busenurakdeniz/Desktop/ilk projem/docs/screenshots/appstore-2026-07-17';
const PORT = 5173;
const URL = `http://127.0.0.1:${PORT}/index.html`;

mkdirSync(OUT, { recursive: true });

const screens = [
  { key: 'home', action: async (page) => { await page.evaluate(() => { window.showScreen('homeScreen'); }); } },
  { key: 'medications', action: async (page) => { await page.evaluate(() => { window.showScreen('medicationScreen'); }); } },
  { key: 'mood', action: async (page) => { await page.evaluate(() => { window.showScreen('moodScreen'); }); } },
  { key: 'emergency', action: async (page) => {
      await page.evaluate(() => {
        window.showScreen('homeScreen');
        window.showEmergencyConfirm();
      });
    }
  },
  { key: 'family', action: async (page) => { await page.evaluate(() => { window.showScreen('familyScreen'); }); } }
];

const devices = [
  { name: 'iphone-69', width: 1320, height: 2868 },
  { name: 'ipad-13', width: 2064, height: 2752 }
];

function startServer() {
  const proc = spawn('python3', ['-m', 'http.server', String(PORT), '--bind', '127.0.0.1'], {
    cwd: ROOT,
    stdio: 'ignore'
  });
  return proc;
}

async function primeApp(page) {
  await page.goto(URL, { waitUntil: 'domcontentloaded' });
  await page.evaluate(() => {
    localStorage.setItem('appLang', 'tr');
    localStorage.setItem('rememberMe', 'true');
    localStorage.setItem('token', 'screenshot-store-token');
    localStorage.setItem('userName', 'Ayşe Yılmaz');
    localStorage.setItem('voiceOnboardingDone', 'true');
    localStorage.setItem('showDemoHint', 'false');
    sessionStorage.removeItem('offlineDemoMode');
  });
  await page.reload({ waitUntil: 'networkidle' });
  await page.evaluate(() => {
    window.showScreen('homeScreen');
    document.getElementById('testHint')?.remove();
    document.getElementById('gracefulOfflineState')?.remove();
    document.getElementById('voiceOnboarding')?.classList.remove('active');
    document.querySelectorAll('.voice-onboarding.active').forEach((el) => el.classList.remove('active'));
    if (typeof window.applyTranslations === 'function') {
      window.applyTranslations();
    }
  });
  await delay(900);
}

const server = startServer();

try {
  await delay(800);
  const browser = await chromium.launch({ headless: true });

  for (const device of devices) {
    const context = await browser.newContext({
      viewport: { width: device.width, height: device.height },
      deviceScaleFactor: 1,
      isMobile: true,
      hasTouch: true,
      locale: 'tr-TR'
    });

    const page = await context.newPage();
    await primeApp(page);

    for (const screen of screens) {
      await screen.action(page);
      await page.evaluate(() => {
        document.getElementById('gracefulOfflineState')?.remove();
        document.getElementById('voiceOnboarding')?.classList.remove('active');
      });
      await delay(700);
      await page.screenshot({
        path: `${OUT}/${device.name}-${screen.key}.png`,
        fullPage: false
      });
    }

    await context.close();
  }

  await browser.close();
  console.log('Screenshots generated in:', OUT);
} finally {
  server.kill('SIGTERM');
}
