/* ═══════════════════════════════════════════════════════════
   NeoVerse CMS — API Service Layer
   ═══════════════════════════════════════════════════════════ */

const API = {
    BASE: 'https://collegemanagementsystem-2gp3.onrender.com/api',
    TIMEOUT: 30000,

    async request(endpoint, opts = {}, retries = 3) {
        const token = localStorage.getItem('cms_token');
        const url = `${this.BASE}${endpoint.toLowerCase()}`;
        const isFormData = opts.body instanceof FormData;
        const headers = {
            ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
            ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
            ...opts.headers
        };

        for (let attempt = 1; attempt <= retries; attempt++) {
            try {
                const ctrl = new AbortController();
                const tid = setTimeout(() => ctrl.abort(), this.TIMEOUT);
                const res = await fetch(url, { ...opts, headers, signal: ctrl.signal });
                clearTimeout(tid);

                if (res.status === 401) {
                    localStorage.clear();
                    location.href = (location.pathname.includes('/pages/') ? '' : 'pages/') + 'login.html';
                    return null;
                }

                // If temporary server error (Thundering Herd / Gateway), throw error to trigger retry
                if (res.status === 500 || res.status === 502 || res.status === 504) {
                    throw new Error(`Server Error ${res.status}`);
                }

                if (res.status === 204) return null;
                if (!res.ok) {
                    const err = await res.json().catch(() => ({}));
                    // Don't retry client errors (400, 403, 404, etc.)
                    throw Object.assign(new Error(err.message || `Error ${res.status}`), { isClientError: res.status >= 400 && res.status < 500 });
                }
                const text = await res.text();
                if (!text) return null;
                const data = JSON.parse(text);
                return data?.data !== undefined ? data.data : data;
            } catch (e) {
                if (e.isClientError || attempt === retries) {
                    if (e.name === 'AbortError') throw new Error('Request timed out');
                    throw e;
                }
                // Exponential backoff wait (1.5s, 3s, ...) before retry
                console.warn(`API: ${endpoint} failed (Attempt ${attempt}/${retries}). Retrying...`);
                await new Promise(resolve => setTimeout(resolve, 1500 * attempt));
            }
        }
    },

    get(ep) { return this.request(ep); },
    post(ep, body) { return this.request(ep, { method: 'POST', body: JSON.stringify(body) }); },
    put(ep, body) { return this.request(ep, { method: 'PUT', body: JSON.stringify(body) }); },
    del(ep) { return this.request(ep, { method: 'DELETE' }); },

    // Auth
    login(email, password) { return this.post('/auth/login', { email, password }); },
    register(data) { return this.post('/auth/register', data); },

    // Photo upload (multipart form data)
    uploadPhoto(file) {
        const fd = new FormData();
        fd.append('photo', file);
        return this.request('/auth/upload-photo', { method: 'POST', body: fd });
    },

    // Students  
    getStudents() { return this.get('/student?pageSize=1000'); },
    getStudent(id) { return this.get(`/student/${id}`); },
    createStudent(d) { return this.post('/student', d); },
    updateStudent(id, d) { return this.put(`/student/${id}`, d); },
    deleteStudent(id) { return this.del(`/student/${id}`); },

    // Courses
    getCourses() { return this.get('/course'); },
    getCourse(id) { return this.get(`/course/${id}`); },
    createCourse(d) { return this.post('/course', d); },
    updateCourse(id, d) { return this.put(`/course/${id}`, d); },
    deleteCourse(id) { return this.del(`/course/${id}`); },

    // Departments
    getDepartments() { return this.get('/department'); },

    // Teachers
    getTeachers() { return this.get('/teacher'); },
    getTeacher(id) { return this.get(`/teacher/${id}`); },
    createTeacher(d) { return this.post('/teacher', d); },
    updateTeacher(id, d) { return this.put(`/teacher/${id}`, d); },
    deleteTeacher(id) { return this.del(`/teacher/${id}`); },

    // Exams
    getExams() { return this.get('/exam'); },
    getExam(id) { return this.get(`/exam/${id}`); },
    createExamSimple(d) { return this.post('/exam', d); },
    updateExam(id, d) { return this.put(`/exam/${id}`, d); },
    deleteExam(id) { return this.del(`/exam/${id}`); },

    // Messages
    getMessages() { return this.get('/message'); },
    createMessage(d) { return this.post('/message', d); },
    getAnnouncements() { return this.get('/announcement'); },

    // Enrollments
    getEnrollments() { return this.get('/enrollment'); },
    createEnrollment(d) { return this.post('/enrollment', d); },
    deleteEnrollment(id) { return this.del(`/enrollment/${id}`); },

    // Fees
    getFees() { return this.get('/fee'); },
    updateFee(id, d) { return this.put(`/fee/${id}`, d); },
    getFeesByStudent(id) { return this.get(`/fee/student/${id}`); },

    // Attendance
    getAttendance() { return this.get('/attendance'); },
    createAttendance(d) { return this.post('/attendance', d); },
    getAttendanceByStudent(id) { return this.get(`/attendance/student/${id}`); },
    getAttendanceByCourse(id) { return this.get(`/attendance/course/${id}`); },

    // Admin
    getUsers() { return this.get('/admin/users'); },
    getStats() { return this.get('/admin/stats'); },

    // Auth User Management (Admin)
    getAllAuthUsers() { return this.get('/auth/users'); },
    changePassword(email, newPassword) { return this.put('/auth/change-password', { email, newPassword }); },
    deleteAuthUser(id) { return this.del(`/auth/users/${id}`); },

    // Grades
    getGrades() { return this.get('/grade'); },
    getGradesByStudent(id) { return this.get(`/grade/student/${id}`); },
    getGradesByCourse(id) { return this.get(`/grade/course/${id}`); },
    createGrade(d) { return this.post('/grade', d); },
    updateGrade(id, d) { return this.put(`/grade/${id}`, d); },

    // Timetable
    getTimeSlots() { return this.get('/timeslot'); },
    getTimeSlotsByTeacher(id) { return this.get(`/timeslot/teacher/${id}`); },
    getTimeSlotsByCourse(id) { return this.get(`/timeslot/course/${id}`); },
    getTimeSlotsByDay(day) { return this.get(`/timeslot/day/${day}`); },
    createTimeSlot(d) { return this.post('/timeslot', d); },

    // Notices & Announcements
    getNotices() { return this.get('/notice'); },
    getActiveNotices(role) { return this.get(`/notice/active${role ? '?role=' + role : ''}`); },
    createAnnouncement(d, creatorId, creatorRole) { return this.post(`/announcement?creatorId=${creatorId}&creatorRole=${creatorRole}`, d); },

    // Exams (extended)
    getExamQuestions(id) { return this.get(`/exam/${id}/questions`); },
    createExam(d, teacherId) { return this.post(`/exam?teacherId=${teacherId}`, d); },
    addExamQuestion(examId, d) { return this.post(`/exam/${examId}/questions`, d); },
    publishExam(id) { return this.put(`/exam/${id}/publish`, {}); },
    submitExam(d, studentId) { return this.post(`/exam/submit?studentId=${studentId}`, d); },
    getExamResultsByStudent(id) { return this.get(`/exam/results/student/${id}`); },
    getExamResultsByExam(id) { return this.get(`/exam/results/exam/${id}`); },
    deleteExamQuestion(examId, questionId) { return this.del(`/exam/${examId}/questions/${questionId}`); },

    // Messages (extended)
    getMessageInbox(userId, role) { return this.get(`/message/inbox/${userId}/${role}`); },
    getMessageSent(userId, role) { return this.get(`/message/sent/${userId}/${role}`); },
    sendMessage(d, senderId, senderRole) { return this.post(`/message?senderId=${senderId}&senderRole=${senderRole}`, d); },
    markMessageRead(id) { return this.put(`/message/${id}/read`, {}); },
    getUnreadCount(userId, role) { return this.get(`/message/unread-count/${userId}/${role}`); },

    // Teacher by User ID
    getTeacherByUserId(userId) { return this.get(`/teacher/user/${userId}`); },

    // Enrollment (extended)
    getEnrollmentsByStudent(id) { return this.get(`/enrollment/student/${id}`); },
    getEnrollmentsByCourse(id) { return this.get(`/enrollment/course/${id}`); },
};

/* ═══════════════════════════════════════════════════════════
   DataStore — In-memory cache for multi-API pages
   ═══════════════════════════════════════════════════════════ */
const DataStore = {
    _ttl: 60000, // 60 seconds default TTL

    _getCacheKey(key) { return `cms_ds_${key}`; },

    async load(key, fetchFn) {
        const cacheKey = this._getCacheKey(key);
        try {
            const cachedStr = sessionStorage.getItem(cacheKey);
            if (cachedStr) {
                const cached = JSON.parse(cachedStr);
                if (Date.now() - cached.ts < this._ttl) return cached.data;
            }
        } catch (e) { console.warn('DataStore: Cache read error', e); }

        try {
            const raw = await fetchFn();
            const data = Array.isArray(raw) ? raw : (raw?.students || raw?.data || (raw ? [raw] : []));
            sessionStorage.setItem(cacheKey, JSON.stringify({ data, ts: Date.now() }));
            return data;
        } catch (e) {
            console.warn(`DataStore: Failed to load "${key}":`, e.message);
            try {
                const cachedStr = sessionStorage.getItem(cacheKey);
                if (cachedStr) return JSON.parse(cachedStr).data;
            } catch { }
            return [];
        }
    },

    get(key) {
        try {
            const cachedStr = sessionStorage.getItem(this._getCacheKey(key));
            if (cachedStr) return JSON.parse(cachedStr).data || [];
        } catch { }
        return [];
    },

    clear(key) {
        if (key) {
            sessionStorage.removeItem(this._getCacheKey(key));
        } else {
            // Only clear our specific cache keys, leave other sessionStorage data intact
            Object.keys(sessionStorage).forEach(k => {
                if (k.startsWith('cms_ds_')) sessionStorage.removeItem(k);
            });
        }
    },

    // Sequential/Batched fetch to prevent Thundering Herd (Supabase Connection Exhaustion)
    async fetchAll(keys) {
        const map = {
            students: () => API.getStudents(),
            courses: () => API.getCourses(),
            departments: () => API.getDepartments(),
            teachers: () => API.getTeachers(),
            enrollments: () => API.getEnrollments(),
            fees: () => API.getFees(),
            attendance: () => API.getAttendance(),
            grades: () => API.getGrades(),
            exams: () => API.getExams(),
            timeslots: () => API.getTimeSlots(),
            notices: () => API.getActiveNotices(),
            announcements: () => API.getAnnouncements(),
            authUsers: () => API.getAllAuthUsers(),
        };
        const toFetch = (keys || Object.keys(map)).filter(k => map[k]);
        const out = {};

        // Chunk requests into batches of 3 to avoid overwhelming the database
        const chunkSize = 3;
        for (let i = 0; i < toFetch.length; i += chunkSize) {
            const chunk = toFetch.slice(i, i + chunkSize);
            await Promise.allSettled(chunk.map(async (k) => {
                try {
                    const data = await this.load(k, map[k]);
                    out[k] = data;
                    // Dispatch event for progressive UI rendering
                    document.dispatchEvent(new CustomEvent('datastore:loaded', { detail: { key: k, data: data } }));
                } catch (e) {
                    out[k] = [];
                }
            }));
        }

        return out;
    }
};

/* ═══════════════════════════════════════════════════════════
   DataResolver — Cross-reference IDs to names/objects
   ═══════════════════════════════════════════════════════════ */
const Resolve = {
    student(id, students) {
        const s = (students || DataStore.get('students')).find(x => (x.studentId || x.id) == id);
        return s ? { ...s, fullName: `${s.firstName || ''} ${s.lastName || ''}`.trim() } : null;
    },
    course(id, courses) {
        return (courses || DataStore.get('courses')).find(x => (x.courseId || x.id) == id) || null;
    },
    teacher(id, teachers) {
        return (teachers || DataStore.get('teachers')).find(x => (x.teacherId || x.id) == id) || null;
    },
    department(id, departments) {
        return (departments || DataStore.get('departments')).find(x => (x.departmentId || x.id) == id) || null;
    },
    deptName(id, departments) {
        const d = this.department(id, departments);
        return d ? (d.departmentName || d.name || '—') : '—';
    },
    studentName(id, students) {
        const s = this.student(id, students);
        return s ? s.fullName : `Student #${id}`;
    },
    courseName(id, courses) {
        const c = this.course(id, courses);
        return c ? (c.courseName || c.name || c.courseCode || '—') : '—';
    },
    teacherName(id, teachers) {
        const t = this.teacher(id, teachers);
        if (!t) return '—';
        if (t.firstName) return `${t.firstName || ''} ${t.lastName || ''}`.trim();
        if (t.user) return `${t.user.firstName || ''} ${t.user.lastName || ''}`.trim() || t.department;
        return t.specialization || t.department || `Teacher #${id}`;
    },
    // Aggregate helpers
    enrollmentsForStudent(sid, enrollments) {
        return (enrollments || DataStore.get('enrollments')).filter(e => e.studentId == sid);
    },
    enrollmentsForCourse(cid, enrollments) {
        return (enrollments || DataStore.get('enrollments')).filter(e => e.courseId == cid);
    },
    feesForStudent(sid, fees) {
        return (fees || DataStore.get('fees')).filter(f => f.studentId == sid);
    },
    attendanceForStudent(sid, attendance) {
        return (attendance || DataStore.get('attendance')).filter(a => a.studentId == sid);
    },
    attendanceForCourse(cid, attendance) {
        return (attendance || DataStore.get('attendance')).filter(a => a.courseId == cid);
    },
    gradesForStudent(sid, grades) {
        return (grades || DataStore.get('grades')).filter(g => g.studentId == sid);
    },
    gradesForCourse(cid, grades) {
        return (grades || DataStore.get('grades')).filter(g => g.courseId == cid);
    },
    timeslotsForCourse(cid, ts) {
        return (ts || DataStore.get('timeslots')).filter(t => t.courseId == cid);
    },
    timeslotsForTeacher(tid, ts) {
        return (ts || DataStore.get('timeslots')).filter(t => t.teacherId == tid);
    },
    attendancePercent(records) {
        if (!records || !records.length) return null;
        const present = records.filter(a => a.isPresent).length;
        return Math.round((present / records.length) * 100);
    },
    cgpa(grades) {
        if (!grades || !grades.length) return null;
        const gradePoints = { 'A+': 10, 'A': 9, 'B+': 8, 'B': 7, 'C+': 6, 'C': 5, 'D': 4, 'F': 0 };
        let total = 0, count = 0;
        grades.forEach(g => {
            const pt = gradePoints[g.gradeLetter] ?? (g.marks >= 90 ? 10 : g.marks >= 80 ? 9 : g.marks >= 70 ? 8 : g.marks >= 60 ? 7 : g.marks >= 50 ? 6 : g.marks >= 40 ? 5 : 0);
            total += pt; count++;
        });
        return count ? (total / count).toFixed(1) : null;
    },
    pendingFees(fees) {
        return (fees || []).filter(f => f.status === 'Pending' || f.status === 'Overdue')
            .reduce((s, f) => s + (f.amount || 0), 0);
    },
    teacherForCourse(cid, timeslots, teachers) {
        const slot = (timeslots || DataStore.get('timeslots')).find(t => t.courseId == cid && t.teacherId);
        return slot ? this.teacher(slot.teacherId, teachers) : null;
    }
};
