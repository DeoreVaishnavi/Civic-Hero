import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const securityApi = {
  getSession: async () => unwrap(await axiosInstance.get('/security/session')),
  changePassword: async (request) => unwrap(await axiosInstance.post('/security/change-password', request)),
  getMyActivity: async (take = 50) => unwrap(await axiosInstance.get('/security/activity', { params: { take } })),
  getMySessions: async () => unwrap(await axiosInstance.get('/security/sessions')),
  revokeMySession: async (sessionId) => unwrap(await axiosInstance.delete(`/security/sessions/${encodeURIComponent(sessionId)}`)),
  revokeMySessions: async () => unwrap(await axiosInstance.post('/security/sessions/revoke-all')),
  getOverview: async () => unwrap(await axiosInstance.get('/security/admin/overview')),
  getEvents: async (take = 50) => unwrap(await axiosInstance.get('/security/admin/events', { params: { take } })),
  getLockedAccounts: async () => unwrap(await axiosInstance.get('/security/admin/locked-accounts')),
  unlockAccount: async (id) => unwrap(await axiosInstance.post(`/security/admin/users/${id}/unlock`)),
  revokeUserSessions: async (id) => unwrap(await axiosInstance.post(`/security/admin/users/${id}/revoke-sessions`)),
  getTwoFactorStatus: async () => unwrap(await axiosInstance.get('/security/two-factor/status')),
  beginTwoFactorSetup: async () => unwrap(await axiosInstance.post('/security/two-factor/setup')),
  enableTwoFactor: async (code) => unwrap(await axiosInstance.post('/security/two-factor/enable', { code })),
  disableTwoFactor: async (code) => unwrap(await axiosInstance.post('/security/two-factor/disable', { code })),
  regenerateRecoveryCodes: async (code) => unwrap(await axiosInstance.post('/security/two-factor/recovery-codes', { code })),
};
