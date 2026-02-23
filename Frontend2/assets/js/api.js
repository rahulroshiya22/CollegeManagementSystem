/* ═══════════════════════════════════════════════════════════
   NeoVerse CMS — API Service Layer
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
                localStorage.clear();
                location.href = (location.pathname.includes('/pages/') ? '' : 'pages/') + 'login.html';
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
            return data?.data !== undefined ? data.data : data;
        } catch (e) {
            if (e.name === 'AbortError') throw new Error('Request timed out');
            throw e;
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
    createExam(d) { return this.post('/exam', d); },
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
