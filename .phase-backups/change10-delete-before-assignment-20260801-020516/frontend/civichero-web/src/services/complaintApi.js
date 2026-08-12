import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data;

function toFormData(request) {
  const formData = new FormData();
  ['title', 'description', 'category', 'departmentId', 'wardId', 'latitude', 'longitude', 'address', 'possibleEmergency', 'emergencyReason']
    .forEach((key) => formData.append(key, request[key] ?? ''));
  (request.images || []).forEach((image) => formData.append('images', image));
  return formData;
}

export const complaintApi = {
  metadata: async () => unwrap(await axiosInstance.get('/complaints/metadata')),
  dashboard: async () => unwrap(await axiosInstance.get('/complaints/stats/dashboard')),
  list: async (params = {}) => unwrap(await axiosInstance.get('/complaints', { params })),
  publicFeed: async (params = {}) => unwrap(await axiosInstance.get('/complaints/public', { params })),
  mine: async (params = {}) => unwrap(await axiosInstance.get('/complaints/mine', { params })),
  nearby: async (params) => unwrap(await axiosInstance.get('/complaints/nearby', { params })),
  getById: async (id) => unwrap(await axiosInstance.get(`/complaints/${id}`)),
  create: async (request) => unwrap(await axiosInstance.post('/complaints', toFormData(request), {
    headers: { 'Content-Type': 'multipart/form-data' }, timeout: 45000,
  })),
  update: async (id, request) => unwrap(await axiosInstance.put(`/complaints/${id}`, request)),
  withdraw: async (id) => unwrap(await axiosInstance.delete(`/complaints/${id}`)),
  upvote: async (id) => unwrap(await axiosInstance.post(`/complaints/${id}/upvote`)),
  removeUpvote: async (id) => unwrap(await axiosInstance.delete(`/complaints/${id}/upvote`)),
  downloadImage: async (complaintId, imageId) => axiosInstance.get(`/complaints/${complaintId}/images/${imageId}`, { responseType: 'blob', timeout: 30000 }),
};
