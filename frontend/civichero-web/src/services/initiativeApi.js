import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const initiativeApi = {
  engagement: async (initiativeId) => unwrap(await axiosInstance.get(`/initiatives/${initiativeId}/engagement`)),
  toggleFollow: async (initiativeId) => unwrap(await axiosInstance.post(`/initiatives/${initiativeId}/follow`)),
  submitFeedback: async (initiativeId, payload) => unwrap(await axiosInstance.post(`/initiatives/${initiativeId}/feedback`, payload)),
  supervisorActivity: async (params = {}) => unwrap(await axiosInstance.get('/initiatives/supervisor/activity', { params })),
};
