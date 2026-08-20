import axiosInstance from '../api/axiosInstance.js';

let cached;
let expiresAt = 0;

export const publicSettingsApi = {
  async get(force = false) {
    if (!force && cached && Date.now() < expiresAt) return cached;
    const response = await axiosInstance.get('/public/settings');
    cached = response.data?.data ?? response.data;
    expiresAt = Date.now() + 60_000;
    return cached;
  },
};
