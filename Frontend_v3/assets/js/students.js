/* CMS V3 | Students Deep Matrix Controller */

document.addEventListener('DOMContentLoaded', () => {

    // 1. Init UI Animations
    gsap.set('.gs-reveal', { visibility: 'visible' });
    gsap.from('.gs-reveal', {
        y: 40, opacity: 0, duration: 0.8, stagger: 0.1, ease: 'power3.out', delay: 0.2
    });

    const studentGridBody = document.getElementById('studentGridBody');
    const searchInput = document.getElementById('studentSearch');
    const recordCountText = document.getElementById('recordCountText');

    // Drawer Variables
    const sideDrawerContainer = document.getElementById('side-drawer-container');
    const drawerOverlay = document.getElementById('drawer-overlay');
    const sideDrawer = document.getElementById('side-drawer');
    const drawerContentInject = document.getElementById('drawer-content-inject');
    let drawerIsOpen = false;
    let allStudents = [];

    // 2. Data Fetching
    const loadStudentData = async () => {
        try {
            // Pulling massive dataset (pageSize=5000 in API.js)
            const students = await API.getStudents();
            allStudents = Array.isArray(students) ? students : [];
            buildFilterChips(allStudents);
            buildYearDropDown(allStudents);
            renderGrid(allStudents);
        } catch (error) {
            console.error('Failed to load students', error);
            studentGridBody.innerHTML = `<tr><td colspan="6" class="text-center py-10 text-ethereal-red">Failed to connect to database.</td></tr>`;
        }
    };

    // 3. Render Engine
    const renderGrid = (data) => {
        studentGridBody.innerHTML = '';

        if (data.length === 0) {
            studentGridBody.innerHTML = `<tr><td colspan="6" class="text-center py-20 text-gray-500">
                <i class="ph ph-empty text-6xl text-obsidian-600 mb-4 block"></i>
                No records found.
            </td></tr>`;
            recordCountText.innerText = 'Showing 0 records';
            return;
        }

        const fragment = document.createDocumentFragment();

        data.forEach((s, i) => {
            const getAvatarSeed = (gender) => {
                if (gender === 'Male') return `men/${(i % 50) + 1}.jpg`;
                if (gender === 'Female') return `women/${(i % 50) + 1}.jpg`;
                return `lego/${(i % 9) + 1}.jpg`;
            };
            const avatar = s.profilePictureUrl || `https://randomuser.me/api/portraits/${getAvatarSeed(s.gender)}`;
            const name = `${s.firstName || 'Unknown'} ${s.lastName || ''}`;
            const dept = s.department?.departmentName || 'General';
            const yearStr = s.createdAt ? new Date(s.createdAt).toLocaleDateString() : 'N/A';
            const status = s.status || 'Active';
            const email = s.email || 'N/A';
            const phone = s.phone || '';

            const tr = document.createElement('tr');
            tr.className = 'border-b border-glassBorder/50 hover:bg-obsidian-700/30 transition-colors group cursor-pointer';

            // Age Calculator
            let ageStr = '';
            if (s.dateOfBirth) {
                const ageDifMs = Date.now() - new Date(s.dateOfBirth).getTime();
                const ageDate = new Date(ageDifMs);
                ageStr = `${Math.abs(ageDate.getUTCFullYear() - 1970)}y`;
            }

            // Progress Bar Logic (4 year Degree assumed)
            let progressPercent = 0;
            if (s.admissionYear) {
                const currentYear = new Date().getFullYear();
                const yearsActive = Math.max(0, currentYear - s.admissionYear);
                progressPercent = Math.min(100, Math.round((yearsActive / 4) * 100)); // Capped at 100%
            }

            // We store stringified subset data to avoid redundant API hits when opening drawer
            tr.dataset.profile = JSON.stringify({
                id: s.rollNumber || s.studentId, name, email: email, phone: phone,
                dept, avatar, date: yearStr, status,
                address: s.address || 'Unknown Address',
                gender: s.gender || 'Not Specified',
                age: ageStr,
                dbId: s.studentId,
                progressPercent: progressPercent
            });

            tr.innerHTML = `
                <td class="py-4 px-6 w-10 relative z-10" onclick="event.stopPropagation()">
                    <input type="checkbox" class="glass-checkbox">
                </td>
                <td class="py-4 px-6 flex items-center gap-4 relative z-0">
                    <img src="${avatar}" class="w-10 h-10 rounded-full border border-glassBorder object-cover">
                    <div>
                        <p class="text-white font-medium group-hover:text-ethereal-gold transition-colors">${name}</p>
                        <p class="text-xs text-gray-500">${email}</p>
                    </div>
                </td>
                <td class="py-4 px-6 text-sm text-gray-400 font-mono has-tooltip">
                    ${(s.rollNumber || s.studentId?.toString()).substring(0, 9).toUpperCase()}
                    <span class="tooltip">${dept} • ID: ${s.studentId}</span>
                </td>
                <td class="py-4 px-6 text-sm">${dept}</td>
                <td class="py-4 px-6 text-sm text-gray-400">${yearStr}</td>
                <td class="py-4 px-6 text-center">
                    <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium ${status === 'Active' ? 'bg-ethereal-green/10 text-ethereal-green border-ethereal-green/20' : 'bg-ethereal-red/10 text-ethereal-red border-ethereal-red/20'} border">
                        <span class="w-1.5 h-1.5 rounded-full ${status === 'Active' ? 'bg-ethereal-green animate-pulse' : 'bg-ethereal-red'}"></span> ${status}
                    </span>
                </td>
                <td class="py-4 px-6 text-right">
                    <button class="text-gray-500 hover:text-ethereal-gold transition-colors p-2" title="View Options">
                        <i class="ph ph-dots-three-outline-vertical text-lg"></i>
                    </button>
                </td>
            `;
            fragment.appendChild(tr);
        });

        studentGridBody.appendChild(fragment);
        recordCountText.innerText = `Showing ${data.length} records`;

        // GSAP animate new rows IN
        gsap.from(studentGridBody.querySelectorAll('tr'), {
            y: 10, opacity: 0, duration: 0.3, stagger: 0.02, ease: 'power2.out'
        });
    };

    // 4. Live Search & Department Filter Logic
    let activeDepartment = 'All';
    let searchQuery = '';
    let activeYear = 'All';
    const yearFilterSelect = document.getElementById('yearFilter');

    const applyFilters = () => {
        let filtered = allStudents;

        if (activeDepartment !== 'All') {
            filtered = filtered.filter(s => (s.department?.departmentName || 'General') === activeDepartment);
        }

        if (activeYear !== 'All') {
            filtered = filtered.filter(s => s.admissionYear && s.admissionYear.toString() === activeYear);
        }

        if (searchQuery) {
            filtered = filtered.filter(s => {
                const name = `${s.firstName || ''} ${s.lastName || ''}`.toLowerCase();
                const email = (s.email || '').toLowerCase();
                const rollNumber = (s.rollNumber || '').toLowerCase();
                const address = (s.address || '').toLowerCase(); // Added Address Search Payload
                return name.includes(searchQuery) || email.includes(searchQuery) || rollNumber.includes(searchQuery) || address.includes(searchQuery);
            });
        }
        renderGrid(filtered);
    };

    searchInput.addEventListener('input', (e) => {
        searchQuery = e.target.value.toLowerCase().trim();
        applyFilters();
    });

    if (yearFilterSelect) {
        yearFilterSelect.addEventListener('change', (e) => {
            activeYear = e.target.value;
            applyFilters();
        });
    }

    const buildYearDropDown = (data) => {
        if (!yearFilterSelect) return;

        const years = new Set();
        data.forEach(s => {
            if (s.admissionYear) years.add(s.admissionYear.toString());
        });

        const sortedYears = Array.from(years).sort((a, b) => b - a); // Descending

        // reset
        yearFilterSelect.innerHTML = '<option value="All">All Years</option>';

        sortedYears.forEach(year => {
            const opt = document.createElement('option');
            opt.value = year;
            opt.innerText = `Class of ${year}`;
            yearFilterSelect.appendChild(opt);
        });
    };

    const buildFilterChips = (data) => {
        const chipsContainer = document.getElementById('filterChips');
        if (!chipsContainer) return;

        // Extract unique department names mathematically
        const depts = new Set(['All']);
        data.forEach(s => depts.add(s.department?.departmentName || 'General'));

        chipsContainer.innerHTML = '';
        depts.forEach(dept => {
            const btn = document.createElement('button');
            const isActive = dept === activeDepartment;

            btn.className = `px-3 py-1.5 rounded-full text-xs font-semibold border transition-all duration-300 ${isActive
                ? 'bg-ethereal-gold text-obsidian-900 border-ethereal-gold shadow-[0_0_10px_rgba(212,175,55,0.4)]'
                : 'bg-obsidian-700/50 text-gray-400 border-glassBorder hover:border-ethereal-gold/30 hover:text-white'
                }`;
            btn.innerText = dept;

            btn.addEventListener('click', () => {
                activeDepartment = dept;
                buildFilterChips(data); // Re-render to update active styling
                applyFilters();
            });

            chipsContainer.appendChild(btn);
        });

        // Small GSAP entry
        gsap.from(chipsContainer.children, { x: -10, opacity: 0, duration: 0.3, stagger: 0.05, ease: 'power2.out' });
        chipsContainer.classList.remove('hidden');
    };

    // 5. Drawer Profile Logic
    const openDrawer = (profileData) => {
        if (drawerIsOpen) return;

        // Inject beautiful dynamic HTML based on clicked row
        // Inject beautiful dynamic HTML based on clicked row
        drawerContentInject.innerHTML = `
            <div class="relative h-48 bg-gradient-to-br from-obsidian-600 to-obsidian-800 overflow-hidden" id="pdfCaptureArea">
                <div class="absolute inset-0 bg-ethereal-gold/10 mix-blend-overlay"></div>
                <!-- Glassmorphism ID Card (Proposal #1) -->
                <div class="absolute top-4 left-4 right-16 h-40 bg-obsidian-900/60 backdrop-blur-xl border border-glassBorder rounded-xl p-4 flex gap-4 tilt-wrapper shadow-2xl">
                    <div class="tilt-element w-full h-full flex gap-4 items-center">
                        <img src="${profileData.avatar}" class="w-24 h-24 rounded-lg object-cover border-2 border-ethereal-gold/50 shadow-lg">
                        <div class="flex-1 overflow-hidden">
                            <h3 class="text-lg font-bold text-white leading-tight truncate">${profileData.name}</h3>
                            <p class="text-xs text-ethereal-gold mb-2">${profileData.id}</p>
                            <a href="#" class="text-[10px] text-gray-400 max-w-[150px] truncate hover:text-white transition-colors block"><i class="ph ph-map-pin"></i> ${profileData.address}</a>
                            <div class="mt-2 text-[10px] font-mono bg-ethereal-gold/20 text-ethereal-gold px-2 py-1 rounded w-max border border-ethereal-gold/30">
                                ${profileData.dept}
                            </div>
                        </div>
                    </div>
                </div>

                <button id="close-drawer" class="absolute top-4 right-4 w-8 h-8 rounded-full bg-black/40 hover:bg-black/80 flex items-center justify-center text-white backdrop-blur-md transition-colors z-10">
                    <i class="ph ph-x"></i>
                </button>
                <div class="absolute top-14 right-4 flex flex-col gap-2 z-10">
                    <!-- Email One-Click (Proposal #17) -->
                    <a href="mailto:${profileData.email}" title="Email Student" class="w-8 h-8 rounded-full bg-black/40 hover:bg-ethereal-green hover:text-obsidian-900 flex items-center justify-center text-white backdrop-blur-md transition-colors"><i class="ph ph-envelope"></i></a>
                </div>
            </div>

            <div class="flex-1 overflow-y-auto pt-6 px-8 pb-8 scrollbar-hide">
                <div class="flex border-b border-glassBorder mb-6 gap-6">
                    <button class="pb-2 text-sm font-medium text-ethereal-gold border-b-2 border-ethereal-gold">Overview</button>
                    <button class="pb-2 text-sm font-medium text-gray-500 hover:text-white transition-colors">Academics</button>
                    <button class="pb-2 text-sm font-medium text-gray-500 hover:text-white transition-colors">Timeline</button>
                </div>

                <div class="space-y-4">
                    <div class="flex flex-col gap-1 py-3 border-b border-glassBorder/30 group">
                        <span class="text-xs text-gray-500 uppercase tracking-wider font-semibold">Contact Portal</span>
                        <div class="flex justify-between items-center mt-1">
                            <span class="text-sm text-white">${profileData.email}</span>
                            <button class="opacity-0 group-hover:opacity-100 transition-opacity text-ethereal-gold"><i class="ph ph-copy"></i></button>
                        </div>
                    </div>
                    
                    <div class="flex flex-col gap-1 py-3 border-b border-glassBorder/30">
                        <span class="text-xs text-gray-500 uppercase tracking-wider font-semibold">Core Demographics</span>
                        <div class="flex justify-between mt-1 items-center">
                            <!-- Telephone One-Click (Proposal #17) -->
                            <a href="tel:${profileData.phone}" class="text-sm text-ethereal-gold hover:underline flex items-center"><i class="ph ph-phone mr-1"></i> ${profileData.phone || '(No Phone Found)'}</a>
                            <span class="text-sm text-white bg-obsidian-700 px-2 py-1 rounded border border-glassBorder shadow-inner">${profileData.age ? `${profileData.age} | ` : ''}${profileData.gender}</span>
                        </div>
                    </div>
                    
                    <div class="flex flex-col gap-2 py-3 border-b border-glassBorder/30">
                        <div class="flex justify-between w-full">
                            <span class="text-xs text-gray-500 uppercase tracking-wider font-semibold">Degree Trajectory</span>
                            <span class="text-xs text-ethereal-gold">${profileData.progressPercent}%</span>
                        </div>
                        <div class="w-full bg-obsidian-900 rounded-full h-2 border border-glassBorder overflow-hidden">
                            <div class="bg-ethereal-gold h-2 rounded-full relative" style="width: ${profileData.progressPercent}%">
                                <div class="absolute inset-0 bg-white/30 mix-blend-overlay w-full h-full animate-[pulse_2s_ease-in-out_infinite]"></div>
                            </div>
                        </div>
                    </div>

                    <!-- Audit Timeline View (Proposal #20) -->
                    <div class="pt-4">
                        <span class="text-xs text-gray-500 uppercase tracking-wider font-semibold block mb-4">Security & Audit Log</span>
                        <div class="border-l-2 border-glassBorder ml-2 pl-4 space-y-4 relative">
                            <div class="relative">
                                <div class="absolute -left-[23px] top-1 w-3 h-3 rounded-full bg-ethereal-gold border-2 border-obsidian-800 shadow-[0_0_8px_rgba(99,102,241,0.5)]"></div>
                                <p class="text-sm text-white">Profile Synchronized</p>
                                <p class="text-xs text-gray-500">System API Engine • Just now</p>
                            </div>
                            <div class="relative">
                                <div class="absolute -left-[23px] top-1 w-3 h-3 rounded-full bg-obsidian-700 border-2 border-glassBorder"></div>
                                <p class="text-sm text-gray-400">Initial Enrollment</p>
                                <p class="text-xs text-gray-500">Administration • Enrolled: ${profileData.date}</p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="p-6 border-t border-glassBorder bg-obsidian-800/80 flex gap-4">
                <button id="exportPdfBtn" class="w-full py-3 rounded-lg bg-obsidian-700 hover:bg-obsidian-600 text-white font-bold transition-all flex items-center justify-center border border-glassBorder group">
                    <i class="ph ph-file-pdf mr-2 text-lg group-hover:text-ethereal-red transition-colors"></i> Export DOSSIER.PDF
                </button>
            </div>
        `;

        drawerIsOpen = true;
        sideDrawerContainer.classList.remove('pointer-events-none');

        const tl = gsap.timeline();
        tl.to(sideDrawerContainer, { opacity: 1, duration: 0.3 })
            .fromTo(sideDrawer, { x: '100%' }, { x: '0%', duration: 0.5, ease: 'power3.out' }, "<");

        // Reattach close event to the newly injected close button container it was destroyed
        document.getElementById('close-drawer').addEventListener('click', closeDrawer);
    };

    const closeDrawer = () => {
        if (!drawerIsOpen) return;
        drawerIsOpen = false;

        const tl = gsap.timeline({ onComplete: () => sideDrawerContainer.classList.add('pointer-events-none') });
        tl.to(sideDrawer, { x: '100%', duration: 0.4, ease: 'power3.in' })
            .to(sideDrawerContainer, { opacity: 0, duration: 0.3 }, "-=0.2");
    };

    // Delegated click for table rows
    studentGridBody.addEventListener('click', (e) => {
        const row = e.target.closest('tr');
        if (row && row.dataset.profile) {
            const profileData = JSON.parse(row.dataset.profile);
            openDrawer(profileData);
        }
    });

    drawerOverlay.addEventListener('click', closeDrawer);

    // 6. Bulk Select & Actions Bar (Proposals 9)
    const bulkActionsBar = document.getElementById('bulkActionsBar');
    const bulkCount = document.getElementById('bulkCount');
    let selectedChecked = 0;

    studentGridBody.addEventListener('change', (e) => {
        if (e.target.classList.contains('glass-checkbox')) {
            const row = e.target.closest('tr');
            if (e.target.checked) {
                selectedChecked++;
                row.classList.add('bg-ethereal-gold/10');
            } else {
                selectedChecked--;
                row.classList.remove('bg-ethereal-gold/10');
            }

            if (selectedChecked > 0) {
                bulkCount.innerText = `${selectedChecked} Selected`;
                bulkActionsBar.classList.remove('opacity-0', 'pointer-events-none', 'translate-y-2');
            } else {
                bulkActionsBar.classList.add('opacity-0', 'pointer-events-none', 'translate-y-2');
            }
        }
    });

    document.getElementById('cancelBulkBtn')?.addEventListener('click', () => {
        document.querySelectorAll('.glass-checkbox').forEach(cb => cb.checked = false);
        document.querySelectorAll('#studentGridBody tr').forEach(tr => tr.classList.remove('bg-ethereal-gold/10'));
        selectedChecked = 0;
        bulkActionsBar.classList.add('opacity-0', 'pointer-events-none', 'translate-y-2');
    });

    // 7. Context Menu Action (Proposal 19)
    const contextMenu = document.getElementById('contextMenu');
    studentGridBody.addEventListener('contextmenu', (e) => {
        const row = e.target.closest('tr');
        if (!row) return;

        e.preventDefault();
        const { clientX: mouseX, clientY: mouseY } = e;

        contextMenu.style.top = `${mouseY}px`;
        contextMenu.style.left = `${mouseX}px`;
        contextMenu.classList.remove('opacity-0', 'pointer-events-none', 'scale-95');
    });

    document.addEventListener('click', (e) => {
        if (!contextMenu.contains(e.target)) {
            contextMenu.classList.add('opacity-0', 'pointer-events-none', 'scale-95');
        }

        // Handle PDF Export click (Delegated since it's injected)
        const exportBtn = e.target.closest('#exportPdfBtn');
        if (exportBtn) {
            const element = document.getElementById('pdfCaptureArea');
            const studentName = document.querySelector('#pdfCaptureArea h3')?.innerText || 'Student';

            const opt = {
                margin: 1,
                filename: `${studentName.replace(/ /g, '_')}_Dossier.pdf`,
                image: { type: 'jpeg', quality: 0.98 },
                html2canvas: { scale: 2, useCORS: true },
                jsPDF: { unit: 'in', format: 'letter', orientation: 'landscape' }
            };

            // Switch text black temporarily for white PDF background, or just export the dark mode directly. 
            // html2pdf usually exports what it sees.
            html2pdf().set(opt).from(element).save();
        }
    });

    // 8. 3D Enroll Modal (Proposal 16)
    const addStudentBtn = document.getElementById('addStudentBtn');
    const enrollModal = document.getElementById('enrollModal');
    const closeEnroll = document.getElementById('closeEnroll');
    const enrollOverlay = document.getElementById('enrollOverlay');

    const toggleEnroll = (show) => {
        if (show) {
            enrollModal.classList.remove('opacity-0', 'pointer-events-none');
            gsap.to('#enrollCard', { y: 0, opacity: 1, duration: 0.5, ease: "back.out(1.5)" });
        } else {
            gsap.to('#enrollCard', { y: 30, opacity: 0, duration: 0.3, ease: "power2.in" });
            setTimeout(() => enrollModal.classList.add('opacity-0', 'pointer-events-none'), 300);
        }
    };

    addStudentBtn?.addEventListener('click', () => toggleEnroll(true));
    closeEnroll?.addEventListener('click', () => toggleEnroll(false));
    enrollOverlay?.addEventListener('click', () => toggleEnroll(false));

    // Blastoff
    loadStudentData();
});
