import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const rewardApi = {
  points: async () => unwrap(await axiosInstance.get('/rewards/points')),
  leaderboard: async (limit = 25, timeframe = 'AllTime') => unwrap(await axiosInstance.get('/rewards/leaderboard', { params: { limit, timeframe } })),
  leaderboardCitizenProfile: async (userId) => unwrap(await axiosInstance.get(`/rewards/leaderboard/${userId}/profile`)),
  followCitizen: async (userId) => unwrap(await axiosInstance.post(`/rewards/leaderboard/${userId}/follow`)),
  unfollowCitizen: async (userId) => unwrap(await axiosInstance.delete(`/rewards/leaderboard/${userId}/follow`)),
  badges: async () => unwrap(await axiosInstance.get('/rewards/badges')),
  catalog: async () => unwrap(await axiosInstance.get('/rewards/catalog')),
  redeem: async (rewardCatalogId) => unwrap(await axiosInstance.post('/rewards/redeem', { rewardCatalogId })),
  redemptions: async () => unwrap(await axiosInstance.get('/rewards/redemptions')),
  history: async () => unwrap(await axiosInstance.get('/rewards/history')),
  certificate: async () => {
    const response = await axiosInstance.get('/rewards/certificate', { responseType: 'blob' });
    const url = URL.createObjectURL(response.data);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = 'CivicHero-Certificate.html';
    anchor.click();
    URL.revokeObjectURL(url);
  },

  adminCatalog: async () => unwrap(await axiosInstance.get('/admin/rewards/catalog')),
  createReward: async (payload) => unwrap(await axiosInstance.post('/admin/rewards/catalog', payload)),
  updateReward: async (id, payload) => unwrap(await axiosInstance.put(`/admin/rewards/catalog/${id}`, payload)),
  refillStock: async (id, payload) => unwrap(await axiosInstance.post(`/admin/rewards/catalog/${id}/stock`, payload)),
  activateReward: async (id) => unwrap(await axiosInstance.post(`/admin/rewards/catalog/${id}/activate`)),
  deactivateReward: async (id) => unwrap(await axiosInstance.delete(`/admin/rewards/catalog/${id}`)),
  rewardRules: async () => unwrap(await axiosInstance.get('/admin/rewards/rules')),
  saveBadgeRules: async (rules) => unwrap(await axiosInstance.put('/admin/rewards/rules/badges', { rules })),
  saveTierRules: async (rules) => unwrap(await axiosInstance.put('/admin/rewards/rules/tiers', { rules })),
  adjustPoints: async (payload) => unwrap(await axiosInstance.post('/admin/rewards/points/adjust', payload)),
  adminRedemptions: async (params = {}) => unwrap(await axiosInstance.get('/admin/rewards/redemptions', { params })),
  updateRedemptionStatus: async (id, payload) => unwrap(await axiosInstance.put(`/admin/rewards/redemptions/${id}/status`, payload)),
};
