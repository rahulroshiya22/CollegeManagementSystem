/* CMS V3 Main JS | GSAP Animation Controller */

document.addEventListener('DOMContentLoaded', () => {

    // 1. Initial Page Load (Stagger Reveals)
    // Overriding the CSS 'visibility: hidden' so JS takes control
    gsap.set('.gs-reveal', { visibility: 'visible' });

    gsap.from('.gs-reveal', {
        y: 40,
        opacity: 0,
        duration: 0.8,
        stagger: 0.1, // Slight delay between each element popping up
        ease: 'power3.out',
        delay: 0.2 // Small delay to let fonts and CSS load fully
    });

    // 2. Magnetic Sidebar Buttons (Micro-animation)
    const navItems = document.querySelectorAll('.nav-item');

    navItems.forEach(item => {
        const icon = item.querySelector('i'); // Target the Phosphor Icon

        item.addEventListener('mousemove', (e) => {
            const rect = item.getBoundingClientRect();
            // Calculate distance from center of the button
            const x = e.clientX - rect.left - rect.width / 2;
            const y = e.clientY - rect.top - rect.height / 2;

            // Subtle magnetic pull (15% strength)
            gsap.to(icon, {
                x: x * 0.15,
                y: y * 0.15,
                duration: 0.3,
                ease: 'power2.out'
            });
        });

        item.addEventListener('mouseleave', () => {
            // Spring back to center
            gsap.to(icon, {
                x: 0,
                y: 0,
                duration: 0.8,
                ease: 'elastic.out(1, 0.3)'
            });
        });
    });

    // 3. Dynamic Number Counters for KPI Cards
    const animateCounter = (elementId, startVal, endVal, prefix = '') => {
        const el = document.getElementById(elementId);
        if (!el) return;

        const obj = { val: startVal };
        gsap.to(obj, {
            val: endVal,
            duration: 2.5,
            ease: 'power3.out',
            delay: 0.8, // Wait for cards to slide in
            onUpdate: function () {
                // Add commas for thousands
                el.innerText = prefix + Math.floor(obj.val).toLocaleString();
            }
        });
    };

    // 3.5 Real-time Data Fetching Interface
    // 3.5 Real-time Data Fetching Interface & Grid Population
    const loadDashboardData = async () => {
        try {
            // Attempt to fetch real stats from microservices
            const stats = await API.getDashboardStats();
            animateCounter('stat-students', 0, stats.totalStudents || 1450);
            animateCounter('stat-revenue', 0, stats.totalRevenue || 2450000, '₹');
        } catch (error) {
            console.log('Stats backend unavailable. Falling back to placeholder UI data.');
            animateCounter('stat-students', 0, 1450);
            animateCounter('stat-revenue', 0, 2450000, '₹');
        }

        try {
            // Fetch massive datasets for the Data Grid
            const students = await API.getRecentStudents();
            renderStudentGrid(students);
        } catch (error) {
            console.log('Students backend unavailable. Using UI skeleton.');
            // Only keeping the skeleton rows if the API fails
        }
    };

    const renderStudentGrid = (students) => {
        const tbody = document.querySelector('tbody');
        if (!tbody || !Array.isArray(students)) return;

        // Clear skeleton rows
        tbody.innerHTML = '';

        students.forEach((s, index) => {
            // Defaulting some values for UI elegance if missing from DB
            const imageUrl = s.profilePictureUrl || `https://i.pravatar.cc/150?img=${(index % 50) + 1}`;
            const dept = s.department?.departmentName || 'General';
            const yearStr = s.createdAt ? new Date(s.createdAt).getFullYear() : 'New';

            const tr = document.createElement('tr');
            tr.className = 'border-b border-glassBorder/50 hover:bg-obsidian-700/30 transition-colors group cursor-pointer gs-reveal';
            tr.dataset.id = s.studentId;

            tr.innerHTML = `
                <td class="py-4 px-6 flex items-center gap-4">
                    <img src="${imageUrl}" class="w-10 h-10 rounded-full object-cover border border-glassBorder">
                    <div>
                        <p class="text-white font-medium group-hover:text-ethereal-gold transition-colors">${s.firstName || 'Unknown'} ${s.lastName || ''}</p>
                        <p class="text-xs text-gray-500">${dept} • ${yearStr}</p>
                    </div>
                </td>
                <td class="py-4 px-6 text-sm">${dept}</td>
                <td class="py-4 px-6">
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-ethereal-green/10 text-ethereal-green border border-ethereal-green/20">
                        <span class="w-1.5 h-1.5 rounded-full bg-ethereal-green animate-pulse"></span> Active
                    </span>
                </td>
                <td class="py-4 px-6 text-right">
                    <button class="text-gray-500 hover:text-ethereal-gold transition-colors p-2"><i class="ph ph-dots-three text-xl"></i></button>
                </td>
            `;
            tbody.appendChild(tr);
        });

        // Trigger GSAP reveal on the newly added rows
        gsap.from(tbody.querySelectorAll('tr'), {
            y: 20,
            opacity: 0,
            duration: 0.4,
            stagger: 0.05,
            ease: 'power2.out'
        });
    };

    // Trigger Initial Data Load
    loadDashboardData();

    // 4. Initialize Chart.js for Revenue Snapshot
    const ctx = document.getElementById('revenueChart');
    if (ctx) {
        new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul'],
                datasets: [{
                    label: 'Fee Collection',
                    data: [420000, 580000, 350000, 600000, 620000, 800000, 750000],
                    borderColor: '#d4af37', // Ethereal Gold
                    backgroundColor: 'rgba(212, 175, 55, 0.15)', // Muted gold fill
                    borderWidth: 3,
                    tension: 0.4, // Smooth curving
                    fill: true,
                    pointBackgroundColor: '#0b0c10',
                    pointBorderColor: '#d4af37',
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: 'rgba(31, 40, 51, 0.9)',
                        titleColor: '#fff',
                        bodyColor: '#d4af37',
                        borderColor: 'rgba(255, 255, 255, 0.1)',
                        borderWidth: 1,
                        padding: 12,
                        displayColors: false,
                        callbacks: {
                            label: function (context) {
                                return '₹ ' + context.parsed.y.toLocaleString();
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        display: false, // Cleaner look without Y axis grid
                        beginAtZero: true
                    },
                    x: {
                        grid: { display: false, drawBorder: false },
                        ticks: {
                            color: '#9ca3af',
                            font: { family: "'Manrope', sans-serif", size: 12 }
                        }
                    }
                }
            }
        });
    }

    // 5. Omni-Search Modal Logic
    const omniModal = document.getElementById('omni-search');
    const omniBackdrop = document.getElementById('omni-backdrop');
    const omniContent = document.getElementById('omni-modal');
    const omniInput = document.getElementById('omni-input');
    const omniCloseBtn = document.getElementById('omni-close');
    const searchTriggerBtn = document.querySelector('header button'); // The search button in topbar

    let omniIsOpen = false;

    const openOmni = () => {
        if (omniIsOpen) return;
        omniIsOpen = true;
        omniModal.classList.remove('pointer-events-none');

        const tl = gsap.timeline();
        tl.to(omniModal, { opacity: 1, duration: 0.2 })
            .fromTo(omniContent, { y: -20, scale: 0.95 }, { y: 0, scale: 1, duration: 0.4, ease: 'back.out(1.5)' }, "<");

        setTimeout(() => omniInput.focus(), 100);
    };

    const closeOmni = () => {
        if (!omniIsOpen) return;
        omniIsOpen = false;

        const tl = gsap.timeline({ onComplete: () => omniModal.classList.add('pointer-events-none') });
        tl.to(omniContent, { y: -10, scale: 0.98, opacity: 0, duration: 0.2 })
            .to(omniModal, { opacity: 0, duration: 0.2 }, "<");

        omniInput.value = ''; // clear input
    };

    // Keyboard shortcut (Ctrl+K or Cmd+K)
    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
            e.preventDefault();
            openOmni();
        }
        if (e.key === 'Escape' && omniIsOpen) {
            closeOmni();
        }
    });

    searchTriggerBtn.addEventListener('click', openOmni);
    omniBackdrop.addEventListener('click', closeOmni);
    omniCloseBtn.addEventListener('click', closeOmni);

    // 6. Side Drawer Logic (Student Profile)
    const sideDrawerContainer = document.getElementById('side-drawer-container');
    const drawerOverlay = document.getElementById('drawer-overlay');
    const sideDrawer = document.getElementById('side-drawer');
    const closeDrawerBtn = document.getElementById('close-drawer');
    const tableRows = document.querySelectorAll('tbody tr'); // Trigger from grid

    let drawerIsOpen = false;

    const openDrawer = () => {
        if (drawerIsOpen) return;
        drawerIsOpen = true;
        sideDrawerContainer.classList.remove('pointer-events-none');

        const tl = gsap.timeline();
        tl.to(sideDrawerContainer, { opacity: 1, duration: 0.3 })
            .fromTo(sideDrawer, { x: '100%' }, { x: '0%', duration: 0.5, ease: 'power3.out' }, "<");
    };

    const closeDrawer = () => {
        if (!drawerIsOpen) return;
        drawerIsOpen = false;

        const tl = gsap.timeline({ onComplete: () => sideDrawerContainer.classList.add('pointer-events-none') });
        tl.to(sideDrawer, { x: '100%', duration: 0.4, ease: 'power3.in' })
            .to(sideDrawerContainer, { opacity: 0, duration: 0.3 }, "-=0.2");
    };

    // Attach click events using event delegation to support dynamically loaded rows from API
    const tableBody = document.querySelector('tbody');
    if (tableBody) {
        tableBody.addEventListener('click', (e) => {
            const row = e.target.closest('tr');
            if (row) {
                // Here we could extract student ID from a data attribute
                // const studentId = row.dataset.id;
                // loadProfileIntoDrawer(studentId);
                openDrawer();
            }
        });
    }

    drawerOverlay.addEventListener('click', closeDrawer);
    closeDrawerBtn.addEventListener('click', closeDrawer);

});
