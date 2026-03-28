// Service Worker for Offline Support
const CACHE_NAME = 'safeguardian-v2';
const urlsToCache = [
    '/',
    '/index.html',
    '/index-elderly-ui.html',
    '/index-elderly-ui-v2.html',
    '/privacy-policy.html',
    '/css/site.css',
    '/js/site.js',
    '/js/i18n.js',
    '/js/ai-emergency.js',
    '/js/signalr.min.js',
    '/favicon.ico'
];

// Offline fallback HTML sayfası
const OFFLINE_FALLBACK = `
<!DOCTYPE html>
<html lang="tr">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Çevrimdışı Mod - SafeGuardian</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
            background: linear-gradient(135deg, #0f0f1e 0%, #1a1a3a 100%);
            color: white;
            height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
        }
        .offline-box {
            text-align: center;
            max-width: 500px;
            background: rgba(255, 255, 255, 0.05);
            border-radius: 20px;
            padding: 40px;
            border: 2px solid #ffd700;
        }
        .offline-icon {
            font-size: 80px;
            margin-bottom: 20px;
        }
        h1 {
            font-size: 32px;
            margin-bottom: 10px;
            color: #ffd700;
        }
        p {
            font-size: 18px;
            line-height: 1.6;
            opacity: 0.9;
            margin-bottom: 20px;
        }
        .status {
            padding: 15px;
            background: rgba(255, 215, 0, 0.1);
            border-left: 4px solid #ffd700;
            text-align: left;
            border-radius: 8px;
            margin-top: 20px;
            font-size: 14px;
        }
        .retry-btn {
            margin-top: 30px;
            padding: 15px 40px;
            background: #1e90ff;
            color: white;
            border: none;
            border-radius: 12px;
            font-size: 18px;
            cursor: pointer;
            font-weight: bold;
        }
        .retry-btn:hover {
            background: #0047ab;
        }
    </style>
</head>
<body>
    <div class="offline-box">
        <div class="offline-icon">📡</div>
        <h1>Çevrimdışı Mod</h1>
        <p>
            İnternet bağlantınız bulunmamaktadır. SafeGuardian yerel modda çalışıyor.
        </p>
        <p style="font-size: 16px; opacity: 0.7;">
            Görevleriniz ve sağlık bilgileriniz cihazda saklanıyor. 
            Bağlantı geri geldiğinde otomatik olarak senkronize olacaktır.
        </p>
        <div class="status">
            ✓ Yerel veri depolaması aktif<br>
            ✓ Sesli komutlar çalışıyor<br>
            ✓ Görevler kaydediliyor
        </div>
        <button class="retry-btn" onclick="location.reload()">⟲ Sayfayı Yenile</button>
    </div>
</body>
</html>
`;

// Install event - Cache files
self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            console.log('Service Worker: Caching files...');
            return cache.addAll(urlsToCache).catch(err => {
                console.warn('Some files could not be cached:', err);
            });
        })
    );
    self.skipWaiting();
});

// Activate event - Clean old caches
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cacheName => {
                    if (cacheName !== CACHE_NAME) {
                        console.log('Service Worker: Deleting old cache:', cacheName);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
    self.clients.claim();
});

// Fetch event - Serve from cache, fallback to network
self.addEventListener('fetch', event => {
    const { request } = event;
    const url = new URL(request.url);

    // Don't cache API calls
    if (url.pathname.startsWith('/api/')) {
        event.respondWith(
            fetch(request)
                .then(response => response)
                .catch(() => {
                    // Return offline response for API calls
                    return new Response(JSON.stringify({
                        offline: true,
                        message: 'Offline mode - data will sync when online'
                    }), {
                        status: 503,
                        statusText: 'Service Unavailable',
                        headers: new Headers({
                            'Content-Type': 'application/json'
                        })
                    });
                })
        );
        return;
    }

    // Cache first, then network for static files
    event.respondWith(
        caches.match(request).then(response => {
            if (response) {
                return response;
            }
            
            return fetch(request).then(response => {
                // Don't cache if not a success response
                if (!response || response.status !== 200 || response.type !== 'basic') {
                    return response;
                }

                // Clone the response
                const responseToCache = response.clone();
                caches.open(CACHE_NAME).then(cache => {
                    cache.put(request, responseToCache);
                });

                return response;
            }).catch(() => {
                // Offline fallback - HTML sayfası döndür
                return new Response(OFFLINE_FALLBACK, {
                    status: 503,
                    statusText: 'Service Unavailable',
                    headers: new Headers({
                        'Content-Type': 'text/html; charset=utf-8'
                    })
                });
            });
        })
    );
});

// Background sync for pending actions
self.addEventListener('sync', event => {
    if (event.tag === 'sync-pending-actions') {
        event.waitUntil(
            // Pending actions'ları sunucuya gönder
            (async () => {
                try {
                    const clients = await self.clients.matchAll();
                    if (clients.length > 0) {
                        clients[0].postMessage({
                            type: 'SYNC_PENDING_ACTIONS',
                            timestamp: new Date().toISOString()
                        });
                        console.log('Service Worker: Syncing pending actions');
                    }
                } catch (err) {
                    console.error('Service Worker sync error:', err);
                }
            })()
        );
    }
});

// Push notifications
self.addEventListener('push', event => {
    if (!event.data) return;
    
    try {
        const data = event.data.json();
        const options = {
            body: data.message || 'Yeni bildirim',
            icon: '/favicon.ico',
            tag: data.tag || 'notification',
            requireInteraction: data.requireInteraction || data.severity === 'high' || false,
            badge: '/favicon.ico',
            vibrate: [200, 100, 200],
            data: data.data || {}
        };

        // Acil durum ise ekstra notifikasyon
        if (data.severity === 'high') {
            options.tag = 'emergency-' + Date.now();
            options.requireInteraction = true;
            options.vibrate = [500, 200, 500, 200, 500];
        }

        event.waitUntil(
            self.registration.showNotification(data.title || '🏥 SafeGuardian', options)
        );
    } catch (err) {
        console.error('Service Worker push notification error:', err);
    }
});

// Notification click handler
self.addEventListener('notificationclick', event => {
    event.notification.close();
    event.waitUntil(
        clients.matchAll({ type: 'window' }).then(clientList => {
            for (let i = 0; i < clientList.length; i++) {
                const client = clientList[i];
                if (client.url === '/' && 'focus' in client) {
                    return client.focus();
                }
            }
            if (clients.openWindow) {
                return clients.openWindow('/');
            }
        })
    );
});

console.log('Service Worker loaded and ready for offline support - v2 with improved offline handling');
