// ========================================================
// KidCare 8-Bit Retro Gaming - Quản lý Theme Dark / Light
// Tối ưu chuyển đổi mượt mà, đồng bộ icon & nhãn 8-bit
// ========================================================

(() => {
    'use strict';

    const THEME_KEY = 'theme';

    const getStoredTheme = () => localStorage.getItem(THEME_KEY);
    const setStoredTheme = theme => localStorage.setItem(THEME_KEY, theme);

    const getPreferredTheme = () => {
        const storedTheme = getStoredTheme();
        if (storedTheme === 'dark' || storedTheme === 'light') {
            return storedTheme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    };

    const setTheme = theme => {
        const resolvedTheme = (theme === 'auto')
            ? (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
            : theme;
        document.documentElement.setAttribute('data-bs-theme', resolvedTheme);
        return resolvedTheme;
    };

    const updateThemeButtonUI = (resolvedTheme) => {
        const btn = document.getElementById('themeToggleBtn') || document.getElementById('quickThemeToggle');
        if (!btn) return;

        const iconContainer = document.getElementById('themeToggleIcon') || btn.querySelector('.theme-icon-active');
        const textContainer = document.getElementById('themeToggleText');

        if (resolvedTheme === 'dark') {
            // Khi đang ở Dark mode: Icon mặt trời để chuyển về Sáng
            if (iconContainer) {
                iconContainer.innerHTML = '<i class="pixelart-icons-font-sun i8-sm text-warning"></i>';
            }
            if (textContainer) {
                textContainer.textContent = 'Sáng';
            }
            btn.setAttribute('title', 'Chuyển sang chế độ Sáng (Light Mode)');
            btn.setAttribute('aria-label', 'Chuyển sang chế độ Sáng (Light Mode)');
        } else {
            // Khi đang ở Light mode: Icon mặt trăng để chuyển sang Tối
            if (iconContainer) {
                iconContainer.innerHTML = '<i class="pixelart-icons-font-moon i8-sm text-info"></i>';
            }
            if (textContainer) {
                textContainer.textContent = 'Tối';
            }
            btn.setAttribute('title', 'Chuyển sang chế độ Tối (Dark Mode)');
            btn.setAttribute('aria-label', 'Chuyển sang chế độ Tối (Dark Mode)');
        }
    };

    // Khởi tạo ngay lập tức
    const initialTheme = setTheme(getPreferredTheme());

    // Lắng nghe thay đổi theme hệ điều hành khi người dùng không cố định lựa chọn
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        const storedTheme = getStoredTheme();
        if (!storedTheme) {
            const nextTheme = setTheme(getPreferredTheme());
            updateThemeButtonUI(nextTheme);
        }
    });

    document.addEventListener('DOMContentLoaded', () => {
        const currentActiveTheme = document.documentElement.getAttribute('data-bs-theme') || initialTheme;
        updateThemeButtonUI(currentActiveTheme);

        // Gắn sự kiện click cho nút toggle theme 8-bit
        const toggleBtn = document.getElementById('themeToggleBtn') || document.getElementById('quickThemeToggle');
        if (toggleBtn) {
            toggleBtn.addEventListener('click', () => {
                const current = document.documentElement.getAttribute('data-bs-theme') || 'light';
                const nextTheme = current === 'dark' ? 'light' : 'dark';
                setStoredTheme(nextTheme);
                setTheme(nextTheme);
                updateThemeButtonUI(nextTheme);
            });
        }
    });
})();
