var root = document.documentElement;
var themeStorageKey = "shiftline-theme";

function getInitialTheme() {
    try {
        var savedTheme = localStorage.getItem(themeStorageKey);
        if (savedTheme === "dark" || savedTheme === "light") {
            return savedTheme;
        }
    } catch (error) {
    }

    return root.classList.contains("dark") ? "dark" : "light";
}

function getCurrentTheme() {
    return root.classList.contains("dark") ? "dark" : "light";
}

function persistTheme(theme) {
    try {
        localStorage.setItem(themeStorageKey, theme);
    } catch (error) {
    }
}

function setTheme(theme, persist) {
    if (theme !== "dark" && theme !== "light") {
        theme = "light";
    }

    if (persist === undefined) {
        persist = true;
    }

    root.classList.toggle("dark", theme === "dark");

    root.setAttribute("data-theme", theme);
    syncThemeToggleIcons(theme);
    syncThemeToggleLabels(theme);

    if (persist) {
        persistTheme(theme);
    }
}

function syncThemeToggleIcons(theme) {
    var desktopIcon = document.querySelector("[data-theme-icon]");
    var mobileIcon = document.querySelector("[data-theme-icon-mobile]");
    var icon = theme === "dark" ? "☀" : "☾";

    if (desktopIcon) {
        desktopIcon.textContent = icon;
    }

    if (mobileIcon) {
        mobileIcon.textContent = icon;
    }
}

function syncThemeToggleLabels(theme) {
    var buttons = [
        document.getElementById("theme-toggle"),
        document.getElementById("theme-toggle-mobile")
    ];
    var label = theme === "dark" ? "Включить светлую тему" : "Включить темную тему";

    for (var i = 0; i < buttons.length; i++) {
        var button = buttons[i];

        if (!button) {
            continue;
        }

        button.setAttribute("aria-label", label);
        button.setAttribute("title", label);
    }
}

function setupThemeToggle() {
    var toggleButtons = [
        document.getElementById("theme-toggle"),
        document.getElementById("theme-toggle-mobile")
    ];

    syncThemeToggleIcons(getCurrentTheme());
    syncThemeToggleLabels(getCurrentTheme());

    for (var i = 0; i < toggleButtons.length; i++) {
        var button = toggleButtons[i];

        if (!button) {
            continue;
        }

        button.addEventListener("click", function (event) {
            event.preventDefault();
            setTheme(getCurrentTheme() === "dark" ? "light" : "dark");
        });
    }
}

function padTime(value) {
    return value < 10 ? "0" + value : String(value);
}

function formatDuration(totalSeconds) {
    var hours = padTime(Math.floor(totalSeconds / 3600));
    var minutes = padTime(Math.floor((totalSeconds % 3600) / 60));
    var seconds = padTime(totalSeconds % 60);
    return hours + ":" + minutes + ":" + seconds;
}

function updateShiftTimers() {
    var timers = document.querySelectorAll("[data-shift-timer]");
    var currentUnix = Math.floor(new Date().getTime() / 1000);

    for (var i = 0; i < timers.length; i++) {
        var timer = timers[i];
        var startUnix = parseInt(timer.getAttribute("data-shift-start-unix"), 10);

        if (isNaN(startUnix)) {
            timer.textContent = "00:00:00";
            continue;
        }

        var diff = currentUnix - startUnix;
        if (diff < 0) {
            diff = 0;
        }

        timer.textContent = formatDuration(diff);
    }
}

function setupShiftTimers() {
    updateShiftTimers();
    window.setInterval(updateShiftTimers, 1000);
}

function updateLiveClocks() {
    var clocks = document.querySelectorAll("[data-live-clock]");
    if (!clocks.length) {
        return;
    }

    var now = new Date();
    var timeValue = padTime(now.getHours()) + ":" + padTime(now.getMinutes()) + ":" + padTime(now.getSeconds());

    for (var i = 0; i < clocks.length; i++) {
        clocks[i].textContent = timeValue;
    }
}

function setupLiveClocks() {
    updateLiveClocks();
    window.setInterval(updateLiveClocks, 1000);
}

function setupAlertAutoHide() {
    var payload = document.getElementById("app-toast-payload");
    if (!payload) {
        return;
    }

    var message = payload.getAttribute("data-toast-message");
    var type = payload.getAttribute("data-toast-type") || "success";

    if (!message) {
        return;
    }

    var stack = document.createElement("div");
    stack.className = "app-toast-stack";

    var alert = document.createElement("div");
    var typeClass = "border-emerald-200 bg-emerald-50/95 text-emerald-700 dark:border-emerald-950 dark:bg-emerald-950/60 dark:text-emerald-200";

    if (type === "error") {
        typeClass = "border-rose-200 bg-rose-50/95 text-rose-700 dark:border-rose-950 dark:bg-rose-950/60 dark:text-rose-200";
    } else if (type === "warning") {
        typeClass = "border-amber-200 bg-amber-50/95 text-amber-700 dark:border-amber-950 dark:bg-amber-950/60 dark:text-amber-200";
    }

    alert.className = "app-toast transform rounded-2xl px-4 py-3 text-sm font-medium shadow-soft transition duration-300 " + typeClass;
    alert.setAttribute("data-alert", "");
    alert.textContent = message;
    stack.appendChild(alert);
    document.body.appendChild(stack);

    window.setTimeout(function () {
        alert.classList.add("opacity-0");
        alert.classList.add("-translate-y-1");

        window.setTimeout(function () {
            stack.remove();
        }, 350);
    }, 4500);
}

function setupMobileMenu() {
    var toggleButton = document.getElementById("mobile-menu-toggle");
    var menu = document.getElementById("mobile-menu");
    var icon = document.querySelector("[data-menu-icon]");

    if (!toggleButton || !menu) {
        return;
    }

    function setMenuState(isOpen) {
        menu.classList.toggle("hidden", !isOpen);
        toggleButton.setAttribute("aria-expanded", isOpen ? "true" : "false");

        if (icon) {
            icon.textContent = isOpen ? "✕" : "☰";
        }
    }

    toggleButton.onclick = function () {
        setMenuState(menu.classList.contains("hidden"));
    };

    document.addEventListener("click", function (event) {
        if (menu.classList.contains("hidden")) {
            return;
        }

        var target = event.target;
        if (!(target instanceof Node)) {
            return;
        }

        if (menu.contains(target) || toggleButton.contains(target)) {
            return;
        }

        setMenuState(false);
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            setMenuState(false);
        }
    });

    window.addEventListener("resize", function () {
        if (window.innerWidth >= 1024) {
            setMenuState(false);
        }
    });
}

document.addEventListener("DOMContentLoaded", function () {
    setTheme(getInitialTheme(), false);
    setupThemeToggle();
    setupMobileMenu();
    setupShiftTimers();
    setupLiveClocks();
    setupAlertAutoHide();
});
