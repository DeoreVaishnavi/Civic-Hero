import axiosInstance from '../api/axiosInstance.js';
const unwrap = (response) => response.data?.data;
export const commentApi = {
  moderationQueue: async (params = {}) => unwrap(await axiosInstance.get('/comment-moderation', { params })),
  list: async (complaintId) => unwrap(await axiosInstance.get(`/complaints/${complaintId}/comments`)),
  add: async (complaintId, request) => unwrap(await axiosInstance.post(`/complaints/${complaintId}/comments`, request)),
  remove: async (complaintId, commentId) => axiosInstance.delete(`/complaints/${complaintId}/comments/${commentId}`),
  moderate: async (complaintId, commentId, request) => unwrap(await axiosInstance.post(`/complaints/${complaintId}/comments/${commentId}/moderate`, request)),
};
