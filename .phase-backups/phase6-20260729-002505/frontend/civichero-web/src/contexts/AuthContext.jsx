import { createContext, useContext, useEffect, useMemo } from 'react';
import { refreshAccessToken } from '../api/tokenRefreshHandler.js';
import { authApi } from '../services/authApi.js';
import { userApi } from '../services/userApi.js';
import { useAuthStore } from '../store/authStore.js';

const AuthContext = createContext(null);

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
      try {
        await refreshAccessToken();
        if (active) {
          const profile = await userApi.getProfile();
          useAuthStore.getState().updateUser(profile);
        }
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
    verifyEmail: async (request) => hydrateProfile(await authApi.verifyEmail(request)),
    login: async (request) => hydrateProfile(await authApi.login(request)),
    refreshUser: async () => {
      const user = await userApi.getProfile();
      useAuthStore.getState().updateUser(user);
      return user;
    },
    updateCurrentUser: (user) => useAuthStore.getState().updateUser(user),
    logout: async () => {
      try { await authApi.logout(); }
      finally { useAuthStore.getState().clearSession(); }
    },
  }), [authState]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error('useAuth must be used inside AuthProvider.');
  return value;
}
