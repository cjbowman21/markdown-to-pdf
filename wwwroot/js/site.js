(() => {
  const storageKey = 'theme';
  const toggle = document.getElementById('themeToggle');
  const sun = document.getElementById('sunIcon');
  const moon = document.getElementById('moonIcon');

  function setTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(storageKey, theme);
    const isDark = theme === 'dark';
    if (sun && moon) {
      sun.style.display = isDark ? 'none' : 'block';
      moon.style.display = isDark ? 'block' : 'none';
    }
    if (toggle) {
      toggle.setAttribute('aria-label', isDark ? 'Switch to light mode' : 'Switch to dark mode');
    }
  }

  function initTheme() {
    const stored = localStorage.getItem(storageKey);
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    setTheme(stored || (prefersDark ? 'dark' : 'light'));
  }

  toggle?.addEventListener('click', () => {
    const current = document.documentElement.getAttribute('data-theme');
    setTheme(current === 'dark' ? 'light' : 'dark');
  });

  initTheme();
})();
