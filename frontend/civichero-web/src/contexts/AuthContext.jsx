import { createContext, useContext, useEffect, useMemo } from 'react';
import {
  isAuthenticationRefreshFailure,
  isTransientRefreshFailure,
  refreshAccessToken,
} from '../api/tokenRefreshHandler.js';
import { stopNotificationConnection } from '../services/signalRService.js';
import { useNotificationStore } from '../store/notificationStore.js';
import { authApi } from '../services/authApi.js';
import { userApi } from '../services/userApi.js';
import { useAuthStore } from '../store/authStore.js';
import { isAccessTokenUsable } from '../utils/authSessionStorage.js';

const AuthContext = createContext(null);
const RENEW_BEFORE_EXPIRY_MS = 60_000;

function isTransientApiFailure(error) {
  const status = error?.status ?? error?.originalError?.response?.status ?? 0;
  return status === 0 || status === 408 || status === 429 || status >= 500 ||
    isTransientRefreshFailure(error?.originalError ?? error);
}

function isAuthenticationFailure(error) {
  const status = error?.status ?? error?.originalError?.response?.status ?? 0;
  return status === 400 || status === 401 || status === 403 ||
    isAuthenticationRefreshFailure(error?.originalError ?? error);
}

async function hydrateProfile(session) {
  useAuthStore.getState().setSession(session);
  try {
    const profile = await userApi.getProfile();
    useAuthStore.getState().updateUser(profile);
    return { ...session, user: profile };
  } catch {
    return session;
  }
}

export function AuthProvider({ children }) {
  const authState = useAuthStore();

  useEffect(() => {
    let active = true;

    const initialize = async () => {
      const storedState = useAuthStore.getState();

      try {
        if (isAccessTokenUsable(storedState)) {
          try {
            const profile = await userApi.getProfile();
            if (!active) return;
            useAuthStore.getState().updateUser(profile);
            useAuthStore.getState().setInitializing(false);
            return;
          } catch (error) {
            if (!active) return;

            // A temporary backend/network failure must not erase a still-valid browser session.
            if (isTransientApiFailure(error) && isAccessTokenUsable(useAuthStore.getState(), 0)) {
              useAuthStore.getState().setInitializing(false);
              return;
            }
          }
        }

        await refreshAccessToken({ retries: 2, timeoutMs: 30_000 });
        if (!active) return;

        const profile = await userApi.getProfile();
        if (!active) return;

        useAuthStore.getState().updateUser(profile);
        useAuthStore.getState().setInitializing(false);
      } catch (error) {
        if (!active) return;

        const current = useAuthStore.getState();
        if (isTransientApiFailure(error) && isAccessTokenUsable(current, 0)) {
          current.setInitializing(false);
          return;
        }

        current.clearSession();
      }
    };

    initialize();
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (!authState.isAuthenticated || !authState.expiresAtUtc) return undefined;

    const expiresAt = Date.parse(authState.expiresAtUtc);
    if (!Number.isFinite(expiresAt)) return undefined;

    let cancelled = false;
    let timerId = null;

    const renew = async () => {
      try {
        await refreshAccessToken({ retries: 2, timeoutMs: 30_000 });
      } catch (error) {
        if (cancelled) return;

        // Only an explicit authentication rejection ends the session. Network outages do not.
        if (isAuthenticationFailure(error)) {
          useAuthStore.getState().clearSession();
        }
      }
    };

    const delay = Math.max(0, expiresAt - Date.now() - RENEW_BEFORE_EXPIRY_MS);
    timerId = globalThis.setTimeout(renew, delay);

    return () => {
      cancelled = true;
      if (timerId) globalThis.clearTimeout(timerId);
    };
  }, [authState.accessToken, authState.expiresAtUtc, authState.isAuthenticated]);

  const value = useMemo(() => ({
    ...authState,
    register: authApi.register,
    verifyEmail: async (request) => hydrateProfile(await authApi.verifyEmail(request)),
    login: async (request) => hydrateProfile(await authApi.login(request)),
    phoneLogin: async (request) => hydrateProfile(await authApi.phoneLogin(request)),
    requestPhoneLoginOtp: authApi.requestPhoneLoginOtp,
    requestPhoneVerification: authApi.requestPhoneVerification,
    verifyPhone: async (request) => {
      const result = await authApi.verifyPhone(request);
      const profile = await userApi.getProfile();
      useAuthStore.getState().updateUser(profile);
      return result;
    },
    refreshUser: async () => {
      const user = await userApi.getProfile();
      useAuthStore.getState().updateUser(user);
      return user;
    },
    updateCurrentUser: (user) => useAuthStore.getState().updateUser(user),
    logout: async () => {
      try {
        await authApi.logout();
      } finally {
        await stopNotificationConnection();
        useNotificationStore.getState().reset();
        useAuthStore.getState().clearSession();
      }
    },
  }), [authState]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error('useAuth must be used inside AuthProvider.');
  return value;
}
