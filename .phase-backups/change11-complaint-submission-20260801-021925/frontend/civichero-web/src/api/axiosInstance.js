import axios from 'axios';
import { useAuthStore } from '../store/authStore.js';
import { refreshAccessToken } from './tokenRefreshHandler.js';

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 10000,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

axiosInstance.interceptors.request.use((config) => {
  const correlationId = globalThis.crypto?.randomUUID?.() || `${Date.now()}-${Math.random()}`;
  config.headers.set('X-Correlation-ID', correlationId);

  const accessToken = useAuthStore.getState().accessToken;
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`);
  }

  return config;
});

axiosInstance.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const isAuthRequest = originalRequest?.url?.includes('/auth/login') ||
      originalRequest?.url?.includes('/auth/register') ||
      originalRequest?.url?.includes('/auth/verify-email') ||
      originalRequest?.url?.includes('/auth/forgot-password') ||
      originalRequest?.url?.includes('/auth/reset-password') ||
      originalRequest?.url?.includes('/auth/refresh-token') ||
      originalRequest?.url?.includes('/auth/phone/');

    if (error.response?.status === 401 && !isAuthRequest && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true;
      try {
        const token = await refreshAccessToken();
        originalRequest.headers.Authorization = `Bearer ${token}`;
        return axiosInstance(originalRequest);
      } catch {
        // Normalized below.
      }
    }

    return Promise.reject({
      status: error.response?.status ?? 0,
      message: error.response?.data?.message || error.message || 'Unable to communicate with CivicHero.',
      errors: error.response?.data?.errors ?? [],
      traceId: error.response?.data?.traceId || error.response?.headers?.['x-correlation-id'] || null,
      originalError: error,
    });
  },
);

export default axiosInstance;
