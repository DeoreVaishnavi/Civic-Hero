import axiosInstance from '../api/axiosInstance.js';
import { assignmentApi } from './assignmentApi.js';

const unwrap = (response) => response.data?.data;

export const adminComplaintApi = {
  list: async (params = {}) => unwrap(await axiosInstance.get('/admin/complaints', { params })),
  get: async (id) => unwrap(await axiosInstance.get(`/admin/complaints/${id}`)),
  duplicateClusters: async (includeArchived = false) => unwrap(await axiosInstance.get('/admin/complaints/duplicate-clusters', { params: { includeArchived } })),
  changePriority: async (id, priority, reason) => unwrap(await axiosInstance.put(`/admin/complaints/${id}/priority`, { priority, reason })),
  correctRouting: async (id, request) => unwrap(await axiosInstance.put(`/admin/complaints/${id}/routing`, request)),
  overrideAssignment: async (id, officerId, reason) => unwrap(await axiosInstance.post(`/admin/complaints/${id}/assignment-override`, { officerId, reason })),
  close: async (id, reason) => unwrap(await axiosInstance.post(`/admin/complaints/${id}/close`, { reason })),
  reopen: async (id, reason) => unwrap(await axiosInstance.post(`/admin/complaints/${id}/reopen`, { reason })),
  archive: async (id, reason) => unwrap(await axiosInstance.post(`/admin/complaints/${id}/archive`, { reason })),
  restore: async (id, reason) => unwrap(await axiosInstance.post(`/admin/complaints/${id}/restore`, { reason })),
  linkDuplicate: async (id, canonicalComplaintId, reason) => unwrap(await axiosInstance.post(`/admin/complaints/${id}/link-duplicate`, { canonicalComplaintId, reason })),
  merge: async (id, canonicalComplaintId, reason) => unwrap(await axiosInstance.post(`/admin/complaints/${id}/merge`, { canonicalComplaintId, reason })),
  removeMedia: async (id, mediaId, reason) => unwrap(await axiosInstance.post(`/admin/complaints/${id}/media/${mediaId}/remove`, { reason })),
  eligibleOfficers: assignmentApi.eligibleOfficers,
};
