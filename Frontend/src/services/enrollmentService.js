import api from './api';

const enrollmentService = {
  getEnrollments: async (params = {}) => {
    const response = await api.get('/api/enrollment', { params });
    return response.data;
  },

  getEnrollmentById: async (id) => {
    const response = await api.get(`/api/enrollment/${id}`);
    return response.data;
  },

  createEnrollment: async (enrollmentData) => {
    const response = await api.post('/api/enrollment', enrollmentData);
    return response.data;
  },

  updateEnrollment: async (id, enrollmentData) => {
    const response = await api.put(`/api/enrollment/${id}`, enrollmentData);
    return response.data;
  },

  deleteEnrollment: async (id) => {
    const response = await api.delete(`/api/enrollment/${id}`);
    return response.data;
  },

  getStudentEnrollments: async (studentId) => {
    const response = await api.get(`/api/enrollment/student/${studentId}`);
    return response.data;
  },

  getCourseEnrollments: async (courseId) => {
    const response = await api.get(`/api/enrollment/course/${courseId}`);
    return response.data;
  },
};

export default enrollmentService;
