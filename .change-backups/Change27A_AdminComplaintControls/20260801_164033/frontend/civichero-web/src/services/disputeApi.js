import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

async function downloadEvidence(disputeId, evidence) {
  const response = await axiosInstance.get(`/disputes/${disputeId}/evidence/${evidence.id}`, { responseType: 'blob' });
  const url = URL.createObjectURL(response.data);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = evidence.fileName || `dispute-evidence-${evidence.id}`;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(url);
}

async function uploadEvidence(id, files, requestId = null) {
  const form = new FormData();
  files.forEach((file) => form.append('evidence', file));
  if (requestId) form.append('requestId', String(requestId));
  return unwrap(await axiosInstance.post(`/disputes/${id}/evidence`, form));
}

export const disputeApi = {
  mine: async () => unwrap(await axiosInstance.get('/disputes/mine')),
  officerMine: async () => unwrap(await axiosInstance.get('/disputes/officer')),
  queue: async (appealsOnly = false) => unwrap(await axiosInstance.get('/disputes/queue', { params: { appealsOnly } })),
  get: async (id) => unwrap(await axiosInstance.get(`/disputes/${id}`)),
  history: async (id) => unwrap(await axiosInstance.get(`/disputes/${id}/history`)),
  raise: async (complaintId, reason) => unwrap(await axiosInstance.post(`/disputes/complaints/${complaintId}`, { reason })),
  uploadEvidence,
  downloadEvidence,
  requestEvidence: async (id, payload) => unwrap(await axiosInstance.post(`/disputes/${id}/evidence-request`, payload)),
  supervisorDecision: async (id, payload) => unwrap(await axiosInstance.post(`/disputes/${id}/supervisor-decision`, payload)),
  reopen: async (id, reason) => unwrap(await axiosInstance.post(`/disputes/${id}/reopen-request`, { reason })),
  appeal: async (id, remarks) => unwrap(await axiosInstance.post(`/disputes/${id}/appeal`, { remarks })),
  adminDecision: async (id, payload) => unwrap(await axiosInstance.post(`/disputes/${id}/admin-decision`, payload)),
  superAdminDecision: async (id, payload) => unwrap(await axiosInstance.post(`/disputes/${id}/superadmin-decision`, payload)),
};
