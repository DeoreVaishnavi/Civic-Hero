import axiosInstance from '../api/axiosInstance.js';

const unwrap = (response) => response.data?.data ?? response.data;

export const chatbotApi = {
  startSession: async () => unwrap(await axiosInstance.post('/chatbot/session')),
  getSession: async (sessionId) => unwrap(await axiosInstance.get(`/chatbot/session/${sessionId}`)),
  sendMessage: async (sessionId, message) => unwrap(await axiosInstance.post('/chatbot/message', { sessionId, message })),
  endSession: async (sessionId) => axiosInstance.delete(`/chatbot/session/${sessionId}`),
};
