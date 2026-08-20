import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;
const paramsFor = (filters = {}) => Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== '' && value !== null && value !== undefined));

async function get(path, filters) {
  const response = await axiosInstance.get(`/analytics/${path}`, { params: paramsFor(filters) });
  return unwrap(response);
}

export const analyticsApi = {
  overview: (filters) => get('overview', filters),
  complaints: (filters) => get('complaints', filters),
  departments: (filters) => get('departments', filters),
  officers: (filters) => get('officers', filters),
  wards: (filters) => get('wards', filters),
  sla: (filters) => get('sla', filters),
  satisfaction: (filters) => get('satisfaction', filters),
  heatmap: (filters) => get('heatmap', filters),
  publicHeatmap: (filters) => get('public/heatmap', filters),
  async exportReport(report, format = 'csv', filters = {}) {
    const response = await axiosInstance.get('/analytics/export', {
      params: { report, format, ...paramsFor(filters) },
      responseType: 'blob',
    });
    const disposition = response.headers['content-disposition'] || '';
    const match = disposition.match(/filename\*?=(?:UTF-8''|\")?([^\";]+)/i);
    return { blob: response.data, fileName: decodeURIComponent(match?.[1] || `civichero-${report}.${format === 'excel' ? 'xlsx' : format}`) };
  },
};
