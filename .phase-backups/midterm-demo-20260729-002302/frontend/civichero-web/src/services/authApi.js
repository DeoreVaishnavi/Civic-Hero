import axiosInstance from '../api/axiosInstance.js';

const data = (response) => response.data?.data;

export const authApi = {
  register: async (request) => data(await axiosInstance.post('/auth/register', request)),
  verifyEmail: async (request) => data(await axiosInstance.post('/auth/verify-email', request)),
  login: async (request) => data(await axiosInstance.post('/auth/login', request)),
  logout: async () => axiosInstance.post('/auth/logout'),
  getMe: async () => data(await axiosInstance.get('/auth/me')),
};
