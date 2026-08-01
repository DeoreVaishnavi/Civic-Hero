import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data;

function toFormData(request) {
  const formData = new FormData();
  [
    'title', 'description', 'category', 'citizenSeverity', 'departmentId', 'wardId',
    'latitude', 'longitude', 'address', 'landmark', 'possibleEmergency', 'emergencyReason',
  ].forEach((key) => formData.append(key, request[key] == null ? '' : String(request[key])));
  (request.evidence || request.images || []).forEach((file) => formData.append('evidence', file, file.name));
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
  preSubmissionReview: async (request) => unwrap(await axiosInstance.post('/complaints/pre-submission-review', request)),
  create: async (request, options = {}) => unwrap(await axiosInstance.post(
    '/complaints',
    toFormData(request),
    {
      timeout: 120000,
      onUploadProgress: options.onUploadProgress,
    },
  )),
  update: async (id, request) => unwrap(await axiosInstance.put(`/complaints/${id}`, request)),
  deleteBeforeAssignment: async (id) => unwrap(await axiosInstance.delete(`/complaints/${id}`)),
  withdraw: async (id) => unwrap(await axiosInstance.delete(`/complaints/${id}`)),
  upvote: async (id) => unwrap(await axiosInstance.post(`/complaints/${id}/upvote`)),
  removeUpvote: async (id) => unwrap(await axiosInstance.delete(`/complaints/${id}/upvote`)),
  follow: async (id) => unwrap(await axiosInstance.post(`/complaints/${id}/follow`)),
  unfollow: async (id) => unwrap(await axiosInstance.delete(`/complaints/${id}/follow`)),
  followStatus: async (id) => unwrap(await axiosInstance.get(`/complaints/${id}/follow-status`)),
  followingIds: async () => unwrap(await axiosInstance.get('/complaints/following/ids')),
  following: async (params = {}) => unwrap(await axiosInstance.get('/complaints/following', { params })),
  removeEvidence: async (complaintId, evidenceId) => unwrap(await axiosInstance.delete(`/complaints/${complaintId}/images/${evidenceId}`)),
  downloadImage: async (complaintId, imageId) => axiosInstance.get(`/complaints/${complaintId}/images/${imageId}`, { responseType: 'blob', timeout: 30000 }),
};
