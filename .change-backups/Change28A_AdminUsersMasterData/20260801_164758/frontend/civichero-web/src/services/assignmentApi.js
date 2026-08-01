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

function progressEvidenceForm(request) {
  const form = new FormData();
  form.append('message', request.message);
  form.append('progressPercent', request.progressPercent);
  if (request.latitude !== '' && request.latitude != null) form.append('latitude', request.latitude);
  if (request.longitude !== '' && request.longitude != null) form.append('longitude', request.longitude);
  if (request.estimatedCompletionAt) form.append('estimatedCompletionAt', request.estimatedCompletionAt);
  (request.evidence || []).forEach((file) => form.append('evidence', file));
  return form;
}

export const assignmentApi = {
  officerDashboard: async () => unwrap(await axiosInstance.get('/assignments/dashboard/officer')),
  officerPerformance: async (days = 90) => unwrap(await axiosInstance.get('/assignments/dashboard/officer/performance', { params: { days } })),
  officerMap: async () => unwrap(await axiosInstance.get('/assignments/map/officer')),
  supervisorDashboard: async () => unwrap(await axiosInstance.get('/assignments/dashboard/supervisor')),
  mine: async (params = {}) => unwrap(await axiosInstance.get('/assignments/mine', { params })),
  pending: async (params = {}) => unwrap(await axiosInstance.get('/assignments/pending', { params })),
  overdue: async () => unwrap(await axiosInstance.get('/assignments/overdue')),
  workload: async (params = {}) => unwrap(await axiosInstance.get('/assignments/workload', { params })),
  eligibleOfficers: async (complaintId) => unwrap(await axiosInstance.get(`/assignments/eligible-officers/${complaintId}`)),
  getByComplaintId: async (complaintId) => unwrap(await axiosInstance.get(`/assignments/${complaintId}`)),
  history: async (complaintId) => unwrap(await axiosInstance.get(`/assignments/${complaintId}/history`)),
  escalationHistory: async (params = {}) => unwrap(await axiosInstance.get('/assignments/escalation-history', { params })),
  assign: async (request) => unwrap(await axiosInstance.post('/assignments', request)),
  bulkAssign: async (request) => unwrap(await axiosInstance.post('/assignments/bulk', request)),
  reassign: async (complaintId, request) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/reassign`, request)),
  escalate: async (complaintId, reason) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/escalate`, { reason })),
  resume: async (complaintId, reason) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/resume`, { reason })),
  requestRework: async (complaintId, reason) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/request-rework`, { reason })),
  sendInstruction: async (complaintId, message) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/instructions`, { message })),
  requestCitizenEvidence: async (complaintId, message) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/request-citizen-evidence`, { message })),
  accept: async (complaintId) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/accept`)),
  reject: async (complaintId, reason) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/reject`, { reason })),
  requestTransfer: async (complaintId, request) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/transfer-request`, request)),
  progress: async (complaintId, request) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/progress`, request)),
  progressWithEvidence: async (complaintId, request) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/progress-evidence`, progressEvidenceForm(request), {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 60000,
  })),
  requestCitizenInformation: async (complaintId, message) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/request-citizen-information`, { message })),
  downloadEvidence: async (path, fileName = 'evidence') => {
    const response = await axiosInstance.get(path, { responseType: 'blob', timeout: 30000 });
    const url = URL.createObjectURL(response.data);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
  },
  complete: async (complaintId, request) => unwrap(await axiosInstance.post(`/assignments/${complaintId}/complete`, completionForm(request), {
    headers: { 'Content-Type': 'multipart/form-data' },
    timeout: 45000,
  })),
};
