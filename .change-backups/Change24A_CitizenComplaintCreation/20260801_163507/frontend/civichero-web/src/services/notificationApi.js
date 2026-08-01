import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const notificationApi = {
  list: async (params = {}) => unwrap(await axiosInstance.get('/notifications', { params })),
  unread: async () => unwrap(await axiosInstance.get('/notifications/unread')),
  markRead: async (id) => unwrap(await axiosInstance.post(`/notifications/${id}/read`)),
  markAllRead: async () => unwrap(await axiosInstance.post('/notifications/read-all')),
  remove: async (id) => unwrap(await axiosInstance.delete(`/notifications/${id}`)),
  preferences: async () => unwrap(await axiosInstance.get('/notifications/preferences')),
  updatePreferences: async (payload) => unwrap(await axiosInstance.put('/notifications/preferences', payload)),
  broadcast: async (payload) => unwrap(await axiosInstance.post('/notifications/broadcast', payload)),
};
