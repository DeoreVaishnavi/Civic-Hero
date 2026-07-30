import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const releaseApi = {
  readiness: async () => unwrap(await axiosInstance.get('/release/readiness')),
};
