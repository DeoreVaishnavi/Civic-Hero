import axios from 'axios';

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
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
      traceId: error.response?.data?.traceId ?? null,
      originalError: error,
    };

    return Promise.reject(normalizedError);
  },
);

export default axiosInstance;
