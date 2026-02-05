/**
 * AI Voice Analysis & Emergency Protocol
 * Handles fall detection, health monitoring, voice checks, and emergency escalation
 */

class AIEmergencyProtocol {
    constructor() {
        this.elderlyId = 'elderly-001';
        this.voiceCheckTimeout = 15000; // 15 seconds
        this.silenceThreshold = 15000; // 15 seconds
        this.isEmergencyMode = false;
        this.voiceCheckInProgress = false;
        this.emotionScores = { calm: 1.0, worried: 0.7, distressed: 0.4, panicked: 0.2 };
        this.recognitionInstance = null;
        
        // Health thresholds (Critical)
        this.thresholds = {
            bloodPressureSystolic: 180,  // > 180 mmHg
            bloodPressureDiastolic: 110, // > 110 mmHg
            heartRate: { min: 50, max: 120 }, // <50 or >120 bpm
            glucose: 180, // >180 mg/dL
            fallAcceleration: 25 // >25 m/s²
        };
    }

    /**
     * Detect fall via accelerometer (G-Sensor)
     * Triggered when sudden acceleration detected
     */
    async detectFall(accelerationMagnitude) {
        console.log(`🚨 Fall Detection - Acceleration: ${accelerationMagnitude.toFixed(2)} m/s²`);
        
        if (accelerationMagnitude > this.thresholds.fallAcceleration) {
            await this.triggerEmergencyProtocol('fall_detected', {
                accelerationMagnitude,
                timestamp: new Date().toISOString()
            });
            return true;
        }
        return false;
    }

    /**
     * Monitor health data for critical values
     */
    async monitorHealthData(metricType, value) {
        let isCritical = false;
        let reason = '';

        if (metricType === 'blood_pressure') {
            // Format: "120/80"
            const [systolic, diastolic] = value.toString().split('/').map(Number);
            if (systolic > this.thresholds.bloodPressureSystolic || 
                diastolic > this.thresholds.bloodPressureDiastolic) {
                isCritical = true;
                reason = `Critical BP: ${systolic}/${diastolic}`;
            }
        } else if (metricType === 'heart_rate') {
            if (value < this.thresholds.heartRate.min || value > this.thresholds.heartRate.max) {
                isCritical = true;
                reason = `Heart Rate: ${value} bpm`;
            }
        } else if (metricType === 'glucose') {
            if (value > this.thresholds.glucose) {
                isCritical = true;
                reason = `Critical Glucose: ${value} mg/dL`;
            }
        }

        if (isCritical) {
            console.log(`⚠️ ${reason}`);
            await this.triggerEmergencyProtocol('health_critical', {
                metricType,
                value,
                timestamp: new Date().toISOString()
            });
        }

        return isCritical;
    }

    /**
     * AI Voice Check - Ask user if they're okay
     * Listens for 15 seconds for voice response
     * Analyzes emotion from voice tone
     */
    async initiateVoiceCheck(reason = 'health_check') {
        if (this.voiceCheckInProgress) return;
        this.voiceCheckInProgress = true;

        console.log('🎤 Initiating AI Voice Check...');

        // Step 1: Speak the question
        const languages = {
            'tr': t('emergency_check'), // "İyi misin? Bir sorun mu var?"
            'en': t('emergency_check'), // "Are you okay? Is there a problem?"
            'de': t('emergency_check')  // "Geht es dir gut? Gibt es ein Problem?"
        };
        
        const questionText = languages[currentLanguage] || languages['tr'];
        await this.speak(questionText, true, 1.3); // Higher pitch for urgency

        // Step 2: Start listening
        const voiceResponse = await this.captureVoiceResponse(this.voiceCheckTimeout);

        // Step 3: Analyze response
        const analysis = await this.analyzeVoiceResponse(voiceResponse);

        // Step 4: Determine action
        if (analysis.positiveResponse && analysis.emotionScore > 0.5) {
            // User confirmed they're okay
            console.log('✅ User confirmed OK via voice');
            await this.cancelEmergency();
            return { success: true, escalated: false };
        } else {
            // No positive response or emotional distress detected
            console.log(`🚨 Voice check failed - Escalating emergency (Emotion: ${analysis.emotionScore.toFixed(2)})`);
            await this.escalateEmergency(reason, voiceResponse, analysis);
            return { success: false, escalated: true };
        }
    }

    /**
     * Capture voice response from user
     */
    async captureVoiceResponse(timeoutMs) {
        return new Promise((resolve) => {
            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (!SpeechRecognition) {
                console.error('Speech Recognition not supported');
                resolve('');
                return;
            }

            const recognition = new SpeechRecognition();
            recognition.lang = currentLanguage === 'de' ? 'de-DE' : 
                               currentLanguage === 'en' ? 'en-US' : 'tr-TR';
            recognition.continuous = false;
            recognition.interimResults = false;

            let transcript = '';
            let silenceStartTime = Date.now();

            recognition.onstart = () => {
                console.log('🎤 Listening for response...');
                document.getElementById('aiListeningIndicator')?.classList.add('active');
            };

            recognition.onresult = (event) => {
                for (let i = event.resultIndex; i < event.results.length; i++) {
                    transcript += event.results[i][0].transcript + ' ';
                }
                silenceStartTime = Date.now(); // Reset silence timer
            };

            recognition.onerror = (event) => {
                console.error('Speech recognition error:', event.error);
            };

            recognition.onend = () => {
                document.getElementById('aiListeningIndicator')?.classList.remove('active');
                clearTimeout(timeoutHandle);
                resolve(transcript.trim());
            };

            const timeoutHandle = setTimeout(() => {
                recognition.stop();
                console.log(`⏱️ Voice check timeout (${timeoutMs}ms) - No response`);
            }, timeoutMs);

            recognition.start();
        });
    }

    /**
     * Analyze voice response for:
     * 1. Positive keywords
     * 2. Emotion tone (frequency analysis)
     */
    async analyzeVoiceResponse(voiceText) {
        console.log(`🔍 Analyzing voice: "${voiceText}"`);

        // Keyword analysis
        const turkishPositive = ['iyiyim', 'tamam', 'sorun yok', 'iyi', 'atlıyorum', 'iyiyim'];
        const englishPositive = ['fine', 'okay', 'good', 'alright', 'im okay', 'yes'];
        const germanPositive = ['mir geht es gut', 'alles ok', 'alles klar', 'ja', 'ok'];

        const allPositive = [...turkishPositive, ...englishPositive, ...germanPositive];
        const hasPositiveKeyword = allPositive.some(keyword => 
            voiceText.toLowerCase().includes(keyword)
        );

        // Emotion analysis (simplified - in production use ML model)
        // Based on word count and punctuation
        let emotionScore = 0.7; // Default: neutral

        if (voiceText.length === 0) {
            emotionScore = 0.2; // Panic - no response
        } else if (voiceText.toLowerCase().includes('acil') || 
                   voiceText.toLowerCase().includes('help') ||
                   voiceText.toLowerCase().includes('hilfe')) {
            emotionScore = 0.1; // Panic
        } else if (hasPositiveKeyword && voiceText.length > 5) {
            emotionScore = 0.9; // Calm
        } else if (voiceText.length > 0 && voiceText.length < 3) {
            emotionScore = 0.4; // Distressed
        }

        return {
            voiceText,
            positiveResponse: hasPositiveKeyword,
            emotionScore,
            emotionLabel: this.getEmotionLabel(emotionScore)
        };
    }

    /**
     * Get emotion label from score (0-1)
     */
    getEmotionLabel(score) {
        if (score >= 0.8) return 'calm';
        if (score >= 0.6) return 'worried';
        if (score >= 0.4) return 'distressed';
        return 'panicked';
    }

    /**
     * Trigger emergency protocol
     */
    async triggerEmergencyProtocol(alertType, details) {
        if (this.isEmergencyMode) return;
        this.isEmergencyMode = true;

        console.log(`🚨 EMERGENCY PROTOCOL TRIGGERED: ${alertType}`);

        // Show emergency UI
        document.getElementById('emergencyScreen')?.classList.add('active');

        // Notify backend
        await fetch('/api/ai-health-check', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                elderlyId: this.elderlyId,
                healthStatus: 'critical',
                alertType,
                ...details
            })
        });

        // Initiate voice check
        await this.initiateVoiceCheck(alertType);
    }

    /**
     * Escalate to emergency broadcast (15 sec no response)
     */
    async escalateEmergency(reason, voiceResponse, analysis) {
        console.log(`📢 Escalating to emergency broadcast...`);

        // Get current location
        const location = await this.getCurrentLocation();

        // Get latest health data
        const healthData = await this.getLatestHealthData();

        // Broadcast emergency to family
        await fetch('/api/emergency-broadcast', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                elderlyId: this.elderlyId,
                timestamp: new Date().toISOString(),
                reason,
                voiceAnalysis: analysis,
                location,
                ...healthData
            })
        });

        // SignalR broadcast
        if (connection) {
            await connection.invoke('SendEmergencyEscalation', this.elderlyId);
        }

        // Show critical alert screen
        this.showCriticalAlert(reason, analysis);
    }

    /**
     * Cancel emergency if user confirms OK
     */
    async cancelEmergency() {
        this.isEmergencyMode = false;
        document.getElementById('emergencyScreen')?.classList.remove('active');

        // Notify backend
        await fetch('/api/ai-voice-check', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                elderlyId: this.elderlyId,
                voiceInput: 'positive_confirmation',
                emotionScore: 0.8
            })
        });

        console.log('✅ Emergency cancelled - User confirmed OK');
    }

    /**
     * Text-to-Speech with multilingual support
     */
    async speak(text, isUrgent = false, pitch = 1.0) {
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = currentLanguage === 'de' ? 'de-DE' : 
                        currentLanguage === 'en' ? 'en-US' : 'tr-TR';
        utterance.rate = isUrgent ? 0.8 : 0.9;
        utterance.pitch = pitch;
        utterance.volume = isUrgent ? 1.0 : 0.8;

        return new Promise((resolve) => {
            utterance.onend = resolve;
            speechSynthesis.speak(utterance);
        });
    }

    /**
     * Get current location via Geolocation API
     */
    async getCurrentLocation() {
        return new Promise((resolve) => {
            if ('geolocation' in navigator) {
                navigator.geolocation.getCurrentPosition(
                    (position) => {
                        const { latitude, longitude } = position.coords;
                        const googleMapsUrl = `https://maps.google.com/?q=${latitude},${longitude}`;
                        resolve({ latitude, longitude, mapsUrl: googleMapsUrl });
                    },
                    () => resolve({ latitude: null, longitude: null, mapsUrl: null })
                );
            } else {
                resolve({ latitude: null, longitude: null, mapsUrl: null });
            }
        });
    }

    /**
     * Get latest health data from backend
     */
    async getLatestHealthData() {
        try {
            const response = await fetch(`/api/health-analytics/${this.elderlyId}`);
            const data = await response.json();
            return data.analytics || {};
        } catch (error) {
            console.error('Error fetching health data:', error);
            return {};
        }
    }

    /**
     * Show critical alert screen
     */
    showCriticalAlert(reason, analysis) {
        const screen = document.getElementById('emergencyScreen');
        if (screen) {
            screen.innerHTML = `
                <div style="text-align: center; color: white;">
                    <div style="font-size: 40px; margin-bottom: 20px;">🚨 ACIL DURUM 🚨</div>
                    <div style="font-size: 30px; margin-bottom: 20px;">${t('help_coming')}</div>
                    <div style="font-size: 20px; margin-bottom: 10px;">Durum: ${analysis.emotionLabel}</div>
                    <div style="font-size: 16px;">Aile ve sağlık kuruluşları bilgilendirilmiştir</div>
                </div>
            `;
            screen.classList.add('active');
        }
    }

    /**
     * Monitor silence for timeout
     */
    monitorSilence(audioContext) {
        const analyser = audioContext.createAnalyser();
        const dataArray = new Uint8Array(analyser.frequencyBinCount);
        let silenceStart = Date.now();

        const checkSilence = () => {
            analyser.getByteFrequencyData(dataArray);
            const average = dataArray.reduce((a, b) => a + b) / dataArray.length;

            if (average < 30) { // Silence threshold
                const silenceDuration = Date.now() - silenceStart;
                if (silenceDuration >= this.silenceThreshold) {
                    console.log('⏱️ Silence timeout reached');
                    this.escalateEmergency('silence_timeout', '', { emotionScore: 0.1 });
                    return;
                }
            } else {
                silenceStart = Date.now(); // Reset
            }

            requestAnimationFrame(checkSilence);
        };

        checkSilence();
    }
}

// Initialize AI Protocol
const aiProtocol = new AIEmergencyProtocol();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { AIEmergencyProtocol };
}
