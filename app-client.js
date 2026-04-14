(function () {
    // Shared helper methods for all frontend pages.
    var USER_STORAGE_KEY = "attendanceHubCurrentUser";
    var DEFAULT_USER = {
        userId: "1",
        fullName: "Lance Timothy Satorre",
        studentId: "2026-1234",
        yearLevel: "Year 1",
        course: "BS Information Technology",
        email: "lanceerrotas@gmail.com"
    };

    // Send an API request and return JSON result.
    // Example: request('/api/register', { method: 'POST', body: JSON.stringify(data) })
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

    // Read the logged-in user from localStorage.
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

    // Save user data in localStorage.
    function setCurrentUser(user) {
        localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
    }

    // Remove saved user data from localStorage.
    function clearCurrentUser() {
        localStorage.removeItem(USER_STORAGE_KEY);
    }

    // Get the current user, or use default user in beginner mode.
    function requireUser(redirectPath) {
        var user = getCurrentUser();
        if (user) {
            return user;
        }

        // Beginner mode: if no login yet, use a default student profile.
        setCurrentUser(DEFAULT_USER);
        return DEFAULT_USER;
    }

    // Ask confirmation before logout.
    function confirmLogout() {
        return Promise.resolve(window.confirm("Are you sure you want to log out of your account?"));
    }

    // Expose helper functions as AttendanceApi.
    window.AttendanceApi = {
        request: request,
        getCurrentUser: getCurrentUser,
        setCurrentUser: setCurrentUser,
        clearCurrentUser: clearCurrentUser,
        requireUser: requireUser,
        confirmLogout: confirmLogout
    };
})();
