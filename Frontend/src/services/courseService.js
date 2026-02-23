import api from './api';

const courseService = {
  getCourses: async (params = {}) => {
    const response = await api.get('/api/course', { params });
    return response.data;
  },

  getCourseById: async (id) => {
    const response = await api.get(`/api/course/${id}`);
    return response.data;
  },

  createCourse: async (courseData) => {
    const response = await api.post('/api/course', courseData);
    return response.data;
  },

  updateCourse: async (id, courseData) => {
    const response = await api.put(`/api/course/${id}`, courseData);
    return response.data;
  },

  deleteCourse: async (id) => {
    const response = await api.delete(`/api/course/${id}`);
    return response.data;
  },

  // Department related methods
  getDepartments: async () => {
    const response = await api.get('/api/department');
    return response.data;
  },

  getDepartmentById: async (id) => {
    const response = await api.get(`/api/department/${id}`);
    return response.data;
  },

  createDepartment: async (departmentData) => {
    const response = await api.post('/api/department', departmentData);
    return response.data;
  },

  updateDepartment: async (id, departmentData) => {
    const response = await api.put(`/api/department/${id}`, departmentData);
    return response.data;
  },

  deleteDepartment: async (id) => {
    const response = await api.delete(`/api/department/${id}`);
    return response.data;
  },
};

export default courseService;
