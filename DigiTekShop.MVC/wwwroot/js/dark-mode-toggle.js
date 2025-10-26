/**
 * Dark Mode Toggle
 * قابلیت تغییر حالت تیره/روشن
 */

class DarkModeToggle {
    constructor() {
        this.darkMode = localStorage.getItem('darkMode') === 'true';
        this.init();
    }

    init() {
        this.createToggleButton();
        this.applyDarkMode();
        this.bindEvents();
    }

    createToggleButton() {
        // ایجاد دکمه تغییر حالت
        const toggleButton = document.createElement('button');
        toggleButton.className = 'dark-mode-toggle';
        toggleButton.innerHTML = this.darkMode ? '☀️' : '🌙';
        toggleButton.setAttribute('aria-label', 'تغییر حالت تیره/روشن');
        
        // اضافه کردن به صفحه
        document.body.appendChild(toggleButton);
        
        // ذخیره رفرنس
        this.toggleButton = toggleButton;
    }

    bindEvents() {
        this.toggleButton.addEventListener('click', () => {
            this.toggleDarkMode();
        });

        // گوش دادن به تغییرات سیستم
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            if (!localStorage.getItem('darkMode')) {
                this.darkMode = e.matches;
                this.applyDarkMode();
            }
        });
    }

    toggleDarkMode() {
        this.darkMode = !this.darkMode;
        this.applyDarkMode();
        this.savePreference();
    }

    applyDarkMode() {
        const body = document.body;
        
        if (this.darkMode) {
            body.classList.add('dark-mode');
            this.toggleButton.innerHTML = '☀️';
        } else {
            body.classList.remove('dark-mode');
            this.toggleButton.innerHTML = '🌙';
        }
    }

    savePreference() {
        localStorage.setItem('darkMode', this.darkMode);
    }
}

// راه‌اندازی خودکار
document.addEventListener('DOMContentLoaded', () => {
    new DarkModeToggle();
});
