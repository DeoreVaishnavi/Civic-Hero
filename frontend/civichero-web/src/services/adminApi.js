import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;
export const getAdminOverview = async () => unwrap(await axiosInstance.get('/admin/overview'));
export const getCategories = async () => unwrap(await axiosInstance.get('/admin/categories'));
export const saveCategory = async (id, payload) => unwrap(await axiosInstance[id ? 'put' : 'post'](id ? `/admin/categories/${id}` : '/admin/categories', payload));
export const setCategoryActive = async (id, active) => unwrap(await axiosInstance[active ? 'post' : 'delete'](`/admin/categories/${id}${active ? '/activate' : ''}`));
export const getDepartments = async () => unwrap(await axiosInstance.get('/admin/departments'));
export const saveDepartment = async (id, payload) => unwrap(await axiosInstance[id ? 'put' : 'post'](id ? `/admin/departments/${id}` : '/admin/departments', payload));
export const setDepartmentActive = async (id, active) => unwrap(await axiosInstance[active ? 'post' : 'delete'](`/admin/departments/${id}${active ? '/activate' : ''}`));
export const getWards = async (departmentId) => unwrap(await axiosInstance.get('/admin/wards', { params: departmentId ? { departmentId } : {} }));
export const saveWard = async (id, payload) => unwrap(await axiosInstance[id ? 'put' : 'post'](id ? `/admin/wards/${id}` : '/admin/wards', payload));
export const setWardActive = async (id, active) => unwrap(await axiosInstance[active ? 'post' : 'delete'](`/admin/wards/${id}${active ? '/activate' : ''}`));
export const getSettings = async () => unwrap(await axiosInstance.get('/admin/settings'));
export const updateSetting = async (key, payload) => unwrap(await axiosInstance.put(`/admin/settings/${encodeURIComponent(key)}`, payload));
export const getAuditLogs = async (params) => unwrap(await axiosInstance.get('/admin/audit-logs', { params }));
export const exportAuditLogs = async (format = 'csv', params = {}) => {
  const response = await axiosInstance.get('/admin/audit-logs/export', { params: { format, ...params }, responseType: 'blob' });
  const disposition = response.headers['content-disposition'] || '';
  const match = disposition.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i);
  return { blob: response.data, fileName: decodeURIComponent(match?.[1] || `civichero-audit.${format}`) };
};
export const getSystemHealth = async () => unwrap(await axiosInstance.get('/admin/system-health'));
export const runMaintenance = async ({ dryRun = true, retentionDays = 365 } = {}) => unwrap(await axiosInstance.post('/admin/maintenance/cleanup', null, { params: { dryRun, retentionDays } }));
