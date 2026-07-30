import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const qualityApi = {
  readiness: async () => unwrap(await axiosInstance.get('/quality/readiness')),
};
