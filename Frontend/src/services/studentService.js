import api from './api';

const studentService = {
  getStudents: async (params = {}) => {
    const response = await api.get('/api/student', { params });
    return response.data;
  },

  getStudentById: async (id) => {
    const response = await api.get(`/api/student/${id}`);
    return response.data;
  },

  createStudent: async (studentData) => {
    const response = await api.post('/api/student', studentData);
    return response.data;
  },

  updateStudent: async (id, studentData) => {
    const response = await api.put(`/api/student/${id}`, studentData);
    return response.data;
  },

  deleteStudent: async (id) => {
    const response = await api.delete(`/api/student/${id}`);
    return response.data;
  },

  updateStudentStatus: async (id, status) => {
    const response = await api.put(`/api/student/${id}/status`, status, {
      headers: {
        'Content-Type': 'application/json',
      },
    });
    return response.data;
  },

  checkStudentExists: async (id) => {
    const response = await api.get(`/api/student/${id}/exists`);
    return response.data;
  },
};

export default studentService;
