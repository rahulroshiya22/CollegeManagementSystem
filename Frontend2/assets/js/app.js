/* ═══════════════════════════════════════════════════════════
   NeoVerse CMS — Auth & App Utilities
   ═══════════════════════════════════════════════════════════ */

const Auth = {
    getToken() { return localStorage.getItem('cms_token'); },
    getUser() { try { return JSON.parse(localStorage.getItem('cms_user')); } catch { return null; } },
    getRole() { return this.getUser()?.role || ''; },
    isLoggedIn() { return !!this.getToken(); },

    save(token, user) {
        localStorage.setItem('cms_token', token);
        localStorage.setItem('cms_user', JSON.stringify(user));
    },

    logout() {
        localStorage.removeItem('cms_token');
        localStorage.removeItem('cms_user');
        location.href = (location.pathname.includes('/pages/') ? '' : 'pages/') + 'login.html';
    },

    requireAuth() {
        if (!this.isLoggedIn()) {
            location.href = (location.pathname.includes('/pages/') ? '' : 'pages/') + 'login.html';
            return false;
        }
        return true;
    },

    parseJwt(token) {
        try {
            const b = token.split('.')[1];
            return JSON.parse(atob(b.replace(/-/g, '+').replace(/_/g, '/')));
        } catch { return null; }
    }
};

/* ─── Toast System ─── */
const Toast = {
    container: null,

    init() {
        if (this.container) return;
        this.container = document.createElement('div');
        this.container.className = 'toast-container';
        document.body.appendChild(this.container);
    },

    show(msg, type = 'info', duration = 4000) {
        this.init();
        const icons = { success: 'ri-checkbox-circle-fill', error: 'ri-error-warning-fill', info: 'ri-information-fill' };
        const colors = { success: '#10b981', error: '#f43f5e', info: '#0ea5e9' };
        const t = document.createElement('div');
        t.className = `toast ${type}`;
        t.innerHTML = `<i class="${icons[type]}" style="color:${colors[type]}"></i><span>${msg}</span>`;
        this.container.appendChild(t);
        setTimeout(() => { t.style.opacity = '0'; t.style.transform = 'translateX(100%)'; setTimeout(() => t.remove(), 300); }, duration);
    },

    success(m) { this.show(m, 'success'); },
    error(m) { this.show(m, 'error'); },
    info(m) { this.show(m, 'info'); }
};

/* ─── Modal Manager ─── */
const Modal = {
    open(id) {
        const el = document.getElementById(id);
        if (el) { el.classList.add('active'); document.body.style.overflow = 'hidden'; }
    },
    close(id) {
        const el = document.getElementById(id);
        if (el) { el.classList.remove('active'); document.body.style.overflow = ''; }
    }
};

/* ─── Utility Helpers ─── */
const Utils = {
    formatDate(d) {
        if (!d) return '—';
        return new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
    },
    formatCurrency(n) {
        return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(n || 0);
    },
    initials(name) {
        return (name || '??').split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2);
    },
    randomColor() {
        const colors = ['#6366f1', '#8b5cf6', '#14b8a6', '#f43f5e', '#f59e0b', '#0ea5e9', '#10b981', '#d946ef'];
        return colors[Math.floor(Math.random() * colors.length)];
    },
    animateCounter(el, target, duration = 1200) {
        let start = 0;
        const step = (ts) => {
            if (!start) start = ts;
            const p = Math.min((ts - start) / duration, 1);
            const ease = 1 - Math.pow(1 - p, 3);
            el.textContent = Math.floor(ease * target).toLocaleString();
            if (p < 1) requestAnimationFrame(step);
        };
        requestAnimationFrame(step);
    },
    debounce(fn, ms = 300) {
        let t;
        return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), ms); };
    }
};

/* ─── Header Init ─── */
function initHeader() {
    const toggle = document.querySelector('.menu-toggle');
    const nav = document.querySelector('.header-nav');
    if (toggle && nav) {
        toggle.addEventListener('click', () => nav.classList.toggle('mobile-open'));
    }
    // Scroll effect
    window.addEventListener('scroll', () => {
        const header = document.querySelector('.neo-header');
        if (header) header.classList.toggle('scrolled', window.scrollY > 20);
    });
    // Set active nav link
    const path = location.pathname.split('/').pop();
    document.querySelectorAll('.header-nav a').forEach(a => {
        if (a.getAttribute('href')?.includes(path)) a.classList.add('active');
    });
    // User info in header
    const user = Auth.getUser();
    const avatar = document.querySelector('.header-avatar');
    if (avatar && user) {
        avatar.textContent = Utils.initials(user.displayName || user.email);
        avatar.title = user.displayName || user.email;
    }
}

/* ─── 3D Card Tilt Effect ─── */
function initTiltCards() {
    document.querySelectorAll('.glass-card.tilt').forEach(card => {
        card.addEventListener('mousemove', e => {
            const rect = card.getBoundingClientRect();
            const x = (e.clientX - rect.left) / rect.width - 0.5;
            const y = (e.clientY - rect.top) / rect.height - 0.5;
            card.style.transform = `perspective(1000px) rotateY(${x * 6}deg) rotateX(${-y * 6}deg) translateY(-2px)`;
        });
        card.addEventListener('mouseleave', () => {
            card.style.transform = '';
        });
    });
}

/* ─── Page Load ─── */
document.addEventListener('DOMContentLoaded', () => {
    initHeader();
    initTiltCards();
});
