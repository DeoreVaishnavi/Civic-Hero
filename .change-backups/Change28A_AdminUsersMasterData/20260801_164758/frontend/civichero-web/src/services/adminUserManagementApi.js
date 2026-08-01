import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const adminUserManagementApi = {
  users: async (params = {}) => unwrap(await axiosInstance.get('/admin/user-management/users', { params })),
  createCitizen: async (payload) => unwrap(await axiosInstance.post('/admin/user-management/citizens', payload)),
  updateEmail: async (userId, payload) => unwrap(await axiosInstance.put(`/admin/user-management/users/${userId}/email`, payload)),
  softDelete: async (userId, payload) => unwrap(await axiosInstance.delete(`/admin/user-management/users/${userId}`, { data: payload })),
  restore: async (userId, payload) => unwrap(await axiosInstance.post(`/admin/user-management/users/${userId}/restore`, payload)),
  history: async (userId, params = {}) => unwrap(await axiosInstance.get(`/admin/user-management/users/${userId}/history`, { params })),
  roleHistory: async (userId, params = {}) => unwrap(await axiosInstance.get(`/admin/user-management/users/${userId}/role-history`, { params })),
  departmentHeads: async () => unwrap(await axiosInstance.get('/admin/user-management/department-heads')),
  assignDepartmentHead: async (departmentId, payload) => unwrap(await axiosInstance.put(`/admin/user-management/department-heads/${departmentId}`, payload)),
  removeDepartmentHead: async (departmentId, payload) => unwrap(await axiosInstance.delete(`/admin/user-management/department-heads/${departmentId}`, { data: payload })),
  masterData: async () => unwrap(await axiosInstance.get('/admin/user-management/master-data')),
  saveMasterData: async (payload) => unwrap(await axiosInstance.put('/admin/user-management/master-data', payload)),
};
