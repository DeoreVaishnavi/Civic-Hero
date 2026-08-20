import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data;

export const verificationApi = {
  pending: async () => unwrap(await axiosInstance.get('/verification/pending')),
  queue: async (overdueOnly = false) => unwrap(await axiosInstance.get('/verification/queue', { params: { overdueOnly } })),
  history: async () => unwrap(await axiosInstance.get('/verification/history')),
  get: async (id) => unwrap(await axiosInstance.get(`/verification/${id}`)),
  geo: async (id, payload) => unwrap(await axiosInstance.post(`/verification/${id}/geo-check`, payload)),
  decide: async (id, payload) => unwrap(await axiosInstance.post(`/verification/${id}/decision`, payload)),
  amend: async (id, payload) => unwrap(await axiosInstance.put(`/verification/${id}/decision`, payload)),
  withdraw: async (id) => unwrap(await axiosInstance.delete(`/verification/${id}/decision`)),
  uploadEvidence: async (id, files) => {
    const formData = new FormData();
    files.forEach((file) => formData.append('Evidence', file));
    return unwrap(await axiosInstance.post(`/verification/${id}/evidence`, formData));
  },
  remind: async (id) => unwrap(await axiosInstance.post(`/verification/${id}/remind`)),
  supervisorDecision: async (id, payload) => unwrap(await axiosInstance.post(`/verification/${id}/supervisor-decision`, payload)),
};
