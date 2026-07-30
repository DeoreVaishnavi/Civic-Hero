import axiosInstance from '../api/axiosInstance.js';
const unwrap = (response) => response.data?.data ?? response.data;
export const finalReleaseApi = {
  readiness: async () => unwrap(await axiosInstance.get('/final-release/readiness')),
};
