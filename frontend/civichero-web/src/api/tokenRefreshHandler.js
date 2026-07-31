import axios from 'axios';
import { useAuthStore } from '../store/authStore.js';

let refreshPromise = null;

const wait = (milliseconds) => new Promise((resolve) => {
  globalThis.setTimeout(resolve, milliseconds);
});

function responseStatus(error) {
  return error?.response?.status ?? error?.status ?? 0;
}

export function isAuthenticationRefreshFailure(error) {
  const status = responseStatus(error);
  return status === 400 || status === 401 || status === 403;
}

export function isTransientRefreshFailure(error) {
  const status = responseStatus(error);
  return status === 0 || status === 408 || status === 429 || status >= 500 ||
    error?.code === 'ECONNABORTED' || error?.code === 'ERR_NETWORK';
}

async function requestRefreshToken({ retries, timeoutMs }) {
  const baseURL = import.meta.env.VITE_API_BASE_URL || '/api/v1';
  let lastError = null;

  for (let attempt = 0; attempt <= retries; attempt += 1) {
    try {
      const response = await axios.post(
        `${baseURL}/auth/refresh-token`,
        {},
        { withCredentials: true, timeout: timeoutMs },
      );

      const session = response.data?.data;
      if (!session?.accessToken || !session?.expiresAtUtc || !session?.user) {
        const invalidResponse = new Error('The refresh response is invalid.');
        invalidResponse.status = 401;
        throw invalidResponse;
      }

      useAuthStore.getState().setSession(session);
      return session.accessToken;
    } catch (error) {
      lastError = error;

      if (isAuthenticationRefreshFailure(error)) {
        useAuthStore.getState().clearSession();
        throw error;
      }

      const shouldRetry = isTransientRefreshFailure(error) && attempt < retries;
      if (!shouldRetry) throw error;

      await wait(600 * (attempt + 1));
    }
  }

  throw lastError ?? new Error('Unable to refresh the CivicHero session.');
}

export async function refreshAccessToken(options = {}) {
  if (!refreshPromise) {
    const retries = Number.isInteger(options.retries) ? Math.max(0, options.retries) : 2;
    const timeoutMs = Number.isFinite(options.timeoutMs) ? Math.max(5_000, options.timeoutMs) : 30_000;

    refreshPromise = requestRefreshToken({ retries, timeoutMs })
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}
