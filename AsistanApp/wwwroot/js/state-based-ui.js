// ============================================================================
// 🧠 STATE-BASED UI MANAGER - Context-Aware Screen Rendering
// ============================================================================
// This module handles automatic screen switching based on UserState from backend
// The backend automatically updates context based on time of day:
// - 09:00-11:00: medication_time
// - 12:00-14:00: meal_time  
// - 18:00-22:00: water_time
// - Otherwise: home

class StateBasedUIManager {
    constructor(token) {
        this.token = token;
        this.currentState = null;
        this.pollInterval = 5000; // Poll every 5 seconds
        this.apiBase = '/api';
        this.requestTimeoutMs = 4500;
        this.errorCount = 0;
        this.lastSuccessfulFetchAt = null;
        this.connection = null;
        this.syncBridgeInitialized = false;
        
        // Screen configurations for each context
        this.screenConfigs = {
            medication_time: {
                title: '💊 İLAÇ SAATİ',
                subtitle: 'İlacını İçtin mi?',
                primaryButtonText: 'İlacını İçtim',
                primaryButtonSize: '90vh', // 90% of viewport height
                backgroundColor: '#FFE4B5',
                accentColor: '#FF6347'
            },
            meal_time: {
                title: '🍽️ YEMEK SAATİ',
                subtitle: 'Yemek Yedin mi?',
                primaryButtonText: 'Yemek Yedim',
                primaryButtonSize: '90vh',
                backgroundColor: '#E8F5E9',
                accentColor: '#4CAF50'
            },
            water_time: {
                title: '💧 SU SAATİ',
                subtitle: 'Su İçtin mi?',
                primaryButtonText: 'Su İçtim',
                primaryButtonSize: '90vh',
                backgroundColor: '#E1F5FE',
                accentColor: '#2196F3'
            },
            emergency: {
                title: '🚨 ACIL DURUM',
                subtitle: 'Aileniz ile iletişime geçiliyor...',
                primaryButtonText: 'İyi misin?',
                primaryButtonSize: '80vh',
                backgroundColor: '#FFEBEE',
                accentColor: '#F44336'
            },
            home: {
                title: '👋 Hoş Geldin',
                subtitle: 'Sana nasıl yardımcı olabilirim?',
                primaryButtonText: '🎤 Dinle',
                primaryButtonSize: '30vh',
                backgroundColor: '#F5F5F5',
                accentColor: '#2196F3'
            }
        };
    }

    // Start polling backend for state changes
    async startPolling() {
        console.log('🔄 StateBasedUIManager: Polling başladı (5 saniye aralığında)');

        this.initSmartSyncBridge();

        // Real-time SignalR bağlantısı (polling fallback ile birlikte)
        await this.initRealtime();
        
        // Initial fetch
        await this.fetchAndRenderState();
        
        // Then poll every 5 seconds
        this.pollTimer = setInterval(async () => {
            await this.fetchAndRenderState();
        }, this.pollInterval);
    }

    // Fetch current state from backend
    async fetchAndRenderState() {
        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), this.requestTimeoutMs);
            const response = await fetch(`${this.apiBase}/user-state`, {
                headers: {
                    'Authorization': `Bearer ${this.token}`
                },
                signal: controller.signal
            });
            clearTimeout(timeoutId);
            
            if (!response.ok) {
                console.error('❌ UserState fetch failed:', response.status);
                this.handlePollingFailure();
                return;
            }

            const state = await response.json();
            this.errorCount = 0;
            this.lastSuccessfulFetchAt = new Date();
            this.hideOfflineBanner();
            
            // Check if context changed
            if (!this.currentState || this.currentState.currentContext !== state.currentContext) {
                console.log(`🎯 Context değişti: ${state.currentContext}`);
                this.currentState = state;
                
                // Update UI with new context
                this.renderContextScreen(state.currentContext);
            } else {
                // Context unchanged, but update timestamp for activity tracking
                this.currentState = state;
            }
        } catch (error) {
            console.error('❌ State fetch error:', error);
            this.handlePollingFailure(error);
        }
    }

    handlePollingFailure(error) {
        this.errorCount += 1;

        if (this.errorCount >= 2) {
            this.showOfflineBanner();
        }

        if (!this.currentState) {
            // İlk yüklemede hata olursa boş ekran yerine güvenli ana ekran
            this.renderContextScreen('home');
        }

        if (error && error.name === 'AbortError') {
            console.warn('⏱️ UserState isteği zaman aşımına uğradı');
        }
    }

    showOfflineBanner() {
        this.showSyncStatus('📡 İnternet zayıf veya yok. Merak etme, veriler cihazda güvenle saklanır ve internet gelince otomatik gönderilir.', '#FFB300', '#222');
    }

    showSyncStatus(message, bg = '#FFB300', color = '#222') {
        let banner = document.getElementById('state-offline-banner');
        if (!banner) {
            banner = document.createElement('div');
            banner.id = 'state-offline-banner';
            banner.style.position = 'fixed';
            banner.style.top = '12px';
            banner.style.left = '50%';
            banner.style.transform = 'translateX(-50%)';
            banner.style.zIndex = '9999';
            banner.style.background = '#FFB300';
            banner.style.color = '#222';
            banner.style.padding = '10px 16px';
            banner.style.borderRadius = '10px';
            banner.style.fontWeight = 'bold';
            banner.style.boxShadow = '0 2px 10px rgba(0,0,0,0.2)';
            banner.style.maxWidth = '92vw';
            banner.style.fontSize = '22px';
            banner.style.lineHeight = '1.35';
            document.body.appendChild(banner);
        }

        banner.style.background = bg;
        banner.style.color = color;
        banner.textContent = message;
        banner.style.display = 'block';
    }

    hideOfflineBanner() {
        const banner = document.getElementById('state-offline-banner');
        if (banner) {
            banner.style.display = 'none';
        }
    }

    initSmartSyncBridge() {
        if (this.syncBridgeInitialized) return;
        this.syncBridgeInitialized = true;

        if (!('serviceWorker' in navigator)) {
            console.warn('⚠️ Service Worker desteklenmiyor');
            return;
        }

        navigator.serviceWorker.register('/sw.js')
            .then(() => console.log('✅ Service Worker registered for smart sync'))
            .catch(err => console.warn('⚠️ Service Worker register error:', err));

        navigator.serviceWorker.addEventListener('message', (event) => {
            const data = event.data || {};
            if (data.type === 'OFFLINE_DATA_QUEUED') {
                this.showSyncStatus(
                    '📦 İnternet yok ama merak etme, ölçümün kaydedildi. İnternet gelince doktora/aileye otomatik gönderilecek.',
                    '#FFD54F',
                    '#222'
                );
            }
            if (data.type === 'OFFLINE_SYNC_COMPLETED') {
                this.showSyncStatus(
                    `✅ Senkron tamamlandı: ${data.count || 0} kayıt sunucuya gönderildi.`,
                    '#2E7D32',
                    '#fff'
                );
                setTimeout(() => this.hideOfflineBanner(), 5000);
            }
        });

        window.addEventListener('online', async () => {
            this.showSyncStatus(
                '🌐 İnternet geri geldi. Bekleyen sağlık verileri şimdi senkronize ediliyor...',
                '#4CAF50',
                '#fff'
            );

            try {
                const registration = await navigator.serviceWorker.ready;
                const worker = registration.active || navigator.serviceWorker.controller;
                if (worker) {
                    worker.postMessage({ type: 'FORCE_SYNC' });
                }
            } catch (error) {
                console.warn('⚠️ FORCE_SYNC message failed:', error);
            }
        });

        window.addEventListener('offline', () => {
            this.showSyncStatus(
                '📡 İnternet kesildi. Ölçümler güvenle yerelde saklanacak ve bağlantı gelince otomatik gönderilecek.',
                '#FFB300',
                '#222'
            );
        });
    }

    // Render screen based on context
    renderContextScreen(context) {
        const config = this.screenConfigs[context] || this.screenConfigs.home;
        
        console.log(`📱 Ekran Render: ${context}`);
        
        // Clear entire screen
        const app = document.getElementById('app') || document.body;
        app.innerHTML = '';
        app.style.backgroundColor = config.backgroundColor;
        app.style.display = 'flex';
        app.style.flexDirection = 'column';
        app.style.justifyContent = 'center';
        app.style.alignItems = 'center';
        app.style.minHeight = '100vh';
        app.style.fontFamily = 'Arial, sans-serif';
        
        // Create container
        const container = document.createElement('div');
        container.style.textAlign = 'center';
        container.style.padding = '20px';
        container.style.maxWidth = '100%';
        
        // Title
        const title = document.createElement('h1');
        title.textContent = config.title;
        title.style.fontSize = '48px';
        title.style.color = config.accentColor;
        title.style.margin = '0 0 20px 0';
        title.style.fontWeight = 'bold';
        
        // Subtitle
        const subtitle = document.createElement('p');
        subtitle.textContent = config.subtitle;
        subtitle.style.fontSize = '28px';
        subtitle.style.color = '#333';
        subtitle.style.margin = '0 0 40px 0';
        
        // Primary button (HUGE for elderly users)
        const primaryBtn = document.createElement('button');
        primaryBtn.textContent = config.primaryButtonText;
        primaryBtn.style.width = '100%';
        primaryBtn.style.height = config.primaryButtonSize;
        primaryBtn.style.fontSize = '48px';
        primaryBtn.style.padding = '30px';
        primaryBtn.style.backgroundColor = config.accentColor;
        primaryBtn.style.color = 'white';
        primaryBtn.style.border = 'none';
        primaryBtn.style.borderRadius = '20px';
        primaryBtn.style.cursor = 'pointer';
        primaryBtn.style.fontWeight = 'bold';
        primaryBtn.style.touchAction = 'manipulation'; // Prevent double-tap zoom
        primaryBtn.style.userSelect = 'none';
        
        // Hover effect for touch devices
        primaryBtn.addEventListener('touchstart', function() {
            this.style.transform = 'scale(0.95)';
            // Haptic feedback if available
            if (navigator.vibrate) {
                navigator.vibrate(50); // 50ms vibration
            }
        });
        
        primaryBtn.addEventListener('touchend', function() {
            this.style.transform = 'scale(1)';
        });
        
        // Click handler
        primaryBtn.addEventListener('click', async () => {
            console.log(`✅ Primary action triggered: ${context}`);
            
            // For medication/meal/water - mark as completed
            if (context === 'medication_time' || context === 'meal_time' || context === 'water_time') {
                // Show confirmation
                primaryBtn.textContent = '✅ Tamamlandı! Aferin sana!';
                primaryBtn.style.backgroundColor = '#4CAF50';
                primaryBtn.disabled = true;
                
                // Haptic feedback
                if (navigator.vibrate) {
                    navigator.vibrate([100, 50, 100]); // Pattern vibration
                }
                
                // Reset after 2 seconds
                setTimeout(() => {
                    this.renderContextScreen('home');
                }, 2000);
            }
        });
        
        container.appendChild(title);
        container.appendChild(subtitle);
        container.appendChild(primaryBtn);
        
        app.appendChild(container);
        
        // Text-to-speech announcement
        this.announceContextChange(context);
    }

    // Text-to-speech announcement when screen changes
    announceContextChange(context) {
        try {
            if (!('speechSynthesis' in window)) {
                console.warn('⚠️ Speech Synthesis not supported');
                return;
            }
            
            const announcements = {
                medication_time: "İlaç saati geldi. İlacını aldın mı? Büyük butona bas.",
                meal_time: "Yemek zamanı. Yemek yedin mi? Büyük butona bas.",
                water_time: "Su içme saati. Su içtin mi? Büyük butona bas.",
                emergency: "Acil durum! Aileniz ile iletişime geçiliyor.",
                home: "Hoş geldin. Sana nasıl yardımcı olabilirim?"
            };
            
            const text = announcements[context] || announcements.home;
            
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = 'tr-TR'; // Turkish
            utterance.rate = 0.9; // Slower for elderly users
            utterance.pitch = 1;
            utterance.volume = 1;
            
            window.speechSynthesis.cancel(); // Cancel any previous speech
            window.speechSynthesis.speak(utterance);
            
            console.log(`🔊 Announce: ${text}`);
        } catch (error) {
            console.error('❌ TTS error:', error);
        }
    }

    // Stop polling
    stopPolling() {
        if (this.pollTimer) {
            clearInterval(this.pollTimer);
            console.log('⏹️ StateBasedUIManager: Polling durduruldu');
        }
    }

    // Manual context update (for emergency or special cases)
    async setContext(context, priority = 'normal') {
        try {
            const response = await fetch(`${this.apiBase}/user-state`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${this.token}`
                },
                body: JSON.stringify({
                    currentContext: context,
                    screenPriority: priority,
                    isAssistantActive: true
                })
            });
            
            if (response.ok) {
                console.log(`🎯 Context manually set: ${context}`);
                await this.fetchAndRenderState();
            }
        } catch (error) {
            console.error('❌ Set context error:', error);
        }
    }

    async initRealtime() {
        if (!window.signalR) {
            console.warn('⚠️ SignalR script bulunamadı, polling ile devam ediliyor');
            return;
        }

        try {
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl('/health-hub')
                .withAutomaticReconnect()
                .build();

            this.connection.on('ReceiveTaskUpdate', async () => {
                await this.fetchAndRenderState();
            });

            this.connection.on('ReceiveAICritical', async () => {
                await this.fetchAndRenderState();
            });

            this.connection.on('ReceiveEmergencyAlert', async () => {
                await this.fetchAndRenderState();
            });

            this.connection.on('ReceiveEmergencyEscalation', async () => {
                await this.fetchAndRenderState();
            });

            this.connection.on('ReceiveFallDetected', async (payload) => {
                console.log('🚨 ReceiveFallDetected:', payload);
                await this.setContext('emergency', 'emergency');
            });

            await this.connection.start();
            console.log('✅ SignalR gerçek zamanlı bağlantı aktif');
        } catch (error) {
            console.warn('⚠️ SignalR bağlantısı kurulamadı, polling devam ediyor:', error);
        }
    }
}

// ============================================================================
// 🚨 EMERGENCY CONTEXT SETTER
// ============================================================================
// Call this when critical health event detected

async function triggerEmergencyContext(token) {
    const manager = window.uiManager;
    if (manager) {
        await manager.setContext('emergency', 'emergency');
    }
}

// ============================================================================
// 📱 INITIALIZATION (On page load)
// ============================================================================

document.addEventListener('DOMContentLoaded', async () => {
    // Get token from URL or localStorage
    const params = new URLSearchParams(window.location.search);
    const token = params.get('token') || localStorage.getItem('elderly_token');
    
    if (!token) {
        document.body.innerHTML = '<h1>❌ Kimlik doğrulaması gerekli</h1>';
        return;
    }
    
    // Create UI manager
    window.uiManager = new StateBasedUIManager(token);
    
    // Start polling (every 5 seconds context check)
    await window.uiManager.startPolling();
    
    console.log('✅ StateBasedUIManager başlatıldı');
});

// ============================================================================
// 🔊 VOICE COMMAND INTEGRATION
// ============================================================================
// When user speaks "İyi misin?", the system handles it

async function handleVoiceCommand(command, token) {
    const manager = window.uiManager;
    
    if (command.includes('ilaç') || command.includes('ilac')) {
        await manager.setContext('medication_time', 'medication');
    } else if (command.includes('yemek') || command.includes('yemeğ')) {
        await manager.setContext('meal_time', 'meal');
    } else if (command.includes('su')) {
        await manager.setContext('water_time', 'meal');
    } else if (command.includes('acil') || command.includes('yardım') || command.includes('help')) {
        await manager.setContext('emergency', 'emergency');
    } else {
        await manager.setContext('home', 'normal');
    }
}

// ============================================================================
// 📊 ACCESSIBILITY ENHANCEMENTS
// ============================================================================

class AccessibilityManager {
    constructor() {
        this.touchPadding = 20; // 20px padding for touch targets
    }

    // Proximity sensor: Auto-wake when phone approaches face
    enableProximitySensor() {
        if ('DeviceProximityEvent' in window) {
            window.addEventListener('deviceproximity', (e) => {
                console.log(`📱 Proximity: ${e.value}cm`);
                if (e.value < 5) {
                    // Wake screen / activate assistant
                    console.log('👤 Yakınlık tespit: Kullanıcı aktif');
                }
            });
        }
    }

    // Enlarge touch targets by adding invisible padding
    enlargeTouchAreas() {
        document.addEventListener('touchstart', (e) => {
            if (e.target.tagName === 'BUTTON') {
                // Get button position
                const rect = e.target.getBoundingClientRect();
                const padding = this.touchPadding;
                
                // Check if touch is within padded area
                if (e.touches[0].clientX >= rect.left - padding &&
                    e.touches[0].clientX <= rect.right + padding &&
                    e.touches[0].clientY >= rect.top - padding &&
                    e.touches[0].clientY <= rect.bottom + padding) {
                    
                    // Trigger button click
                    e.target.click();
                }
            }
        });
    }

    // Voice descriptions using TTS
    describeScreen(screenContext) {
        const descriptions = {
            medication_time: "İlaç zamanı ekranında misin. Ekranda büyük bir buton var. İlacını aldığında butona bas.",
            meal_time: "Yemek zamanı ekranında misin. Yemek yedin mi sorusu var. Evet ise butona bas.",
            water_time: "Su zamanı. Su içtin mi diye sorulmaktadır. İçtiysen butona bas.",
            home: "Ana ekran. Mikrofon ve durum kartları var. Soruların var mı diye sor."
        };
        
        const text = descriptions[screenContext] || descriptions.home;
        
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = 'tr-TR';
        utterance.rate = 0.8;
        window.speechSynthesis.speak(utterance);
    }

    // Haptic feedback for actions
    triggerHaptic(pattern = 'standard') {
        if (!navigator.vibrate) return;
        
        const patterns = {
            standard: 50,
            success: [100, 50, 100],
            error: [200, 100, 200],
            warning: [100, 100, 100]
        };
        
        navigator.vibrate(patterns[pattern] || patterns.standard);
    }
}

// Initialize accessibility
window.accessibilityManager = new AccessibilityManager();
window.accessibilityManager.enableProximitySensor();
window.accessibilityManager.enlargeTouchAreas();
