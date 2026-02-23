import api from './api';

const authService = {
  login: async (credentials) => {
    const response = await api.post('/api/auth/login', credentials);
    return response.data;
  },

  register: async (userData) => {
    const response = await api.post('/api/auth/register', userData);
    return response.data;
  },

  getCurrentUser: async () => {
    const response = await api.get('/api/auth/me');
    return response.data;
  },

  logout: async (refreshToken) => {
    const response = await api.post('/api/auth/logout', { refreshToken });
    return response.data;
  },

  refreshToken: async (refreshToken) => {
    const response = await api.post('/api/auth/refresh', { refreshToken });
    return response.data;
  },

  changePassword: async (passwordData) => {
    const response = await api.put('/api/auth/change-password', passwordData);
    return response.data;
  },

  uploadPhoto: async (photo) => {
    const formData = new FormData();
    formData.append('photo', photo);
    const response = await api.post('/api/auth/upload-photo', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  deletePhoto: async () => {
    const response = await api.delete('/api/auth/delete-photo');
    return response.data;
  },

  googleLogin: async (code) => {
    const response = await api.post('/api/auth/google-login', { code });
    return response.data;
  },
};

export default authService;
