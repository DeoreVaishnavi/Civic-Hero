import axiosInstance from '../api/axiosInstance.js';

export async function getApiHealth() {
  const response = await axiosInstance.get('/health');
  return response.data;
}

export async function getCorrelationHeaderCheck() {
  const response = await axiosInstance.get('/health/headers');
  return response.data;
}
