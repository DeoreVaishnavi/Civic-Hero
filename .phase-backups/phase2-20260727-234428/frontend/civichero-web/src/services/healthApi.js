import axiosInstance from '../api/axiosInstance.js';

export async function getApiHealth() {
  const response = await axiosInstance.get('/health');
  return response.data;
}

export async function getCorrelationHeaderCheck() {
  const response = await axiosInstance.get('/health/headers');
  return response.data;
}

export async function getInfrastructureHealth() {
  const response = await axiosInstance.get('/health/infrastructure');
  return response.data;
}

export async function getDatabaseHealth() {
  const response = await axiosInstance.get('/health/database');
  return response.data;
}

export async function getStorageHealth() {
  const response = await axiosInstance.get('/health/storage');
  return response.data;
}
