(function () {
    "use strict";

    const storageKey = "municipal-elections-theme";
    const root = document.documentElement;
    const systemTheme = window.matchMedia ? window.matchMedia("(prefers-color-scheme: dark)") : null;
    let preference = readPreference();

    function readPreference() {
        try {
            const value = window.localStorage.getItem(storageKey);
            return value === "light" || value === "dark" ? value : null;
        } catch {
            return null;
        }
    }

    function updateButtons() {
        const dark = root.getAttribute("data-bs-theme") === "dark";
        document.querySelectorAll("[data-theme-toggle]").forEach(function (button) {
            button.textContent = dark ? "Light mode" : "Dark mode";
            button.setAttribute("aria-label", dark ? "Switch to light mode" : "Switch to dark mode");
        });
    }

    function applyTheme() {
        const theme = preference || (systemTheme && systemTheme.matches ? "dark" : "light");
        root.setAttribute("data-bs-theme", theme);
        root.style.colorScheme = theme;
        updateButtons();
    }

    // Run in the head before styles load, so saved preferences apply immediately.
    applyTheme();
    document.addEventListener("DOMContentLoaded", updateButtons);
    document.addEventListener("click", function (event) {
        if (!event.target.closest || !event.target.closest("[data-theme-toggle]")) return;
        preference = root.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
        try {
            window.localStorage.setItem(storageKey, preference);
        } catch {
            // The toggle still works when browser storage is unavailable.
        }
        applyTheme();
    });

    if (systemTheme && systemTheme.addEventListener) {
        systemTheme.addEventListener("change", function () {
            if (!preference) applyTheme();
        });
    }
    window.addEventListener("storage", function (event) {
        if (event.key === storageKey || event.key === null) {
            preference = readPreference();
            applyTheme();
        }
    });
}());
