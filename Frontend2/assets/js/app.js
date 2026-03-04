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
    formatPercent(n) {
        if (n == null) return '—';
        return n + '%';
    },
    timeAgo(dateStr) {
        if (!dateStr) return '—';
        const diff = Date.now() - new Date(dateStr).getTime();
        const mins = Math.floor(diff / 60000);
        if (mins < 1) return 'just now';
        if (mins < 60) return mins + 'm ago';
        const hrs = Math.floor(mins / 60);
        if (hrs < 24) return hrs + 'h ago';
        const days = Math.floor(hrs / 24);
        if (days < 7) return days + 'd ago';
        return Utils.formatDate(dateStr);
    },
    initials(name) {
        return (name || '??').split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2);
    },
    randomColor() {
        const colors = ['#6366f1', '#8b5cf6', '#14b8a6', '#f43f5e', '#f59e0b', '#0ea5e9', '#10b981', '#d946ef'];
        return colors[Math.floor(Math.random() * colors.length)];
    },
    colorByIndex(i) {
        const colors = ['#6366f1', '#8b5cf6', '#14b8a6', '#f43f5e', '#f59e0b', '#0ea5e9', '#10b981', '#d946ef'];
        return colors[i % colors.length];
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
    },
    gradeColor(letter) {
        const map = { 'A+': '#10b981', 'A': '#10b981', 'B+': '#0ea5e9', 'B': '#0ea5e9', 'C+': '#f59e0b', 'C': '#f59e0b', 'D': '#fb923c', 'F': '#f43f5e' };
        return map[letter] || '#94a3b8';
    },
    gradeChipClass(letter) {
        if (!letter) return 'chip-indigo';
        if (letter.startsWith('A')) return 'chip-emerald';
        if (letter.startsWith('B')) return 'chip-sky';
        if (letter.startsWith('C')) return 'chip-amber';
        return 'chip-rose';
    },
    feeStatusBadge(status) {
        const s = (status || '').toLowerCase();
        if (s === 'paid') return '<span class="badge badge-emerald"><i class="ri-checkbox-circle-fill"></i> Paid</span>';
        if (s === 'overdue') return '<span class="badge badge-rose"><i class="ri-error-warning-fill"></i> Overdue</span>';
        return '<span class="badge badge-amber"><i class="ri-time-fill"></i> Pending</span>';
    },
    attendanceBadge(pct) {
        if (pct == null) return '<span class="badge badge-indigo">—</span>';
        if (pct >= 90) return `<span class="badge badge-emerald">${pct}%</span>`;
        if (pct >= 75) return `<span class="badge badge-teal">${pct}%</span>`;
        if (pct >= 60) return `<span class="badge badge-amber">${pct}%</span>`;
        return `<span class="badge badge-rose">${pct}%</span>`;
    },
    daysUntil(dateStr) {
        if (!dateStr) return null;
        const diff = new Date(dateStr).getTime() - Date.now();
        return Math.ceil(diff / 86400000);
    }
};

/* ─── MiniChart — Pure CSS/JS Chart Components ─── */
const MiniChart = {
    // Donut chart — returns HTML string
    donut(percent, size = 60, color = '#6366f1', label = '') {
        const r = (size / 2) - 4, circ = 2 * Math.PI * r;
        const offset = circ - (Math.min(percent, 100) / 100) * circ;
        return `<div style="position:relative;width:${size}px;height:${size}px;display:inline-flex;align-items:center;justify-content:center;" title="${label || percent + '%'}">
            <svg width="${size}" height="${size}" style="transform:rotate(-90deg)">
                <circle cx="${size / 2}" cy="${size / 2}" r="${r}" fill="none" stroke="rgba(255,255,255,0.06)" stroke-width="4"/>
                <circle cx="${size / 2}" cy="${size / 2}" r="${r}" fill="none" stroke="${color}" stroke-width="4" stroke-linecap="round"
                    stroke-dasharray="${circ}" stroke-dashoffset="${offset}" style="transition:stroke-dashoffset 0.8s ease"/>
            </svg>
            <span style="position:absolute;font-size:${size * 0.22}px;font-weight:800;color:var(--text-primary);">${Math.round(percent)}%</span>
        </div>`;
    },

    // Mini horizontal bar
    bar(value, max, color = '#6366f1', height = 6) {
        const pct = max ? Math.min((value / max) * 100, 100) : 0;
        return `<div style="width:100%;height:${height}px;background:rgba(255,255,255,0.06);border-radius:${height / 2}px;overflow:hidden;">
            <div style="width:${pct}%;height:100%;background:${color};border-radius:${height / 2}px;transition:width 0.6s ease;"></div>
        </div>`;
    },

    // Mini bar chart (vertical bars)
    barChart(values, labels, colors, height = 80) {
        const max = Math.max(...values, 1);
        return `<div style="display:flex;align-items:flex-end;gap:4px;height:${height}px;padding-top:4px;">
            ${values.map((v, i) => {
            const pct = (v / max * 100) || 2;
            const c = colors ? (colors[i] || colors[i % colors.length]) : Utils.colorByIndex(i);
            return `<div style="flex:1;display:flex;flex-direction:column;align-items:center;gap:2px;">
                    <span style="font-size:0.55rem;color:var(--text-secondary);font-weight:700;">${v}</span>
                    <div style="width:100%;height:${pct}%;background:${c};border-radius:3px 3px 0 0;min-height:2px;transition:height 0.6s ease;" title="${labels?.[i] || ''}: ${v}"></div>
                    <span style="font-size:0.5rem;color:var(--text-tertiary);white-space:nowrap;">${labels?.[i] || ''}</span>
                </div>`;
        }).join('')}
        </div>`;
    },

    // Stat row item (icon + label + value)
    statRow(icon, label, value, color = '#6366f1') {
        return `<div style="display:flex;align-items:center;gap:10px;padding:8px 0;border-bottom:1px solid rgba(255,255,255,0.04);">
            <div style="width:32px;height:32px;border-radius:10px;background:${color}20;color:${color};display:grid;place-items:center;font-size:0.95rem;flex-shrink:0;"><i class="${icon}"></i></div>
            <div style="flex:1;"><div style="font-size:0.65rem;text-transform:uppercase;color:var(--text-tertiary);font-weight:700;">${label}</div>
            <div style="font-size:0.88rem;font-weight:600;margin-top:1px;">${value}</div></div>
        </div>`;
    },

    // Summary card
    summaryCard(title, value, subtitle, icon, color = '#6366f1') {
        return `<div style="background:var(--bg-glass);border:1px solid var(--border-subtle);border-radius:18px;padding:16px 18px;display:flex;align-items:center;gap:14px;transition:all 0.3s;cursor:default;" onmouseenter="this.style.transform='translateY(-2px)';this.style.borderColor='var(--border-default)'" onmouseleave="this.style.transform='';this.style.borderColor='var(--border-subtle)'">
            <div style="width:44px;height:44px;border-radius:14px;background:linear-gradient(135deg,${color},${color}cc);display:grid;place-items:center;font-size:1.2rem;color:#fff;flex-shrink:0;"><i class="${icon}"></i></div>
            <div><div style="font-size:1.4rem;font-weight:800;line-height:1;">${value}</div>
            <div style="font-size:0.72rem;text-transform:uppercase;letter-spacing:0.06em;color:var(--text-tertiary);font-weight:600;margin-top:2px;">${title}</div>
            ${subtitle ? `<div style="font-size:0.7rem;color:var(--text-secondary);margin-top:2px;">${subtitle}</div>` : ''}
            </div>
        </div>`;
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
    Offline.init();
    CmdPalette.init();
    NeoTooltip.init('[data-tooltip]');
    NavLayout.init();
    NavGroups.init();
    SidebarSearch.init();
    SidebarCollapse.init();
    NavBadges.init();
    Breadcrumbs.init();
    RecentPages.init();
    NavShortcuts.init();
    ThemeToggle.init();
    ProfileDropdown.init();
});

/* ═══════════════════════════════════════════════════════════
   V2 Components
   ═══════════════════════════════════════════════════════════ */

/* ─── Confirm Dialog (#32) ─── */
const Confirm = {
    show(title, message, onYes, yesLabel = 'Confirm', noLabel = 'Cancel') {
        const el = document.createElement('div');
        el.className = 'confirm-overlay';
        el.innerHTML = `<div class="confirm-box">
            <h4>${title}</h4>
            <p>${message}</p>
            <div class="confirm-actions">
                <button class="btn btn-ghost" id="confirmNo">${noLabel}</button>
                <button class="btn btn-primary" id="confirmYes" style="background:var(--rose-500);">${yesLabel}</button>
            </div>
        </div>`;
        document.body.appendChild(el);
        el.querySelector('#confirmYes').onclick = () => { el.remove(); if (onYes) onYes(); };
        el.querySelector('#confirmNo').onclick = () => el.remove();
        el.addEventListener('click', e => { if (e.target === el) el.remove(); });
        el.querySelector('#confirmNo').focus();
    }
};

/* ─── Command Palette (#61) ─── */
const CmdPalette = {
    overlay: null,
    pages: [
        { name: 'Dashboard', icon: 'ri-dashboard-3-line', url: 'dashboard.html' },
        { name: 'Students', icon: 'ri-group-line', url: 'students.html' },
        { name: 'Courses', icon: 'ri-book-open-line', url: 'courses.html' },
        { name: 'Teachers', icon: 'ri-user-star-line', url: 'teachers.html' },
        { name: 'Attendance', icon: 'ri-calendar-check-line', url: 'attendance.html' },
        { name: 'Enrollment', icon: 'ri-file-list-3-line', url: 'enrollment.html' },
        { name: 'Fees', icon: 'ri-money-dollar-circle-line', url: 'fees.html' },
        { name: 'Exams', icon: 'ri-edit-2-line', url: 'exams.html' },
        { name: 'Timetable', icon: 'ri-calendar-2-line', url: 'timetable.html' },
        { name: 'Messages', icon: 'ri-mail-line', url: 'messages.html' },
    ],

    init() {
        // Create overlay
        const el = document.createElement('div');
        el.className = 'cmd-palette-overlay';
        el.innerHTML = `<div class="cmd-palette">
            <div class="cmd-palette-input">
                <i class="ri-search-line"></i>
                <input type="text" placeholder="Search pages, actions..." id="cmdInput" autocomplete="off">
                <kbd>ESC</kbd>
            </div>
            <div class="cmd-results" id="cmdResults"></div>
        </div>`;
        document.body.appendChild(el);
        this.overlay = el;

        const input = document.getElementById('cmdInput');
        input.addEventListener('input', () => this.search(input.value));
        el.addEventListener('click', e => { if (e.target === el) this.close(); });

        // Keyboard shortcut: Ctrl+K or Cmd+K
        document.addEventListener('keydown', e => {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') { e.preventDefault(); this.toggle(); }
            if (e.key === 'Escape' && this.overlay.classList.contains('active')) this.close();
        });

        this.search(''); // Initial render
    },

    toggle() { this.overlay.classList.contains('active') ? this.close() : this.open(); },
    open() {
        this.overlay.classList.add('active');
        setTimeout(() => document.getElementById('cmdInput').focus(), 50);
    },
    close() {
        this.overlay.classList.remove('active');
        document.getElementById('cmdInput').value = '';
        this.search('');
    },

    search(q) {
        const results = document.getElementById('cmdResults');
        const query = q.toLowerCase().trim();
        const filtered = query ? this.pages.filter(p => p.name.toLowerCase().includes(query)) : this.pages;
        if (!filtered.length) { results.innerHTML = '<div class="cmd-no-results"><i class="ri-search-line" style="font-size:1.5rem;display:block;margin-bottom:8px;"></i>No results for "' + q + '"</div>'; return; }
        results.innerHTML = '<div class="cmd-result-group">Pages</div>' + filtered.map(p =>
            `<div class="cmd-result-item" onclick="location.href='${p.url}'"><i class="${p.icon}"></i><span>${this.highlight(p.name, query)}</span></div>`
        ).join('');
    },

    highlight(text, query) {
        if (!query) return text;
        const idx = text.toLowerCase().indexOf(query);
        if (idx < 0) return text;
        return text.slice(0, idx) + '<strong style="color:var(--indigo-400);">' + text.slice(idx, idx + query.length) + '</strong>' + text.slice(idx + query.length);
    }
};

/* ─── Tooltip (#37) ─── */
const NeoTooltip = {
    init(selector = '[data-tooltip]') {
        document.querySelectorAll(selector).forEach(el => {
            el.addEventListener('mouseenter', () => {
                const tip = document.createElement('div');
                tip.className = 'neo-tooltip';
                tip.textContent = el.getAttribute('data-tooltip');
                document.body.appendChild(tip);
                const r = el.getBoundingClientRect();
                tip.style.left = r.left + r.width / 2 - tip.offsetWidth / 2 + 'px';
                tip.style.top = r.top - tip.offsetHeight - 8 + window.scrollY + 'px';
                el._tooltip = tip;
            });
            el.addEventListener('mouseleave', () => { if (el._tooltip) { el._tooltip.remove(); el._tooltip = null; } });
        });
    }
};

/* ─── Avatar Generator (#35) ─── */
const Avatar = {
    colors: ['#6366f1', '#8b5cf6', '#14b8a6', '#f43f5e', '#f59e0b', '#0ea5e9', '#10b981', '#d946ef'],
    create(name, size = 36) {
        const initials = Utils.initials(name || '??');
        const colorIdx = (name || '').split('').reduce((a, c) => a + c.charCodeAt(0), 0) % this.colors.length;
        return `<div style="width:${size}px;height:${size}px;border-radius:10px;display:grid;place-items:center;font-size:${size * 0.4}px;font-weight:800;color:#fff;background:${this.colors[colorIdx]};flex-shrink:0;">${initials}</div>`;
    }
};

/* ─── Badge Counter (#36) ─── */
const Badge = {
    update(elementId, count) {
        const el = document.getElementById(elementId);
        if (!el) return;
        const old = parseInt(el.textContent) || 0;
        el.textContent = count;
        if (count !== old) {
            el.style.transform = 'scale(1.3)';
            setTimeout(() => { el.style.transform = 'scale(1)'; el.style.transition = 'transform .2s var(--ease-spring)'; }, 50);
        }
    }
};

/* ─── Offline Indicator (#15) ─── */
const Offline = {
    banner: null,
    init() {
        const b = document.createElement('div');
        b.className = 'offline-banner';
        b.innerHTML = '<i class="ri-wifi-off-line"></i> You are offline — data may be outdated';
        document.body.prepend(b);
        this.banner = b;
        window.addEventListener('offline', () => b.classList.add('show'));
        window.addEventListener('online', () => b.classList.remove('show'));
        if (!navigator.onLine) b.classList.add('show');
    }
};

/* ─── Navigation Layout Toggle ─── */
const NavLayout = {
    KEY: 'neo-nav-layout',
    btn: null,
    init() {
        const btn = document.createElement('button');
        btn.className = 'layout-toggle';
        btn.setAttribute('aria-label', 'Toggle navigation layout');
        btn.innerHTML = '<i class="ri-layout-left-line"></i><i class="ri-layout-top-line"></i><span class="toggle-tip">Switch layout</span>';
        document.body.appendChild(btn);
        this.btn = btn;
        const overlay = document.createElement('div');
        overlay.className = 'sidebar-overlay';
        overlay.addEventListener('click', () => document.body.classList.remove('sidebar-open'));
        document.body.appendChild(overlay);
        const saved = localStorage.getItem(this.KEY);
        if (saved !== 'header') document.body.classList.add('sidebar-mode');
        this.updateTip();
        btn.addEventListener('click', () => this.toggle());
    },
    toggle() {
        const isSidebar = document.body.classList.toggle('sidebar-mode');
        document.body.classList.remove('sidebar-open');
        localStorage.setItem(this.KEY, isSidebar ? 'sidebar' : 'header');
        this.updateTip();
    },
    updateTip() {
        const tip = this.btn?.querySelector('.toggle-tip');
        if (tip) tip.textContent = document.body.classList.contains('sidebar-mode') ? 'Switch to header' : 'Switch to sidebar';
    }
};

/* ─── #1: Collapsible Sidebar ─── */
const SidebarCollapse = {
    KEY: 'neo-sidebar-mini',
    init() {
        const nav = document.querySelector('.header-nav');
        if (!nav) return;
        const btn = document.createElement('button');
        btn.className = 'sidebar-collapse-btn';
        btn.innerHTML = '<i class="ri-arrow-left-s-line"></i>';
        btn.title = 'Collapse sidebar';
        btn.addEventListener('click', () => this.toggle());
        nav.parentElement.insertBefore(btn, nav);
        if (localStorage.getItem(this.KEY) === 'true') document.body.classList.add('sidebar-mini');
    },
    toggle() {
        const mini = document.body.classList.toggle('sidebar-mini');
        localStorage.setItem(this.KEY, mini);
    }
};

/* ─── #2: Breadcrumbs ─── */
const Breadcrumbs = {
    pageNames: {
        'dashboard': { name: 'Dashboard', icon: 'ri-dashboard-3-line' },
        'students': { name: 'Students', icon: 'ri-group-line' },
        'courses': { name: 'Courses', icon: 'ri-book-open-line' },
        'teachers': { name: 'Teachers', icon: 'ri-user-star-line' },
        'timetable': { name: 'Timetable', icon: 'ri-calendar-2-line' },
        'enrollment': { name: 'Enrollment', icon: 'ri-file-list-3-line' },
        'fees': { name: 'Fees', icon: 'ri-money-dollar-circle-line' },
        'attendance': { name: 'Attendance', icon: 'ri-calendar-check-line' },
        'exams': { name: 'Exams', icon: 'ri-edit-2-line' },
        'messages': { name: 'Messages', icon: 'ri-mail-line' },
    },
    init() {
        const page = (window.location.pathname.split('/').pop() || 'dashboard').replace('.html', '');
        const info = this.pageNames[page] || { name: page, icon: 'ri-file-line' };
        const bc = document.createElement('div');
        bc.className = 'neo-breadcrumbs';
        bc.innerHTML = `<a href="dashboard.html"><i class="ri-home-4-line"></i> Home</a><i class="ri-arrow-right-s-line bc-sep"></i><span class="bc-current"><i class="${info.icon}"></i> ${info.name}</span>`;
        const wrapper = document.querySelector('.page-wrapper .container');
        if (wrapper && wrapper.firstChild) wrapper.insertBefore(bc, wrapper.firstChild);
    }
};

/* ─── #4: Notification Badges ─── */
const NavBadges = {
    init() {
        document.querySelectorAll('.header-nav a').forEach(link => {
            const badge = document.createElement('span');
            badge.className = 'nav-badge';
            link.style.position = 'relative';
            link.appendChild(badge);
            const href = link.getAttribute('href') || '';
            if (href.includes('messages')) badge.dataset.badgeKey = 'messages';
            else if (href.includes('attendance')) badge.dataset.badgeKey = 'attendance';
            else if (href.includes('fees')) badge.dataset.badgeKey = 'fees';
        });
    },
    update(key, count) {
        document.querySelectorAll(`.nav-badge[data-badge-key="${key}"]`).forEach(b => {
            b.textContent = count > 0 ? (count > 99 ? '99+' : count) : '';
        });
    }
};

/* ─── #5: Sidebar Search ─── */
const SidebarSearch = {
    init() {
        const nav = document.querySelector('.header-nav');
        if (!nav) return;
        const box = document.createElement('div');
        box.className = 'sidebar-search';
        box.innerHTML = '<i class="ri-search-line"></i><input type="text" placeholder="Search pages...">';
        nav.parentElement.insertBefore(box, nav);
        box.querySelector('input').addEventListener('input', (e) => this.filter(e.target.value));
    },
    filter(q) {
        const lq = q.toLowerCase();
        document.querySelectorAll('.header-nav a').forEach(link => {
            link.style.display = link.textContent.toLowerCase().includes(lq) || !lq ? '' : 'none';
        });
        document.querySelectorAll('.nav-section-label').forEach(l => { l.style.display = lq ? 'none' : ''; });
    }
};

/* ─── #6: Grouped Nav Sections ─── */
const NavGroups = {
    groups: [
        { label: 'Overview', items: ['dashboard.html'] },
        { label: 'Academic', items: ['courses.html', 'timetable.html', 'exams.html'] },
        { label: 'People', items: ['students.html', 'teachers.html'] },
        { label: 'Operations', items: ['enrollment.html', 'attendance.html'] },
        { label: 'Finance & Comms', items: ['fees.html', 'messages.html'] },
    ],
    init() {
        const nav = document.querySelector('.header-nav');
        if (!nav) return;
        const links = [...nav.querySelectorAll('a')];
        const frag = document.createDocumentFragment();
        this.groups.forEach(g => {
            const label = document.createElement('div');
            label.className = 'nav-section-label';
            label.textContent = g.label;
            frag.appendChild(label);
            g.items.forEach(href => {
                const link = links.find(l => (l.getAttribute('href') || '').includes(href));
                if (link) frag.appendChild(link);
            });
        });
        links.forEach(l => { if (!frag.contains(l)) frag.appendChild(l); });
        nav.innerHTML = '';
        nav.appendChild(frag);
    }
};

/* ─── #7: Recent Pages ─── */
const RecentPages = {
    KEY: 'neo-recent-pages',
    MAX: 3,
    icons: { 'dashboard': 'ri-dashboard-3-line', 'students': 'ri-group-line', 'courses': 'ri-book-open-line', 'teachers': 'ri-user-star-line', 'timetable': 'ri-calendar-2-line', 'enrollment': 'ri-file-list-3-line', 'fees': 'ri-money-dollar-circle-line', 'attendance': 'ri-calendar-check-line', 'exams': 'ri-edit-2-line', 'messages': 'ri-mail-line' },
    init() {
        this.track(); this.render();
    },
    track() {
        const page = window.location.pathname.split('/').pop() || 'dashboard.html';
        let r = JSON.parse(localStorage.getItem(this.KEY) || '[]').filter(x => x !== page);
        r.unshift(page);
        localStorage.setItem(this.KEY, JSON.stringify(r.slice(0, this.MAX + 1)));
    },
    render() {
        const nav = document.querySelector('.header-nav');
        if (!nav) return;
        const current = window.location.pathname.split('/').pop() || 'dashboard.html';
        const recents = JSON.parse(localStorage.getItem(this.KEY) || '[]').filter(r => r !== current).slice(0, this.MAX);
        if (!recents.length) return;
        const c = document.createElement('div');
        c.className = 'sidebar-recents';
        c.innerHTML = '<div class="sr-label">Recent</div>' + recents.map(r => {
            const n = r.replace('.html', '');
            return `<a href="${r}"><i class="${this.icons[n] || 'ri-file-line'}"></i> ${n.charAt(0).toUpperCase() + n.slice(1)}</a>`;
        }).join('');
        nav.parentElement.appendChild(c);
    }
};

/* ─── #8: Keyboard Shortcuts (Alt+1-9) ─── */
const NavShortcuts = {
    init() {
        const links = document.querySelectorAll('.header-nav a');
        document.addEventListener('keydown', (e) => {
            if (e.altKey && !e.ctrlKey && !e.metaKey) {
                const n = parseInt(e.key);
                if (n >= 1 && n <= 9 && links[n - 1]) { e.preventDefault(); links[n - 1].click(); }
            }
        });
    }
};

/* ─── #9: Theme Toggle ─── */
const ThemeToggle = {
    KEY: 'neo-theme',
    init() {
        const btn = document.createElement('button');
        btn.className = 'theme-toggle-btn';
        btn.title = 'Toggle theme';
        btn.innerHTML = '<i class="ri-moon-line"></i><i class="ri-sun-line"></i>';
        btn.addEventListener('click', () => this.toggle());
        const actions = document.querySelector('.header-actions');
        if (actions) actions.insertBefore(btn, actions.firstChild);
        if (localStorage.getItem(this.KEY) === 'light') document.body.classList.add('light-mode');
    },
    toggle() {
        const isLight = document.body.classList.toggle('light-mode');
        localStorage.setItem(this.KEY, isLight ? 'light' : 'dark');
    }
};

/* ─── #10: Profile Dropdown ─── */
const ProfileDropdown = {
    init() {
        const avatar = document.getElementById('headerAvatar');
        if (!avatar) return;
        const user = JSON.parse(localStorage.getItem('user') || '{}');
        const name = user.firstName ? `${user.firstName} ${user.lastName || ''}`.trim() : 'Admin User';
        const role = user.role || 'Admin';
        const dd = document.createElement('div');
        dd.className = 'profile-dropdown';
        dd.innerHTML = `<div class="pd-header"><div class="pd-name">${name}</div><div class="pd-role">${role}</div></div>
            <a href="dashboard.html"><i class="ri-dashboard-3-line"></i> Dashboard</a>
            <button onclick="ThemeToggle.toggle()"><i class="ri-palette-line"></i> Toggle Theme</button>
            <button onclick="NavLayout.toggle()"><i class="ri-layout-line"></i> Switch Layout</button>
            <button class="pd-danger" onclick="Auth.logout()"><i class="ri-logout-box-r-line"></i> Sign Out</button>`;
        const actions = document.querySelector('.header-actions');
        if (actions) actions.appendChild(dd);
        avatar.addEventListener('click', (e) => { e.stopPropagation(); dd.classList.toggle('open'); });
        document.addEventListener('click', (e) => { if (!dd.contains(e.target) && e.target !== avatar) dd.classList.remove('open'); });
    }
};


