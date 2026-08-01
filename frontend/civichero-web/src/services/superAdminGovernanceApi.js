import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const superAdminGovernanceApi = {
  getAdminAccounts: async (params = {}) => unwrap(await axiosInstance.get('/superadmin/governance/admin-accounts', { params })),
  createAdmin: async (payload) => unwrap(await axiosInstance.post('/superadmin/governance/admin-accounts', payload)),
  setAdminActive: async (userId, payload) => unwrap(await axiosInstance.put(`/superadmin/governance/admin-accounts/${userId}/active`, payload)),
  resetAdminPassword: async (userId, payload) => unwrap(await axiosInstance.post(`/superadmin/governance/admin-accounts/${userId}/reset-password`, payload)),
  getPolicies: async () => unwrap(await axiosInstance.get('/superadmin/governance/policies')),
  updateRolePolicy: async (payload) => unwrap(await axiosInstance.put('/superadmin/governance/policies/roles', payload)),
  updateAuthenticationPolicy: async (payload) => unwrap(await axiosInstance.put('/superadmin/governance/policies/authentication', payload)),
  getSessions: async (params = {}) => unwrap(await axiosInstance.get('/superadmin/governance/sessions', { params })),
  revokeSession: async (sessionId, payload) => unwrap(await axiosInstance.delete(`/superadmin/governance/sessions/${encodeURIComponent(sessionId)}`, { data: payload })),
  getReleaseDecisions: async (take = 100) => unwrap(await axiosInstance.get('/superadmin/governance/release-decisions', { params: { take } })),
  decideRelease: async (target, payload) => unwrap(await axiosInstance.post(`/superadmin/governance/release-decisions/${encodeURIComponent(target)}`, payload)),
};
