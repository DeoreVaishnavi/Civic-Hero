import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const aiApi = {
  classify: async (payload) => unwrap(await axiosInstance.post('/ai/classify', payload)),
  duplicateCheck: async (payload) => unwrap(await axiosInstance.post('/ai/duplicate-check', payload)),
  fraudCheck: async (payload) => unwrap(await axiosInstance.post('/ai/fraud-check', payload)),
  priority: async (payload) => unwrap(await axiosInstance.post('/ai/priority-predict', payload)),
  analyzeComplaint: async (complaintId, force = false) => unwrap(await axiosInstance.post(`/ai/complaints/${complaintId}/analyze`, null, { params: { force } })),
  mediaForensics: async (complaintId) => unwrap(await axiosInstance.post(`/ai/complaints/${complaintId}/media-forensics/analyze`)),
  reviewQueue: async () => unwrap(await axiosInstance.get('/ai/review-queue')),
  decide: async (complaintId, payload) => unwrap(await axiosInstance.post(`/ai/review-queue/${complaintId}/decision`, payload)),
  hotspots: async (days = 30) => unwrap(await axiosInstance.get('/ai/hotspots', { params: { days } })),
  metrics: async () => unwrap(await axiosInstance.get('/ai/metrics')),
};
