import axiosInstance from '../api/axiosInstance.js';

const data = (response) => response.data?.data;
const LOGIN_TIMEOUT_MS = 60_000;
const OTP_REQUEST_TIMEOUT_MS = 45_000;

const timeoutConfig = (timeout, civicTimeoutMessage) => ({
  timeout,
  civicTimeoutMessage,
});

export const authApi = {
  register: async (request) => data(await axiosInstance.post('/auth/register', request)),
  verifyEmail: async (request) => data(await axiosInstance.post('/auth/verify-email', request)),
  login: async (request) => data(await axiosInstance.post(
    '/auth/login',
    request,
    timeoutConfig(
      LOGIN_TIMEOUT_MS,
      'Login took longer than expected. Confirm that the CivicHero backend and database are running, then try again.',
    ),
  )),
  forgotPassword: async (identifier) => data(await axiosInstance.post('/auth/forgot-password', { identifier })),
  resetPassword: async (request) => axiosInstance.post('/auth/reset-password', request),
  requestPhoneLoginOtp: async (phoneNumber) => data(await axiosInstance.post(
    '/auth/phone/request-login-otp',
    { phoneNumber },
    timeoutConfig(
      OTP_REQUEST_TIMEOUT_MS,
      'The OTP request took longer than expected. Confirm that the backend is reachable, then try again.',
    ),
  )),
  phoneLogin: async (request) => data(await axiosInstance.post(
    '/auth/phone/login',
    request,
    timeoutConfig(
      LOGIN_TIMEOUT_MS,
      'OTP login took longer than expected. Confirm that the CivicHero backend and database are running, then try again.',
    ),
  )),
  requestPhoneVerification: async (phoneNumber) => data(await axiosInstance.post('/auth/phone/request-verification', { phoneNumber })),
  verifyPhone: async (request) => data(await axiosInstance.post('/auth/phone/verify', request)),
  logout: async () => axiosInstance.post('/auth/logout'),
  getMe: async () => data(await axiosInstance.get('/auth/me', { timeout: 30_000 })),
};
