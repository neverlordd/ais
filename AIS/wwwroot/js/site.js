var root = document.documentElement;

function getInitialTheme() {
    return "light";
}

function setTheme(theme, save) {
    if (theme !== "dark" && theme !== "light") {
        theme = "light";
    }

    root.classList.remove("dark");

    if (theme === "dark") {
        root.classList.add("dark");
    }

    root.setAttribute("data-theme", theme);
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
    var alert = document.querySelector("[data-alert]");
    if (!alert) {
        return;
    }

    window.setTimeout(function () {
        alert.classList.add("opacity-0");
        alert.classList.add("-translate-y-1");
    }, 4500);
}

function setupMobileMenu() {
    var toggleButton = document.getElementById("mobile-menu-toggle");
    var menu = document.getElementById("mobile-menu");
    var icon = document.querySelector("[data-menu-icon]");

    if (!toggleButton || !menu) {
        return;
    }

    toggleButton.onclick = function () {
        var isHidden = menu.classList.contains("hidden");

        if (isHidden) {
            menu.classList.remove("hidden");
        } else {
            menu.classList.add("hidden");
        }

        toggleButton.setAttribute("aria-expanded", isHidden ? "true" : "false");

        if (icon) {
            icon.textContent = isHidden ? "✕" : "☰";
        }
    };
}

document.addEventListener("DOMContentLoaded", function () {
    setTheme(getInitialTheme(), false);
    setupMobileMenu();
    setupShiftTimers();
    setupLiveClocks();
    setupAlertAutoHide();
});
