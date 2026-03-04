/* ═══════════════════════════════════════════════════════════
   CMS V3 — Advanced API Connectivity Layer
   ═══════════════════════════════════════════════════════════ */

const API = {
    BASE: 'https://localhost:7000/api',
    TIMEOUT: 15000,

    async request(endpoint, opts = {}) {
        const token = localStorage.getItem('cms_token');
        const url = `${this.BASE}${endpoint.toLowerCase()}`;
        const isFormData = opts.body instanceof FormData;
        const headers = {
            ...(isFormData ? {} : { 'Content-Type': 'application/json' }),
            ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
            ...opts.headers
        };

        try {
            const ctrl = new AbortController();
            const tid = setTimeout(() => ctrl.abort(), this.TIMEOUT);

            const res = await fetch(url, { ...opts, headers, signal: ctrl.signal });
            clearTimeout(tid);

            if (res.status === 401) {
                console.warn('Unauthorized access. Handled by V3 Core.');
                localStorage.clear();
                // To be implemented: smoothly trigger GSAP login modal instead of harsh redirect
                return null;
            }
            if (res.status === 204) return null;

            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                throw new Error(err.message || `Error ${res.status}`);
            }

            const text = await res.text();
            if (!text) return null;

            const data = JSON.parse(text);
            // Extracts standard wrapper if exists, else raw data
            return data?.data !== undefined ? data.data : data;

        } catch (e) {
            console.error(`API Error [${endpoint}]:`, e);
            throw e;
        }
    },

    get(ep) { return this.request(ep); },
    post(ep, body) { return this.request(ep, { method: 'POST', body: JSON.stringify(body) }); },
    put(ep, body) { return this.request(ep, { method: 'PUT', body: JSON.stringify(body) }); },
    del(ep) { return this.request(ep, { method: 'DELETE' }); },

    /* ====================================================================
       AUTHENTICATION & CORE
       ==================================================================== */
    login(email, password) { return this.post('/auth/login', { email, password }); },
    register(data) { return this.post('/auth/register', data); },
    uploadPhoto(file) {
        const fd = new FormData();
        fd.append('photo', file);
        return this.request('/auth/upload-photo', { method: 'POST', body: fd });
    },
    getDashboardStats() { return this.get('/admin/stats'); },
    getUsers() { return this.get('/admin/users'); },
    getAllAuthUsers() { return this.get('/auth/users'); },
    changePassword(email, newPassword) { return this.put('/auth/change-password', { email, newPassword }); },
    deleteAuthUser(id) { return this.del(`/auth/users/${id}`); },

    /* ====================================================================
       STUDENTS (Includes Pagination Prep)
       ==================================================================== */
    // Fetches massive lists. Handled server side via pageSize but we bump to 1000 for the Data Grid
    getStudents() { return this.get('/student?pageSize=5000'); },
    getRecentStudents() { return this.get('/student?pageSize=10'); },
    getStudent(id) { return this.get(`/student/${id}`); },
    createStudent(d) { return this.post('/student', d); },
    updateStudent(id, d) { return this.put(`/student/${id}`, d); },
    deleteStudent(id) { return this.del(`/student/${id}`); },

    /* ====================================================================
       TEACHERS & DEPARTMENTS
       ==================================================================== */
    getTeachers() { return this.get('/teacher?pageSize=1000'); },
    getTeacher(id) { return this.get(`/teacher/${id}`); },
    getTeacherByUserId(userId) { return this.get(`/teacher/user/${userId}`); },
    createTeacher(d) { return this.post('/teacher', d); },
    updateTeacher(id, d) { return this.put(`/teacher/${id}`, d); },
    deleteTeacher(id) { return this.del(`/teacher/${id}`); },
    getDepartments() { return this.get('/department'); },

    /* ====================================================================
       ACADEMICS (Courses, Enrollments, TimeSlots)
       ==================================================================== */
    getCourses() { return this.get('/course'); },
    getCourse(id) { return this.get(`/course/${id}`); },
    createCourse(d) { return this.post('/course', d); },
    updateCourse(id, d) { return this.put(`/course/${id}`, d); },
    deleteCourse(id) { return this.del(`/course/${id}`); },

    getEnrollments() { return this.get('/enrollment'); },
    getEnrollmentsByStudent(id) { return this.get(`/enrollment/student/${id}`); },
    getEnrollmentsByCourse(id) { return this.get(`/enrollment/course/${id}`); },
    createEnrollment(d) { return this.post('/enrollment', d); },
    deleteEnrollment(id) { return this.del(`/enrollment/${id}`); },

    getTimeSlots() { return this.get('/timeslot'); },
    getTimeSlotsByTeacher(id) { return this.get(`/timeslot/teacher/${id}`); },
    getTimeSlotsByCourse(id) { return this.get(`/timeslot/course/${id}`); },
    getTimeSlotsByDay(day) { return this.get(`/timeslot/day/${day}`); },
    createTimeSlot(d) { return this.post('/timeslot', d); },

    /* ====================================================================
       EXAMS & GRADES
       ==================================================================== */
    getExams() { return this.get('/exam'); },
    getExam(id) { return this.get(`/exam/${id}`); },
    getExamQuestions(id) { return this.get(`/exam/${id}/questions`); },
    createExam(d, teacherId) { return this.post(`/exam?teacherId=${teacherId}`, d); },
    updateExam(id, d) { return this.put(`/exam/${id}`, d); },
    publishExam(id) { return this.put(`/exam/${id}/publish`, {}); },
    deleteExam(id) { return this.del(`/exam/${id}`); },

    addExamQuestion(examId, d) { return this.post(`/exam/${examId}/questions`, d); },
    deleteExamQuestion(examId, questionId) { return this.del(`/exam/${examId}/questions/${questionId}`); },
    submitExam(d, studentId) { return this.post(`/exam/submit?studentId=${studentId}`, d); },

    getExamResultsByStudent(id) { return this.get(`/exam/results/student/${id}`); },
    getExamResultsByExam(id) { return this.get(`/exam/results/exam/${id}`); },

    getGrades() { return this.get('/grade'); },
    getGradesByStudent(id) { return this.get(`/grade/student/${id}`); },
    getGradesByCourse(id) { return this.get(`/grade/course/${id}`); },
    createGrade(d) { return this.post('/grade', d); },
    updateGrade(id, d) { return this.put(`/grade/${id}`, d); },

    /* ====================================================================
       OPERATIONS (Fees & Attendance)
       ==================================================================== */
    getFees() { return this.get('/fee'); },
    getFeesByStudent(id) { return this.get(`/fee/student/${id}`); },
    updateFee(id, d) { return this.put(`/fee/${id}`, d); },

    getAttendance() { return this.get('/attendance'); },
    getAttendanceByStudent(id) { return this.get(`/attendance/student/${id}`); },
    getAttendanceByCourse(id) { return this.get(`/attendance/course/${id}`); },
    createAttendance(d) { return this.post('/attendance', d); },

    /* ====================================================================
       COMMUNICATION (Messages & Notices)
       ==================================================================== */
    getNotices() { return this.get('/notice'); },
    getActiveNotices(role) { return this.get(`/notice/active${role ? '?role=' + role : ''}`); },
    getAnnouncements() { return this.get('/announcement'); },
    createAnnouncement(d, creatorId, creatorRole) { return this.post(`/announcement?creatorId=${creatorId}&creatorRole=${creatorRole}`, d); },

    getMessages() { return this.get('/message'); },
    getMessageInbox(userId, role) { return this.get(`/message/inbox/${userId}/${role}`); },
    getMessageSent(userId, role) { return this.get(`/message/sent/${userId}/${role}`); },
    sendMessage(d, senderId, senderRole) { return this.post(`/message?senderId=${senderId}&senderRole=${senderRole}`, d); },
    markMessageRead(id) { return this.put(`/message/${id}/read`, {}); },
    getUnreadCount(userId, role) { return this.get(`/message/unread-count/${userId}/${role}`); }
};
