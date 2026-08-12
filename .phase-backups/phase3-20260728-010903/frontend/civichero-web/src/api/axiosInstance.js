import axios from 'axios';

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

axiosInstance.interceptors.request.use((config) => {
  const correlationId = globalThis.crypto?.randomUUID?.() || `${Date.now()}-${Math.random()}`;
  config.headers.set('X-Correlation-ID', correlationId);
  return config;
});

axiosInstance.interceptors.response.use(
  (response) => response,
  (error) => {
    const normalizedError = {
      status: error.response?.status ?? 0,
      message:
        error.response?.data?.message ||
        error.message ||
        'Unable to communicate with the CivicHero API.',
      errors: error.response?.data?.errors ?? [],
      traceId:
        error.response?.data?.traceId ||
        error.response?.headers?.['x-correlation-id'] ||
        null,
      originalError: error,
    };

    return Promise.reject(normalizedError);
  },
);

export default axiosInstance;
