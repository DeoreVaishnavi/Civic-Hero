import axiosInstance from '../api/axiosInstance.js';

const data = (response) => response.data?.data;

export const authApi = {
  register: async (request) => data(await axiosInstance.post('/auth/register', request)),
  verifyEmail: async (request) => data(await axiosInstance.post('/auth/verify-email', request)),
  login: async (request) => data(await axiosInstance.post('/auth/login', request)),
  forgotPassword: async (identifier) => data(await axiosInstance.post('/auth/forgot-password', { identifier })),
  resetPassword: async (request) => axiosInstance.post('/auth/reset-password', request),
  requestPhoneLoginOtp: async (phoneNumber) => data(await axiosInstance.post('/auth/phone/request-login-otp', { phoneNumber })),
  phoneLogin: async (request) => data(await axiosInstance.post('/auth/phone/login', request)),
  requestPhoneVerification: async (phoneNumber) => data(await axiosInstance.post('/auth/phone/request-verification', { phoneNumber })),
  verifyPhone: async (request) => data(await axiosInstance.post('/auth/phone/verify', request)),
  logout: async () => axiosInstance.post('/auth/logout'),
  getMe: async () => data(await axiosInstance.get('/auth/me')),
};
