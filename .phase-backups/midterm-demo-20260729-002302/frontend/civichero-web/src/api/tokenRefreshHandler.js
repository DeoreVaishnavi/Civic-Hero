import axios from 'axios';
import { useAuthStore } from '../store/authStore.js';

let refreshPromise = null;

export async function refreshAccessToken() {
  if (!refreshPromise) {
    const baseURL = import.meta.env.VITE_API_BASE_URL || '/api/v1';
    refreshPromise = axios
      .post(`${baseURL}/auth/refresh-token`, {}, { withCredentials: true, timeout: 10000 })
      .then((response) => {
        const session = response.data?.data;
        if (!session?.accessToken || !session?.user) {
          throw new Error('The refresh response is invalid.');
        }
        useAuthStore.getState().setSession(session);
        return session.accessToken;
      })
      .catch((error) => {
        useAuthStore.getState().clearSession();
        throw error;
      })
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}
