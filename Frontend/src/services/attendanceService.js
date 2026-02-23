import api from './api';

const attendanceService = {
  getAttendanceRecords: async (params = {}) => {
    const response = await api.get('/api/attendance', { params });
    return response.data;
  },

  getAttendanceById: async (id) => {
    const response = await api.get(`/api/attendance/${id}`);
    return response.data;
  },

  markAttendance: async (attendanceData) => {
    const response = await api.post('/api/attendance', attendanceData);
    return response.data;
  },

  updateAttendance: async (id, attendanceData) => {
    const response = await api.put(`/api/attendance/${id}`, attendanceData);
    return response.data;
  },

  deleteAttendance: async (id) => {
    const response = await api.delete(`/api/attendance/${id}`);
    return response.data;
  },

  getAttendanceReport: async (params = {}) => {
    const response = await api.get('/api/attendance/report', { params });
    return response.data;
  },

  getStudentAttendance: async (studentId, params = {}) => {
    const response = await api.get(`/api/attendance/student/${studentId}`, { params });
    return response.data;
  },

  getCourseAttendance: async (courseId, params = {}) => {
    const response = await api.get(`/api/attendance/course/${courseId}`, { params });
    return response.data;
  },

  markBulkAttendance: async (attendanceData) => {
    const response = await api.post('/api/attendance/bulk', attendanceData);
    return response.data;
  },

  getAttendanceSummary: async (params = {}) => {
    const response = await api.get('/api/attendance/summary', { params });
    return response.data;
  },
};

export default attendanceService;
