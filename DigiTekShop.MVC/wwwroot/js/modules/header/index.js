/**
 * Header Module - Main Entry Point
 * Composes all header-related functionality
 */

import { mountHeaderSearch } from './search.js';
import { mountMegaSearch } from './mega-search.js';
import { mountVerticalMenu } from './vertical-menu.js';
import { mountNavigation } from './navigation.js';

// Export checkAuthStatus globally so it can be called after login
export function refreshAuthStatus() {
    checkAuthStatus();
}

// Export logout function globally
export function handleLogout() {
    performLogout();
}

export function mountHeader(context = {}) {
    console.log('Initializing header modules...');
    
    try {
        // Initialize all header modules
        mountHeaderSearch();
        mountMegaSearch();
        mountVerticalMenu();
        mountNavigation();
        
        // Setup logout button handler
        setupLogoutHandler();
        
        // Check authentication status and update UI
        // Use setTimeout to ensure DOM is fully ready
        // Also check on page load (in case of redirect after login)
        setTimeout(() => {
            checkAuthStatus();
        }, 100);
        
        // Also check when page is fully loaded (for redirects)
        if (document.readyState === 'loading') {
            window.addEventListener('load', () => {
                setTimeout(() => checkAuthStatus(), 200);
            });
        }
        
        console.log('Header modules initialized successfully');
    } catch (error) {
        console.error('Error initializing header modules:', error);
    }
}

/**
 * Checks authentication status by calling /api/v1/Auth/me
 * Updates UI based on response (200 = logged in, 401 = guest)
 */
async function checkAuthStatus() {
    console.log('[Auth] Checking authentication status...');
    
    // First, verify DOM elements exist
    const guestElements = document.querySelectorAll('.nav-guest');
    const authElements = document.querySelectorAll('.nav-auth');
    console.log('[Auth] DOM check - Guest elements:', guestElements.length, 'Auth elements:', authElements.length);
    
    if (guestElements.length === 0 && authElements.length === 0) {
        console.warn('[Auth] No auth/guest elements found in DOM! Retrying in 500ms...');
        setTimeout(() => checkAuthStatus(), 500);
        return;
    }
    
    try {
        const res = await fetch('/api/v1/Auth/me', {
            method: 'GET',
            credentials: 'same-origin',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        console.log('[Auth] Response status:', res.status, res.statusText);

        if (!res.ok) {
            // 401, 403, or any other error → user is not authenticated
            console.log('[Auth] User is not authenticated (status:', res.status, ')');
            setHeaderLoggedOutState();
            return;
        }

        const payload = await res.json();
        console.log('[Auth] Response payload:', payload);
        
        // ApiResponse structure: { data: {...}, meta: null, traceId: "...", timestamp: "..." }
        // Check if data exists (not success field!)
        if (!payload || !payload.data) {
            console.warn('[Auth] Invalid response format - missing data field:', payload);
            setHeaderLoggedOutState();
            return;
        }

        // User is authenticated
        console.log('[Auth] User is authenticated:', payload.data);
        setHeaderLoggedInState(payload.data);
    } catch (err) {
        console.error('[Auth] checkAuthStatus failed:', err);
        // On error, default to logged out state
        setHeaderLoggedOutState();
    }
}

/**
 * Updates header UI to show logged-in state
 * @param {Object} user - User data from /api/v1/Auth/me response
 */
function setHeaderLoggedInState(user) {
    console.log('[Auth] Setting logged-in state for user:', user);
    
    // Hide guest elements - use more specific selector
    const guestElements = document.querySelectorAll('.nav-guest');
    console.log('[Auth] Found', guestElements.length, 'guest elements');
    if (guestElements.length === 0) {
        console.error('[Auth] ⚠️ No guest elements found! Check HTML structure.');
    }
    guestElements.forEach((el, index) => {
        console.log(`[Auth] Hiding guest element ${index + 1}:`, el, 'Current classes:', el.className);
        el.classList.add('d-none');
        el.style.display = 'none'; // Force hide as backup
        console.log(`[Auth] After hide - classes:`, el.className, 'display:', window.getComputedStyle(el).display);
    });

    // Show authenticated elements - use more specific selector
    const authElements = document.querySelectorAll('.nav-auth');
    console.log('[Auth] Found', authElements.length, 'auth elements');
    if (authElements.length === 0) {
        console.error('[Auth] ⚠️ No auth elements found! Check HTML structure.');
    }
    authElements.forEach((el, index) => {
        console.log(`[Auth] Showing auth element ${index + 1}:`, el, 'Current classes:', el.className);
        el.classList.remove('d-none');
        el.style.display = ''; // Remove inline style if exists
        console.log(`[Auth] After show - classes:`, el.className, 'display:', window.getComputedStyle(el).display);
    });

    // Update user name
    const nameElement = document.querySelector('[data-user-name]');
    if (nameElement && user) {
        const displayName = user.fullName || user.phone || 'حساب کاربری من';
        nameElement.textContent = displayName;
        console.log('[Auth] Updated user name to:', displayName);
    } else {
        console.warn('[Auth] Name element not found or user data missing');
    }

    // Update user phone
    const phoneElement = document.querySelector('[data-user-phone]');
    if (phoneElement && user?.phone) {
        phoneElement.textContent = user.phone;
        console.log('[Auth] Updated user phone to:', user.phone);
    } else {
        console.warn('[Auth] Phone element not found or phone missing');
    }
}

/**
 * Updates header UI to show logged-out (guest) state
 */
function setHeaderLoggedOutState() {
    console.log('[Auth] Setting logged-out (guest) state');
    
    // Show guest elements
    const guestElements = document.querySelectorAll('.nav-guest');
    console.log('[Auth] Found', guestElements.length, 'guest elements');
    guestElements.forEach((el, index) => {
        console.log(`[Auth] Showing guest element ${index + 1}:`, el);
        el.classList.remove('d-none');
        el.style.display = ''; // Remove inline style if exists
    });

    // Hide authenticated elements
    const authElements = document.querySelectorAll('.nav-auth');
    console.log('[Auth] Found', authElements.length, 'auth elements');
    authElements.forEach((el, index) => {
        console.log(`[Auth] Hiding auth element ${index + 1}:`, el);
        el.classList.add('d-none');
        el.style.display = 'none'; // Force hide as backup
    });
}

/**
 * Sets up logout button click handler
 */
function setupLogoutHandler() {
    // Find all logout links/buttons
    const logoutElements = document.querySelectorAll('[data-logout], #logoutLink');
    console.log('[Auth] Found', logoutElements.length, 'logout elements');
    
    logoutElements.forEach((element, index) => {
        element.addEventListener('click', async (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            console.log(`[Auth] Logout button ${index + 1} clicked`);
            
            // Perform logout directly (no confirmation)
            await performLogout();
        });
        
        console.log(`[Auth] Logout handler attached to element ${index + 1}`);
    });
}

/**
 * Performs logout by calling API and clearing cookies
 */
async function performLogout() {
    console.log('[Auth] Starting logout process...');
    
    try {
        // Step 1: Get user info from /api/v1/Auth/me to get userId
        const meRes = await fetch('/api/v1/Auth/me', {
            method: 'GET',
            credentials: 'same-origin',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            }
        });

        let userId = null;
        if (meRes.ok) {
            const meData = await meRes.json();
            if (meData?.data?.userId) {
                userId = meData.data.userId;
                console.log('[Auth] Got userId for logout:', userId);
            }
        }

        // Step 2: Call API logout-all endpoint (if we have userId)
        // logout-all only needs userId and revokes all refresh tokens for the user
        if (userId) {
            try {
                const logoutRes = await fetch('/api/v1/Auth/logout-all', {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Requested-With': 'XMLHttpRequest'
                    },
                    body: JSON.stringify({
                        userId: userId
                    })
                });

                if (logoutRes.ok) {
                    console.log('[Auth] API logout-all successful - all tokens revoked');
                } else {
                    console.warn('[Auth] API logout-all returned status:', logoutRes.status);
                    // Try to read error response
                    try {
                        const errorData = await logoutRes.json();
                        console.warn('[Auth] API logout-all error:', errorData);
                    } catch (e) {
                        // Ignore JSON parse error
                    }
                }
            } catch (apiErr) {
                console.warn('[Auth] API logout-all error (continuing anyway):', apiErr);
            }
        }

        // Step 3: Call MVC logout to clear cookies
        try {
            const mvcLogoutRes = await fetch('/Auth/Logout', {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (mvcLogoutRes.ok) {
                console.log('[Auth] MVC logout successful, cookies cleared');
            } else {
                console.warn('[Auth] MVC logout returned status:', mvcLogoutRes.status);
            }
        } catch (mvcErr) {
            console.warn('[Auth] MVC logout error (continuing anyway):', mvcErr);
        }

        // Step 4: Manually clear cookies as backup (in case server-side deletion fails)
        // Note: HttpOnly cookies cannot be deleted from JavaScript, but we try anyway
        document.cookie = 'dt_at=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; SameSite=Lax; Secure';
        document.cookie = 'dt_rt=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; SameSite=Lax; Secure';
        console.log('[Auth] Attempted to clear cookies from client side');

        // Step 5: Update UI immediately
        setHeaderLoggedOutState();

        // Step 6: Force redirect to login page (use replace to prevent back button)
        console.log('[Auth] Redirecting to login page...');
        window.location.replace('/Auth/Login');

    } catch (err) {
        console.error('[Auth] Logout error:', err);
        // Even on error, try to clear UI and redirect
        setHeaderLoggedOutState();
        window.location.href = '/Auth/Login';
    }
}
