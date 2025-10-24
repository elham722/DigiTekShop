/**
 * DigiTekShop Token Management – final
 * هماهنگ با: /api/v1/auth/refresh , /api/v1/auth/logout
 * RefreshTokenResponse: { accessToken, refreshToken, tokenType, expiresIn, issuedAtUtc, expiresAtUtc }
 */

class TokenManager {
    constructor() {
        this.ak = 'accessToken';
        this.rk = 'refreshToken';
        this.ae = 'accessTokenExpiresAtUtc'; // ISO
        this.uk = 'uid';                      // userId برای LogoutRequest

        this.init();
    }

    init() {
        this.setupTokenRefresh();
        this.setupLogoutHandlers();
    }

    // getters
    get accessToken() { return localStorage.getItem(this.ak); }
    get refreshToken() { return localStorage.getItem(this.rk); }
    get accessExpUtc() { return localStorage.getItem(this.ae); }
    get userId() { return localStorage.getItem(this.uk); }

    // setters
    setTokens({ accessToken, refreshToken, accessTokenExpiresAtUtc }) {
        if (accessToken) localStorage.setItem(this.ak, accessToken);
        if (refreshToken) localStorage.setItem(this.rk, refreshToken);
        if (accessTokenExpiresAtUtc) localStorage.setItem(this.ae, accessTokenExpiresAtUtc);
    }
    setUser({ userId }) {
        if (userId) localStorage.setItem(this.uk, String(userId));
    }

    clearTokens() {
        localStorage.removeItem(this.ak);
        localStorage.removeItem(this.rk);
        localStorage.removeItem(this.ae);
        localStorage.removeItem(this.uk);
    }

    isAccessExpiredSoon(thresholdSec = 30) {
        const expStr = this.accessExpUtc;
        if (!expStr) return true;
        const exp = new Date(expStr).getTime();
        const now = Date.now();
        return (exp - now) <= thresholdSec * 1000;
    }

    async refreshAccessToken() {
        const rt = this.refreshToken;
        if (!rt) throw new Error('no refresh token');

        const res = await fetch('/api/v1/auth/refresh', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: JSON.stringify({ refreshToken: rt })
        });

        if (!res.ok) {
            this.clearTokens();
            throw new Error('refresh failed');
        }

        const data = await res.json();
        // RefreshTokenResponse از API
        this.setTokens({
            accessToken: data.accessToken,
            refreshToken: data.refreshToken,
            accessTokenExpiresAtUtc: data.expiresAtUtc // API تاریخ UTC ISO می‌دهد
        });
        return data.accessToken;
    }

    async getValidAccessToken() {
        if (this.accessToken && !this.isAccessExpiredSoon()) return this.accessToken;
        return await this.refreshAccessToken();
    }

    async makeAuthenticatedRequest(url, options = {}) {
        const token = await this.getValidAccessToken();
        const merged = {
            ...options,
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                ...(options.headers || {}),
                'Authorization': `Bearer ${token}`
            }
        };

        let res = await fetch(url, merged);

        if (res.status === 401) {
            try {
                const newToken = await this.refreshAccessToken();
                merged.headers['Authorization'] = `Bearer ${newToken}`;
                res = await fetch(url, merged);
            } catch {
                this.clearTokens();
                window.location.href = '/Auth/Login';
            }
        }

        return res;
    }

    setupTokenRefresh() {
        // هر ۱ دقیقه چک می‌کنیم اگر نزدیک انقضا بود رفرش کند
        setInterval(() => {
            if (this.accessToken && this.isAccessExpiredSoon()) {
                this.refreshAccessToken().catch(() => {
                    this.clearTokens();
                    window.location.href = '/Auth/Login';
                });
            }
        }, 60 * 1000);
    }

    setupLogoutHandlers() {
        document.addEventListener('click', (e) => {
            if (e.target.matches('[data-logout]') || e.target.closest('[data-logout]')) {
                e.preventDefault();
                this.logout();
            }
        });
    }

    async logout() {
        try {
            const rt = this.refreshToken;
            const at = this.accessToken;
            if (rt && at) {
                await fetch('/api/v1/auth/logout', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest',
                        'Authorization': `Bearer ${at}`
                    },
                    body: JSON.stringify({
                        userId: this.userId || null,     // DTO شما UserId می‌خواهد؛ از Login ذخیره کرده‌ایم
                        refreshToken: rt
                    })
                });
            }
        } finally {
            this.clearTokens();
            window.location.href = '/Auth/Login';
        }
    }
}

window.tokenManager = new TokenManager();

// ریدایرکت ساده روی صفحه لاگین اگر قبلاً لاگین است
document.addEventListener('DOMContentLoaded', () => {
    if (window.location.pathname === '/Auth/Login' && window.tokenManager.accessToken && !window.tokenManager.isAccessExpiredSoon()) {
        window.location.href = '/';
    }
});
