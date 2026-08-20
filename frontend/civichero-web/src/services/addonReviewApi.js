import axiosInstance from '../api/axiosInstance.js';
const unwrap = (response) => response.data?.data;
export const emergencyReviewApi = {
  list: async (params = {}) => unwrap(await axiosInstance.get('/emergency-reviews', { params })),
  decide: async (id, request) => unwrap(await axiosInstance.post(`/emergency-reviews/${id}/decision`, request)),
};
export const visualVerificationApi = {
  list: async (params = {}) => unwrap(await axiosInstance.get('/visual-verification', { params })),
  get: async (id) => unwrap(await axiosInstance.get(`/visual-verification/${id}`)),
  analyze: async (complaintId) => unwrap(await axiosInstance.post(`/visual-verification/complaints/${complaintId}/analyze`)),
  review: async (id, request) => unwrap(await axiosInstance.post(`/visual-verification/${id}/review`, request)),
};
