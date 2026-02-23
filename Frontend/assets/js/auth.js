/**
 * ========================================
 * 🔐 Authentication & Authorization Module
 * College Management System - Frontend
 * ========================================
 * Handles JWT token management, role-based routing,
 * and page-level access control.
 */

const AUTH_CONFIG = {
    API_URL: 'https://localhost:7000/api',
    TOKEN_KEY: 'accessToken',
    REFRESH_KEY: 'refreshToken',
    USER_KEY: 'userInfo',
    LOGIN_PAGE: '/pages/login.html',
    ROLES: {
        STUDENT: 'Student',
        TEACHER: 'Teacher',
        ADMIN: 'Admin'
    },
    // Role-based default dashboards
    DASHBOARDS: {
        Student: '/pages/student-dashboard.html',
        Teacher: '/pages/teacher-dashboard.html',
        Admin: '/pages/admin-dashboard.html'
    }
};

/**
 * Auth Manager — Singleton for managing authentication state
 */
class AuthManager {
    // ──────── Token Management ────────

    static getToken() {
        return localStorage.getItem(AUTH_CONFIG.TOKEN_KEY);
    }

    static getRefreshToken() {
        return localStorage.getItem(AUTH_CONFIG.REFRESH_KEY);
    }

    static setTokens(accessToken, refreshToken) {
        localStorage.setItem(AUTH_CONFIG.TOKEN_KEY, accessToken);
        if (refreshToken) localStorage.setItem(AUTH_CONFIG.REFRESH_KEY, refreshToken);
    }

    static clearTokens() {
        localStorage.removeItem(AUTH_CONFIG.TOKEN_KEY);
        localStorage.removeItem(AUTH_CONFIG.REFRESH_KEY);
        localStorage.removeItem(AUTH_CONFIG.USER_KEY);
    }

    // ──────── User Info ────────

    static getUser() {
        const userStr = localStorage.getItem(AUTH_CONFIG.USER_KEY);
        if (!userStr) return null;
        try {
            return JSON.parse(userStr);
        } catch {
            return null;
        }
    }

    static setUser(user) {
        localStorage.setItem(AUTH_CONFIG.USER_KEY, JSON.stringify(user));
    }

    static getRole() {
        const user = AuthManager.getUser();
        return user?.role || null;
    }

    static getUserId() {
        const user = AuthManager.getUser();
        return user?.userId || null;
    }

    static getFullName() {
        const user = AuthManager.getUser();
        if (!user) return '';
        return `${user.firstName || ''} ${user.lastName || ''}`.trim();
    }

    // ──────── Auth State ────────

    static isLoggedIn() {
        const token = AuthManager.getToken();
        if (!token) return false;

        // Check if token is expired (decode JWT payload)
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            const expiry = payload.exp * 1000; // Convert to ms
            if (Date.now() >= expiry) {
                AuthManager.clearTokens();
                return false;
            }
            return true;
        } catch {
            return false;
        }
    }

    static isRole(role) {
        return AuthManager.getRole() === role;
    }

    static isAdmin() { return AuthManager.isRole(AUTH_CONFIG.ROLES.ADMIN); }
    static isTeacher() { return AuthManager.isRole(AUTH_CONFIG.ROLES.TEACHER); }
    static isStudent() { return AuthManager.isRole(AUTH_CONFIG.ROLES.STUDENT); }

    // ──────── Login / Logout ────────

    static async login(email, password) {
        const response = await fetch(`${AUTH_CONFIG.API_URL}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.message || 'Login failed');
        }

        // Store tokens and user info
        AuthManager.setTokens(data.accessToken, data.refreshToken);
        AuthManager.setUser({
            userId: data.userId,
            email: data.email,
            firstName: data.firstName,
            lastName: data.lastName,
            role: data.role,
            photoUrl: data.photoUrl
        });

        return data;
    }

    static logout() {
        AuthManager.clearTokens();
        window.location.href = AuthManager.resolveBasePath() + 'pages/login.html';
    }

    // ──────── Role-Based Redirect ────────

    static redirectToDashboard() {
        const role = AuthManager.getRole();
        const basePath = AuthManager.resolveBasePath();
        const dashboard = AUTH_CONFIG.DASHBOARDS[role] || '/pages/admin-dashboard.html';
        window.location.href = basePath + dashboard.replace(/^\//, '');
    }

    static resolveBasePath() {
        const path = window.location.pathname;
        if (path.includes('/pages/')) {
            return '../';
        }
        return './';
    }

    // ──────── Protected Fetch (adds JWT header) ────────

    static async apiFetch(endpoint, options = {}) {
        const token = AuthManager.getToken();
        const headers = {
            'Content-Type': 'application/json',
            ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
            ...options.headers
        };

        const response = await fetch(`${AUTH_CONFIG.API_URL}${endpoint.toLowerCase()}`, {
            ...options,
            headers
        });

        // If 401, try to redirect to login
        if (response.status === 401) {
            AuthManager.logout();
            return;
        }

        return response;
    }

    static async apiGet(endpoint) {
        const response = await AuthManager.apiFetch(endpoint);
        if (!response) return null;
        if (!response.ok) {
            const err = await response.json().catch(() => ({}));
            throw new Error(err.message || 'Request failed');
        }
        return response.json();
    }

    static async apiPost(endpoint, body) {
        const response = await AuthManager.apiFetch(endpoint, {
            method: 'POST',
            body: JSON.stringify(body)
        });
        if (!response) return null;
        if (!response.ok) {
            const err = await response.json().catch(() => ({}));
            throw new Error(err.message || 'Request failed');
        }
        return response.json();
    }

    static async apiPut(endpoint, body) {
        const response = await AuthManager.apiFetch(endpoint, {
            method: 'PUT',
            body: JSON.stringify(body)
        });
        if (!response) return null;
        if (!response.ok) {
            const err = await response.json().catch(() => ({}));
            throw new Error(err.message || 'Request failed');
        }
        return response.json();
    }

    static async apiDelete(endpoint) {
        const response = await AuthManager.apiFetch(endpoint, { method: 'DELETE' });
        if (!response) return null;
        if (!response.ok) {
            const err = await response.json().catch(() => ({}));
            throw new Error(err.message || 'Request failed');
        }
        return response.json();
    }

    // ──────── Page Guards ────────

    /**
     * Call this at the top of any protected page.
     * Pass allowed roles as arguments: requireAuth('Admin', 'Teacher')
     * If no roles given, any logged-in user can access.
     */
    static requireAuth(...allowedRoles) {
        if (!AuthManager.isLoggedIn()) {
            window.location.href = AuthManager.resolveBasePath() + 'pages/login.html';
            return false;
        }

        if (allowedRoles.length > 0) {
            const userRole = AuthManager.getRole();
            if (!allowedRoles.includes(userRole)) {
                // Redirect to their proper dashboard
                AuthManager.redirectToDashboard();
                return false;
            }
        }
        return true;
    }
}

// ──────── Sidebar Component Generator ────────

function generateSidebar(activePage = '') {
    const role = AuthManager.getRole();
    const user = AuthManager.getUser();
    const fullName = AuthManager.getFullName();

    const menuItems = getSidebarMenuForRole(role);

    return `
    <aside class="sidebar" id="sidebar">
        <div class="sidebar-header">
            <div class="sidebar-logo">
                <i class="ri-graduation-cap-fill"></i>
            </div>
            <h3 class="sidebar-title">CMS</h3>
            <p class="sidebar-subtitle">${role} Panel</p>
        </div>

        <nav class="sidebar-nav">
            ${menuItems.map(item => `
                <a href="${item.href}" class="sidebar-link ${activePage === item.id ? 'active' : ''}" id="nav-${item.id}">
                    <i class="${item.icon}"></i>
                    <span>${item.label}</span>
                </a>
            `).join('')}
        </nav>

        <div class="sidebar-footer">
            <div class="sidebar-user">
                <div class="sidebar-avatar">
                    ${user?.photoUrl ? `<img src="${user.photoUrl}" alt="${fullName}">` : `<span>${(user?.firstName?.[0] || 'U').toUpperCase()}</span>`}
                </div>
                <div class="sidebar-user-info">
                    <span class="sidebar-user-name">${fullName}</span>
                    <span class="sidebar-user-role">${role}</span>
                </div>
            </div>
            <button class="sidebar-logout" onclick="AuthManager.logout()" title="Logout">
                <i class="ri-logout-box-r-line"></i>
            </button>
        </div>
    </aside>`;
}

function getSidebarMenuForRole(role) {
    const base = '../pages/'; // relative from pages directory

    const common = [
        { id: 'notices', label: 'Notices', icon: 'ri-notification-3-fill', href: `${base}notices.html` },
        { id: 'ai-chat', label: 'AI Assistant', icon: 'ri-robot-fill', href: `${base}ai-assistant.html` }
    ];

    if (role === 'Admin') {
        return [
            { id: 'dashboard', label: 'Dashboard', icon: 'ri-dashboard-3-fill', href: `${base}admin-dashboard.html` },
            { id: 'users', label: 'Users', icon: 'ri-group-fill', href: `${base}users.html` },
            { id: 'teachers', label: 'Teachers', icon: 'ri-user-star-fill', href: `${base}teachers.html` },
            { id: 'students', label: 'Students', icon: 'ri-user-3-fill', href: `${base}students.html` },
            { id: 'departments', label: 'Departments', icon: 'ri-building-2-fill', href: `${base}departments.html` },
            { id: 'courses', label: 'Courses', icon: 'ri-book-2-fill', href: `${base}courses.html` },
            { id: 'timetable', label: 'Timetable', icon: 'ri-calendar-schedule-fill', href: `${base}timetable-manage.html` },
            { id: 'enrollment', label: 'Enrollment', icon: 'ri-file-list-3-fill', href: `${base}enrollment.html` },
            { id: 'attendance', label: 'Attendance', icon: 'ri-checkbox-multiple-fill', href: `${base}attendance.html` },
            { id: 'fees', label: 'Fees', icon: 'ri-money-dollar-circle-fill', href: `${base}fees.html` },
            { id: 'grades', label: 'Grades', icon: 'ri-award-fill', href: `${base}grades-manage.html` },
            ...common
        ];
    }

    if (role === 'Teacher') {
        return [
            { id: 'dashboard', label: 'Dashboard', icon: 'ri-dashboard-3-fill', href: `${base}teacher-dashboard.html` },
            { id: 'my-courses', label: 'My Courses', icon: 'ri-book-2-fill', href: `${base}teacher-courses.html` },
            { id: 'timetable', label: 'My Timetable', icon: 'ri-calendar-schedule-fill', href: `${base}teacher-timetable.html` },
            { id: 'mark-attendance', label: 'Mark Attendance', icon: 'ri-checkbox-multiple-fill', href: `${base}mark-attendance.html` },
            { id: 'grades', label: 'Manage Grades', icon: 'ri-award-fill', href: `${base}teacher-grades.html` },
            ...common
        ];
    }

    // Student
    return [
        { id: 'dashboard', label: 'Dashboard', icon: 'ri-dashboard-3-fill', href: `${base}student-dashboard.html` },
        { id: 'my-courses', label: 'My Courses', icon: 'ri-book-2-fill', href: `${base}my-courses.html` },
        { id: 'timetable', label: 'Timetable', icon: 'ri-calendar-schedule-fill', href: `${base}my-timetable.html` },
        { id: 'my-attendance', label: 'Attendance', icon: 'ri-checkbox-multiple-fill', href: `${base}my-attendance.html` },
        { id: 'my-fees', label: 'Fees', icon: 'ri-money-dollar-circle-fill', href: `${base}my-fees.html` },
        { id: 'my-grades', label: 'My Grades', icon: 'ri-award-fill', href: `${base}my-grades.html` },
        ...common
    ];
}

// ──────── Header Component Generator ────────

function generateHeader(pageTitle = 'Dashboard') {
    const user = AuthManager.getUser();
    const fullName = AuthManager.getFullName();

    return `
    <header class="page-header">
        <div class="header-left">
            <button class="sidebar-toggle" onclick="toggleSidebar()" id="sidebarToggle">
                <i class="ri-menu-line"></i>
            </button>
            <div>
                <h1 class="header-title">${pageTitle}</h1>
                <p class="header-subtitle">Welcome back, ${user?.firstName || 'User'}</p>
            </div>
        </div>
        <div class="header-right">
            <button class="header-icon-btn" onclick="toggleTheme()" title="Toggle Theme">
                <i class="ri-moon-fill" id="themeIcon"></i>
            </button>
            <div class="header-user">
                <div class="header-avatar">
                    ${user?.photoUrl ? `<img src="${user.photoUrl}" alt="${fullName}">` : `<span>${(user?.firstName?.[0] || 'U').toUpperCase()}</span>`}
                </div>
                <span class="header-user-name">${fullName}</span>
            </div>
        </div>
    </header>`;
}

// ──────── Theme Toggle ────────

function toggleTheme() {
    const html = document.documentElement;
    const current = html.getAttribute('data-theme') || 'light';
    const next = current === 'light' ? 'dark' : 'light';
    html.setAttribute('data-theme', next);
    localStorage.setItem('cms-theme', next);

    const icon = document.getElementById('themeIcon');
    if (icon) icon.className = next === 'dark' ? 'ri-sun-fill' : 'ri-moon-fill';
}

function loadTheme() {
    const theme = localStorage.getItem('cms-theme') || 'light';
    document.documentElement.setAttribute('data-theme', theme);
    setTimeout(() => {
        const icon = document.getElementById('themeIcon');
        if (icon) icon.className = theme === 'dark' ? 'ri-sun-fill' : 'ri-moon-fill';
    }, 100);
}

// ──────── Sidebar Toggle ────────

function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const layout = document.querySelector('.layout');
    if (sidebar) sidebar.classList.toggle('collapsed');
    if (layout) layout.classList.toggle('sidebar-collapsed');
}

// ──────── Toast Notifications ────────

function showToast(message, type = 'info', duration = 3000) {
    const container = document.getElementById('toastContainer') || createToastContainer();
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;

    const icons = { success: 'ri-check-line', error: 'ri-error-warning-line', warning: 'ri-alert-line', info: 'ri-information-line' };

    toast.innerHTML = `
        <i class="${icons[type] || icons.info}"></i>
        <span>${message}</span>
    `;

    container.appendChild(toast);
    requestAnimationFrame(() => toast.classList.add('show'));

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toastContainer';
    container.className = 'toast-container';
    document.body.appendChild(container);
    return container;
}

// ──────── Page Initialization Helper ────────

function initPage(pageTitle, activeSidebarId, ...allowedRoles) {
    // Guard
    if (!AuthManager.requireAuth(...allowedRoles)) return false;

    // Load theme
    loadTheme();

    // Inject sidebar
    const sidebarContainer = document.getElementById('sidebarContainer');
    if (sidebarContainer) sidebarContainer.innerHTML = generateSidebar(activeSidebarId);

    // Inject header
    const headerContainer = document.getElementById('headerContainer');
    if (headerContainer) headerContainer.innerHTML = generateHeader(pageTitle);

    return true;
}
