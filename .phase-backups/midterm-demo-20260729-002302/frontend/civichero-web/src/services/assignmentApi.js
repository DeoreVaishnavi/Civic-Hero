import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data;

function completionForm(request) {
  const form = new FormData();
  form.append('notes', request.notes);
  if (request.latitude !== '' && request.latitude != null) form.append('latitude', request.latitude);
  if (request.longitude !== '' && request.longitude != null) form.append('longitude', request.longitude);
  (request.evidence || []).forEach((file) => form.append('evidence', file));
  return form;
}

export const assignmentApi = {
  officerDashboard: async () => unwrap(await axiosInstance.get('/assignments/dashboard/officer')),
  supervisorDashboard: async () => unwrap(await axiosInstance.get('/assignments/dashboard/supervisor')),
  mine: async (params = {}) => unwrap(await axiosInstance.get('/assignments/mine', { params })),
  pending: async (params = {}) => unwrap(await axiosInstance.get('/assignments/pending', { params })),
  overdue: async () => unwrap(await axiosInstance.get('/assignments/overdue')),
  workload: async (params = {}) => unwrap(await axiosInstance.get('/assignments/workload', { params })),
  eligibleOfficers: async (complaintId) => unwrap(await axiosInstance.get(`/assignments/eligible-officers/${complaintId}`)),
  getByComplaintId: async (complaintId) => unwrap(await axiosInstance.get(`/assignments/${complaintId}`)),
  history: async (complaintId) => unwrap(await axiosInstance.get(`/assignments/${complaintId}/history`)),
  assign: async (request) => unwrap(await axiosInstance.post('/assignments', request)),
  bulkAssign: async (request) => unwrap(await axiosInstance.post('/assignments/bulk', request)),
  reassign: async (complaintId, request) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/reassign`, request)),
  accept: async (complaintId) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/accept`)),
  reject: async (complaintId, reason) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/reject`, { reason })),
  progress: async (complaintId, request) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/progress`, request)),
  complete: async (complaintId, request) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/complete`, completionForm(request), {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 45000,
  })),
};
