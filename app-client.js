(function () {
    var USER_STORAGE_KEY = "attendanceHubCurrentUser";
    var UI_ROOT_ID = "attendanceUiRoot";
    var UI_STYLE_ID = "attendanceUiStyle";

    async function request(path, options) {
        var config = options || {};
        var headers = Object.assign({ "Content-Type": "application/json" }, config.headers || {});

        var response = await fetch(path, Object.assign({}, config, { headers: headers }));
        var payload = null;

        try {
            payload = await response.json();
        } catch (error) {
            payload = null;
        }

        if (!response.ok) {
            var message = payload && payload.message ? payload.message : "Request failed.";
            throw new Error(message);
        }

        return payload;
    }

    function getCurrentUser() {
        var raw = localStorage.getItem(USER_STORAGE_KEY);
        if (!raw) {
            return null;
        }

        try {
            return JSON.parse(raw);
        } catch (error) {
            localStorage.removeItem(USER_STORAGE_KEY);
            return null;
        }
    }

    function setCurrentUser(user) {
        localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
    }

    function clearCurrentUser() {
        localStorage.removeItem(USER_STORAGE_KEY);
    }

    function requireUser(redirectPath) {
        var user = getCurrentUser();
        if (!user) {
            window.location.href = redirectPath || "login.html";
            return null;
        }

        return user;
    }

    function ensureUiLayer() {
        if (!document.getElementById(UI_STYLE_ID)) {
            var style = document.createElement("style");
            style.id = UI_STYLE_ID;
            style.textContent =
                ".attendance-ui-backdrop{position:fixed;inset:0;background:rgba(22,21,26,.45);backdrop-filter:blur(3px);display:grid;place-items:center;padding:16px;z-index:9999}" +
                ".attendance-ui-modal{width:min(92vw,360px);background:#fff;color:#1c1b19;border:1px solid #e8e6e2;border-radius:14px;padding:18px;box-shadow:0 20px 48px rgba(0,0,0,.18)}" +
                ".attendance-ui-title{font-size:16px;font-weight:700;margin-bottom:6px}" +
                ".attendance-ui-text{font-size:13px;color:#6b6861;line-height:1.5}" +
                ".attendance-ui-actions{margin-top:16px;display:flex;justify-content:flex-end;gap:8px}" +
                ".attendance-ui-btn{height:36px;padding:0 14px;border-radius:10px;border:1px solid #d9d6d1;background:#f7f6f4;color:#1c1b19;font-size:12px;font-weight:600;cursor:pointer}" +
                ".attendance-ui-btn.primary{background:#1c1b19;color:#fff;border-color:#1c1b19}" +
                ".attendance-ui-loading{display:flex;align-items:center;gap:12px}" +
                ".attendance-ui-spinner{width:22px;height:22px;border:3px solid #d8d5cf;border-top-color:#1c1b19;border-radius:50%;animation:attendance-ui-spin .8s linear infinite}" +
                "@keyframes attendance-ui-spin{to{transform:rotate(360deg)}}";
            document.head.appendChild(style);
        }

        var root = document.getElementById(UI_ROOT_ID);
        if (!root) {
            root = document.createElement("div");
            root.id = UI_ROOT_ID;
            document.body.appendChild(root);
        }

        return root;
    }

    function showGlobalLoading(message) {
        var root = ensureUiLayer();
        var text = message || "Please wait...";
        root.innerHTML =
            '<div class="attendance-ui-backdrop" role="status" aria-live="polite">' +
            '  <div class="attendance-ui-modal">' +
            '    <div class="attendance-ui-loading">' +
            '      <div class="attendance-ui-spinner" aria-hidden="true"></div>' +
            '      <div>' +
            '        <div class="attendance-ui-title">Loading</div>' +
            '        <div class="attendance-ui-text">' + text + '</div>' +
            '      </div>' +
            '    </div>' +
            '  </div>' +
            '</div>';
    }

    function hideGlobalLoading() {
        var root = document.getElementById(UI_ROOT_ID);
        if (root) {
            root.innerHTML = "";
        }
    }

    function confirmDialog(options) {
        var root = ensureUiLayer();
        var title = options && options.title ? options.title : "Confirm Action";
        var message = options && options.message ? options.message : "Are you sure you want to continue?";
        var confirmText = options && options.confirmText ? options.confirmText : "Confirm";
        var cancelText = options && options.cancelText ? options.cancelText : "Cancel";

        return new Promise(function (resolve) {
            root.innerHTML =
                '<div class="attendance-ui-backdrop" id="attendanceConfirmBackdrop">' +
                '  <div class="attendance-ui-modal" role="dialog" aria-modal="true" aria-labelledby="attendanceConfirmTitle">' +
                '    <div class="attendance-ui-title" id="attendanceConfirmTitle">' + title + '</div>' +
                '    <div class="attendance-ui-text">' + message + '</div>' +
                '    <div class="attendance-ui-actions">' +
                '      <button class="attendance-ui-btn" type="button" data-action="cancel">' + cancelText + '</button>' +
                '      <button class="attendance-ui-btn primary" type="button" data-action="confirm">' + confirmText + '</button>' +
                '    </div>' +
                '  </div>' +
                '</div>';

            var backdrop = document.getElementById("attendanceConfirmBackdrop");
            var cancelButton = root.querySelector('[data-action="cancel"]');
            var confirmButton = root.querySelector('[data-action="confirm"]');

            function close(result) {
                document.removeEventListener("keydown", onKeyDown);
                root.innerHTML = "";
                resolve(result);
            }

            function onKeyDown(event) {
                if (event.key === "Escape") {
                    close(false);
                }
            }

            backdrop.addEventListener("click", function (event) {
                if (event.target === backdrop) {
                    close(false);
                }
            });

            cancelButton.addEventListener("click", function () {
                close(false);
            });

            confirmButton.addEventListener("click", function () {
                close(true);
            });

            document.addEventListener("keydown", onKeyDown);
            confirmButton.focus();
        });
    }

    function confirmLogout() {
        return confirmDialog({
            title: "Log Out",
            message: "Are you sure you want to log out of your account?",
            confirmText: "Log Out",
            cancelText: "Stay"
        });
    }

    window.AttendanceApi = {
        request: request,
        getCurrentUser: getCurrentUser,
        setCurrentUser: setCurrentUser,
        clearCurrentUser: clearCurrentUser,
        requireUser: requireUser,
        showGlobalLoading: showGlobalLoading,
        hideGlobalLoading: hideGlobalLoading,
        confirmDialog: confirmDialog,
        confirmLogout: confirmLogout
    };
})();
