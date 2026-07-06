(function (global) {
    function isNativeCapacitor() {
        try {
            return global.Capacitor?.isNativePlatform?.() === true;
        } catch {
            return false;
        }
    }

    function getHttpPlugin() {
        return global.Capacitor?.Plugins?.CapacitorHttp || null;
    }

    function toHeaderObject(headers) {
        if (!headers) return {};
        if (headers instanceof Headers) {
            const out = {};
            headers.forEach((value, key) => {
                out[key] = value;
            });
            return out;
        }
        return { ...headers };
    }

    function parseBody(data) {
        if (data == null || data === '') return undefined;
        if (typeof data === 'object') return data;
        try {
            return JSON.parse(data);
        } catch {
            return data;
        }
    }

    function makeResponse(nativeResponse) {
        const status = Number(nativeResponse?.status || 0);
        const raw = nativeResponse?.data;
        const bodyText = typeof raw === 'string' ? raw : JSON.stringify(raw ?? '');
        return {
            ok: status >= 200 && status < 300,
            status,
            headers: {
                get(name) {
                    const key = String(name || '').toLowerCase();
                    const map = nativeResponse?.headers || {};
                    const found = Object.keys(map).find((h) => h.toLowerCase() === key);
                    return found ? map[found] : null;
                }
            },
            async text() {
                return bodyText;
            },
            async json() {
                return parseBody(raw);
            }
        };
    }

    async function nativeRequest(url, options = {}, timeoutMs = 12000) {
        const Http = getHttpPlugin();
        if (!Http?.request) {
            throw new Error('CapacitorHttp unavailable');
        }

        const method = String(options.method || 'GET').toUpperCase();
        const headers = toHeaderObject(options.headers);
        let data;
        if (options.body != null && options.body !== '') {
            data = typeof options.body === 'string' ? parseBody(options.body) : options.body;
        }

        const response = await Http.request({
            url,
            method,
            headers,
            data,
            connectTimeout: timeoutMs,
            readTimeout: timeoutMs
        });
        return makeResponse(response);
    }

    async function request(url, options = {}, timeoutMs = 12000) {
        if (isNativeCapacitor() && getHttpPlugin()?.request) {
            try {
                return await nativeRequest(url, options, timeoutMs);
            } catch (error) {
                console.warn('[native-fetch] CapacitorHttp failed, falling back to fetch:', error?.message || error);
            }
        }

        const controller = typeof AbortController !== 'undefined' ? new AbortController() : null;
        const timeoutId = controller ? setTimeout(() => controller.abort(), timeoutMs) : null;
        try {
            return await fetch(url, {
                ...options,
                signal: controller?.signal
            });
        } finally {
            if (timeoutId) clearTimeout(timeoutId);
        }
    }

    global.SafeGuardianFetch = {
        request,
        isNativeCapacitor
    };
})(window);
