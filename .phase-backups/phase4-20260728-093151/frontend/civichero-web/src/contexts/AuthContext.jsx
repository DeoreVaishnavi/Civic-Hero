import { createContext, useContext, useEffect, useMemo } from 'react';
import { refreshAccessToken } from '../api/tokenRefreshHandler.js';
import { authApi } from '../services/authApi.js';
import { useAuthStore } from '../store/authStore.js';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const authState = useAuthStore();

  useEffect(() => {
    let active = true;
    const initialize = async () => {
      try {
        await refreshAccessToken();
      } catch {
        if (active) useAuthStore.getState().clearSession();
      }
    };
    initialize();
    return () => { active = false; };
  }, []);

  const value = useMemo(() => ({
    ...authState,
    register: authApi.register,
    verifyEmail: async (request) => {
      const session = await authApi.verifyEmail(request);
      useAuthStore.getState().setSession(session);
      return session;
    },
    login: async (request) => {
      const session = await authApi.login(request);
      useAuthStore.getState().setSession(session);
      return session;
    },
    logout: async () => {
      try {
        await authApi.logout();
      } finally {
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
