import api from './api';

const academicService = {
  // Exam related methods
  getExams: async (params = {}) => {
    const response = await api.get('/api/exam', { params });
    return response.data;
  },

  getExamById: async (id) => {
    const response = await api.get(`/api/exam/${id}`);
    return response.data;
  },

  createExam: async (examData) => {
    const response = await api.post('/api/exam', examData);
    return response.data;
  },

  updateExam: async (id, examData) => {
    const response = await api.put(`/api/exam/${id}`, examData);
    return response.data;
  },

  deleteExam: async (id) => {
    const response = await api.delete(`/api/exam/${id}`);
    return response.data;
  },

  // Grade related methods
  getGrades: async (params = {}) => {
    const response = await api.get('/api/grade', { params });
    return response.data;
  },

  getGradeById: async (id) => {
    const response = await api.get(`/api/grade/${id}`);
    return response.data;
  },

  createGrade: async (gradeData) => {
    const response = await api.post('/api/grade', gradeData);
    return response.data;
  },

  updateGrade: async (id, gradeData) => {
    const response = await api.put(`/api/grade/${id}`, gradeData);
    return response.data;
  },

  deleteGrade: async (id) => {
    const response = await api.delete(`/api/grade/${id}`);
    return response.data;
  },

  getStudentGrades: async (studentId) => {
    const response = await api.get(`/api/grade/student/${studentId}`);
    return response.data;
  },

  getCourseGrades: async (courseId) => {
    const response = await api.get(`/api/grade/course/${courseId}`);
    return response.data;
  },

  // Notice related methods
  getNotices: async (params = {}) => {
    const response = await api.get('/api/notice', { params });
    return response.data;
  },

  getNoticeById: async (id) => {
    const response = await api.get(`/api/notice/${id}`);
    return response.data;
  },

  createNotice: async (noticeData) => {
    const response = await api.post('/api/notice', noticeData);
    return response.data;
  },

  updateNotice: async (id, noticeData) => {
    const response = await api.put(`/api/notice/${id}`, noticeData);
    return response.data;
  },

  deleteNotice: async (id) => {
    const response = await api.delete(`/api/notice/${id}`);
    return response.data;
  },

  // TimeSlot related methods
  getTimeSlots: async (params = {}) => {
    const response = await api.get('/api/timeslot', { params });
    return response.data;
  },

  getTimeSlotById: async (id) => {
    const response = await api.get(`/api/timeslot/${id}`);
    return response.data;
  },

  createTimeSlot: async (timeSlotData) => {
    const response = await api.post('/api/timeslot', timeSlotData);
    return response.data;
  },

  updateTimeSlot: async (id, timeSlotData) => {
    const response = await api.put(`/api/timeslot/${id}`, timeSlotData);
    return response.data;
  },

  deleteTimeSlot: async (id) => {
    const response = await api.delete(`/api/timeslot/${id}`);
    return response.data;
  },

  // Message related methods
  getMessages: async (params = {}) => {
    const response = await api.get('/api/message', { params });
    return response.data;
  },

  sendMessage: async (messageData) => {
    const response = await api.post('/api/message', messageData);
    return response.data;
  },

  deleteMessage: async (id) => {
    const response = await api.delete(`/api/message/${id}`);
    return response.data;
  },
};

export default academicService;
