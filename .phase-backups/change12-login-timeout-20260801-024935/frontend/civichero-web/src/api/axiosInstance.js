import axios from 'axios';
import { useAuthStore } from '../store/authStore.js';
import { refreshAccessToken } from './tokenRefreshHandler.js';

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 10000,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' },
});

function removeContentTypeForFormData(config) {
  if (!(config.data instanceof FormData)) return;

  // The browser must generate the multipart boundary. A manually supplied
  // multipart Content-Type can reach ASP.NET without a boundary and leave all
  // [FromForm] complaint fields empty.
  if (typeof config.headers?.delete === 'function') {
    config.headers.delete('Content-Type');
  } else if (config.headers) {
    delete config.headers['Content-Type'];
    delete config.headers['content-type'];
  }
}

function normalizeValidationErrors(errors) {
  if (!errors) return [];
  if (Array.isArray(errors)) return errors.filter(Boolean).map(String);
  if (typeof errors === 'string') return [errors];
  if (typeof errors !== 'object') return [];

  return Object.values(errors)
    .flatMap((value) => (Array.isArray(value) ? value : [value]))
    .filter(Boolean)
    .map(String);
}

axiosInstance.interceptors.request.use((config) => {
  const correlationId = globalThis.crypto?.randomUUID?.() || `${Date.now()}-${Math.random()}`;
  config.headers.set('X-Correlation-ID', correlationId);
  removeContentTypeForFormData(config);

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

    const isTimeout = error.code === 'ECONNABORTED' || /timeout/i.test(error.message || '');
    const errors = normalizeValidationErrors(error.response?.data?.errors);

    return Promise.reject({
      status: error.response?.status ?? 0,
      code: error.code || null,
      isTimeout,
      message: error.response?.data?.message ||
        (isTimeout
          ? 'The request took too long. Check your connection and try again.'
          : error.message || 'Unable to communicate with CivicHero.'),
      errors,
      traceId: error.response?.data?.traceId || error.response?.headers?.['x-correlation-id'] || null,
      originalError: error,
    });
  },
);

export default axiosInstance;
