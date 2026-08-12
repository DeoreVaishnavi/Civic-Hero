import axiosInstance from '../api/axiosInstance.js';

export async function getApiHealth() {
  const response = await axiosInstance.get('/health');
  return response.data;
}

export async function getCorrelationHeaderCheck() {
  const response = await axiosInstance.get('/health/headers');
  return response.data;
}

export async function getDependencyHealth() {
  const response = await axiosInstance.get('/health/dependencies', {
    validateStatus: (status) => status === 200 || status === 503,
  });

  return response.data;
}
