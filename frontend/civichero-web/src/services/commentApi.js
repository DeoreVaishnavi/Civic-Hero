import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data;

export const commentApi = {
  moderationQueue: async (params = {}) => unwrap(await axiosInstance.get('/comment-moderation', { params })),
  list: async (complaintId) => unwrap(await axiosInstance.get(`/complaints/${complaintId}/comments`)),
  add: async (complaintId, request) => unwrap(await axiosInstance.post(`/complaints/${complaintId}/comments`, request)),
  update: async (complaintId, commentId, request) => unwrap(await axiosInstance.put(`/complaints/${complaintId}/comments/${commentId}`, request)),
  report: async (complaintId, commentId, request) => unwrap(await axiosInstance.post(`/complaints/${complaintId}/comments/${commentId}/report`, request)),
  remove: async (complaintId, commentId) => axiosInstance.delete(`/complaints/${complaintId}/comments/${commentId}`),
  moderate: async (complaintId, commentId, request) => unwrap(await axiosInstance.post(`/complaints/${complaintId}/comments/${commentId}/moderate`, request)),
};
