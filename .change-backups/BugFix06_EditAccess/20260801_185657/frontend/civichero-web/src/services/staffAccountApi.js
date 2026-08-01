import axiosInstance from '../api/axiosInstance.js';

const data = (response) => response.data?.data ?? response.data;

export const staffAccountApi = {
  getMetadata: async () => data(await axiosInstance.get('/staff-accounts/metadata')),
  getAccounts: async (params = {}) => data(await axiosInstance.get('/staff-accounts', { params })),
  getPending: async (search = '') => data(await axiosInstance.get('/staff-accounts/pending', { params: search ? { search } : {} })),
  create: async (request) => data(await axiosInstance.post('/staff-accounts', request)),
  review: async (userId, request) => data(await axiosInstance.post(`/staff-accounts/${userId}/review`, request)),
};
