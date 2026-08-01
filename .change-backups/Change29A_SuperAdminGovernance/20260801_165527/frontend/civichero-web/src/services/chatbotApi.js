import axiosInstance from '../api/axiosInstance.js';
import { refreshAccessToken } from '../api/tokenRefreshHandler.js';
import { useAuthStore } from '../store/authStore.js';

const unwrap = (response) => response.data?.data ?? response.data;
const apiBase = (import.meta.env.VITE_API_BASE_URL || '/api/v1').replace(/\/$/, '');

async function openStream(sessionId, message, accessToken) {
  return fetch(`${apiBase}/chatbot/message/stream`, {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/x-ndjson',
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
    },
    body: JSON.stringify({ sessionId, message }),
  });
}

async function streamMessage(sessionId, message, handlers = {}) {
  let response = await openStream(sessionId, message, useAuthStore.getState().accessToken);
  if (response.status === 401) {
    const refreshedToken = await refreshAccessToken();
    response = await openStream(sessionId, message, refreshedToken);
  }

  if (!response.ok) {
    let payload = null;
    try { payload = await response.json(); } catch { /* NDJSON or empty response */ }
    throw new Error(payload?.message || `Chat streaming failed with status ${response.status}.`);
  }
  if (!response.body) throw new Error('This browser does not support streaming responses.');

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  const processLine = (line) => {
    if (!line.trim()) return;
    const event = JSON.parse(line);
    if (event.type === 'user') handlers.onUser?.(event.data);
    else if (event.type === 'meta') handlers.onMeta?.(event.data);
    else if (event.type === 'token') handlers.onToken?.(event.data?.value || '');
    else if (event.type === 'done') handlers.onDone?.(event.data);
  };

  while (true) {
    const { done, value } = await reader.read();
    buffer += decoder.decode(value || new Uint8Array(), { stream: !done });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';
    lines.forEach(processLine);
    if (done) break;
  }
  if (buffer.trim()) processLine(buffer);
}

export const chatbotApi = {
  startSession: async () => unwrap(await axiosInstance.post('/chatbot/session')),
  listSessions: async ({ search = '', take = 30 } = {}) => unwrap(await axiosInstance.get('/chatbot/sessions', { params: { search: search || undefined, take } })),
  getSession: async (sessionId) => unwrap(await axiosInstance.get(`/chatbot/session/${sessionId}`)),
  renameSession: async (sessionId, title) => unwrap(await axiosInstance.put(`/chatbot/session/${sessionId}/title`, { title })),
  continueSession: async (sessionId) => unwrap(await axiosInstance.post(`/chatbot/session/${sessionId}/continue`)),
  sendMessage: async (sessionId, message) => unwrap(await axiosInstance.post('/chatbot/message', { sessionId, message })),
  streamMessage,
  submitFeedback: async (sessionId, messageId, helpful, comment = null) => unwrap(await axiosInstance.post(`/chatbot/session/${sessionId}/messages/${messageId}/feedback`, { helpful, comment })),
  endSession: async (sessionId) => axiosInstance.delete(`/chatbot/session/${sessionId}`),
  deleteSession: async (sessionId) => axiosInstance.delete(`/chatbot/sessions/${sessionId}`),
};
