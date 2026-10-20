// ========================================================
// Quản lý Dark Mode & Light Mode cho Website (Bootstrap 5.3)
// ========================================================

(() => {
    'use strict';

    const getStoredTheme = () => localStorage.getItem('theme');
    const setStoredTheme = theme => localStorage.setItem('theme', theme);

    const getPreferredTheme = () => {
        const storedTheme = getStoredTheme();
        if (storedTheme) {
            return storedTheme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    };

    const setTheme = theme => {
        if (theme === 'auto') {
            document.documentElement.setAttribute('data-bs-theme', (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'));
        } else {
            document.documentElement.setAttribute('data-bs-theme', theme);
        }
    };

    // Áp dụng theme ngay khi script chạy
    setTheme(getPreferredTheme());

    const showActiveTheme = (theme, focus = false) => {
        const themeSwitcher = document.querySelector('#themeSwitcher');
        if (!themeSwitcher) return;

        const activeThemeIcon = document.querySelector('.theme-icon-active');
        const btnToActive = document.querySelector(`[data-bs-theme-value="${theme}"]`);
        
        if (!btnToActive) return;

        const svgOfActiveBtn = btnToActive.querySelector('svg use')?.getAttribute('href') ||
                               btnToActive.querySelector('i')?.className;

        document.querySelectorAll('[data-bs-theme-value]').forEach(element => {
            element.classList.remove('active');
            element.setAttribute('aria-pressed', 'false');
            const check = element.querySelector('.theme-check');
            if (check) check.classList.add('d-none');
        });

        btnToActive.classList.add('active');
        btnToActive.setAttribute('aria-pressed', 'true');
        const check = btnToActive.querySelector('.theme-check');
        if (check) check.classList.remove('d-none');

        // Cập nhật icon trên nút chính
        const targetIcon = btnToActive.getAttribute('data-icon');
        if (activeThemeIcon && targetIcon) {
            activeThemeIcon.innerHTML = targetIcon;
        }

        if (focus) {
            themeSwitcher.focus();
        }
    };

    // Lắng nghe thay đổi theme từ hệ điều hành (OS)
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        const storedTheme = getStoredTheme();
        if (storedTheme !== 'light' && storedTheme !== 'dark') {
            setTheme(getPreferredTheme());
        }
    });

    window.addEventListener('DOMContentLoaded', () => {
        showActiveTheme(getPreferredTheme());

        document.querySelectorAll('[data-bs-theme-value]').forEach(toggle => {
            toggle.addEventListener('click', (e) => {
                e.preventDefault();
                const theme = toggle.getAttribute('data-bs-theme-value');
                setStoredTheme(theme);
                setTheme(theme);
                showActiveTheme(theme, true);
            });
        });

        // Hỗ trợ nút click 1 chạm toggle nhanh giữa Dark và Light (nếu có)
        const quickToggle = document.querySelector('#quickThemeToggle');
        if (quickToggle) {
            quickToggle.addEventListener('click', () => {
                const current = document.documentElement.getAttribute('data-bs-theme');
                const nextTheme = current === 'dark' ? 'light' : 'dark';
                setStoredTheme(nextTheme);
                setTheme(nextTheme);
                showActiveTheme(nextTheme);
            });
        }
    });
})();
