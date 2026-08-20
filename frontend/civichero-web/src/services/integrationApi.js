
import axiosInstance from '../api/axiosInstance.js';
const unwrap = (response) => response.data?.data ?? response.data;
export const integrationApi = {
  readiness: async () => unwrap(await axiosInstance.get('/integration/readiness')),
  testCache: async () => unwrap(await axiosInstance.post('/integration/cache/test')),
  testMessaging: async () => unwrap(await axiosInstance.post('/integration/messaging/test')),
};
