import axiosInstance from '../api/axiosInstance.js';

const data = (response) => response.data?.data;

export const userApi = {
  getProfile: async () => data(await axiosInstance.get('/users/profile')),
  updateProfile: async (request) => data(await axiosInstance.put('/users/profile', request)),
  changeEmail: async (request) => data(await axiosInstance.post('/users/profile/email-change', request)),
  getAvatar: async (cacheKey = Date.now()) => (await axiosInstance.get('/users/profile/avatar', {
    responseType: 'blob',
    params: { v: cacheKey },
    headers: { 'Cache-Control': 'no-cache', Pragma: 'no-cache' },
  })).data,
  uploadAvatar: async (file) => {
    const form = new FormData();
    form.append('file', file);
    return data(await axiosInstance.post('/users/profile/avatar', form));
  },
  deleteAvatar: async () => data(await axiosInstance.delete('/users/profile/avatar')),
  getUsers: async (params) => data(await axiosInstance.get('/users', { params })),
  getUser: async (id) => data(await axiosInstance.get(`/users/${id}`)),
  getMetadata: async () => data(await axiosInstance.get('/users/metadata')),
  changeRole: async (id, request) => data(await axiosInstance.post(`/users/${id}/role`, request)),
  assignDepartment: async (id, departmentId) => data(await axiosInstance.post(`/users/${id}/department`, { departmentId })),
  assignWard: async (id, wardId) => data(await axiosInstance.post(`/users/${id}/ward`, { wardId })),
  deactivate: async (id) => data(await axiosInstance.delete(`/users/${id}`)),
  activate: async (id) => data(await axiosInstance.post(`/users/${id}/activate`)),
  forceLogout: async (id) => axiosInstance.post(`/users/${id}/force-logout`),
};
