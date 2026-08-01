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

  adminTemplates: async () => unwrap(await axiosInstance.get('/admin/notifications/templates')),
  createAdminTemplate: async (payload) => unwrap(await axiosInstance.post('/admin/notifications/templates', payload)),
  updateAdminTemplate: async (key, payload) => unwrap(await axiosInstance.put(`/admin/notifications/templates/${encodeURIComponent(key)}`, payload)),
  deleteAdminTemplate: async (key) => unwrap(await axiosInstance.delete(`/admin/notifications/templates/${encodeURIComponent(key)}`)),
  adminBroadcast: async (payload) => unwrap(await axiosInstance.post('/admin/notifications/broadcast', payload)),
  adminDeliveries: async (params = {}) => unwrap(await axiosInstance.get('/admin/notifications/deliveries', { params })),
  adminDeliverySummary: async () => unwrap(await axiosInstance.get('/admin/notifications/deliveries/summary')),
  retryAdminDelivery: async (id, payload) => unwrap(await axiosInstance.post(`/admin/notifications/deliveries/${id}/retry`, payload)),
  adminSchedules: async () => unwrap(await axiosInstance.get('/admin/notifications/schedules')),
  cancelAdminSchedule: async (id) => unwrap(await axiosInstance.delete(`/admin/notifications/schedules/${encodeURIComponent(id)}`)),
};
