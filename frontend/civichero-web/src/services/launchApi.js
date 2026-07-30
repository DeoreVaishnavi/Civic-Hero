import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const launchApi = {
  readiness: async () => unwrap(await axiosInstance.get('/launch/readiness')),
  documents: async () => unwrap(await axiosInstance.get('/launch/document-catalog')),
};
