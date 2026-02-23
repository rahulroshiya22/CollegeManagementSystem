import api from './api';

const feeService = {
  getFees: async (params = {}) => {
    const response = await api.get('/api/fee', { params });
    return response.data;
  },

  getFeeById: async (id) => {
    const response = await api.get(`/api/fee/${id}`);
    return response.data;
  },

  createFee: async (feeData) => {
    const response = await api.post('/api/fee', feeData);
    return response.data;
  },

  updateFee: async (id, feeData) => {
    const response = await api.put(`/api/fee/${id}`, feeData);
    return response.data;
  },

  deleteFee: async (id) => {
    const response = await api.delete(`/api/fee/${id}`);
    return response.data;
  },

  processPayment: async (paymentData) => {
    const response = await api.post('/api/fee/payment', paymentData);
    return response.data;
  },

  getStudentFees: async (studentId) => {
    const response = await api.get(`/api/fee/student/${studentId}`);
    return response.data;
  },

  getFeeSummary: async () => {
    const response = await api.get('/api/fee/summary');
    return response.data;
  },

  getPaymentHistory: async (params = {}) => {
    const response = await api.get('/api/fee/payments', { params });
    return response.data;
  },
};

export default feeService;
