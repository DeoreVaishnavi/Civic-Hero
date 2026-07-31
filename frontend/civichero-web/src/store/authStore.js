import { create } from 'zustand';
import {
  clearStoredAuthSession,
  isAccessTokenUsable,
  readStoredAuthSession,
  writeStoredAuthSession,
} from '../utils/authSessionStorage.js';

const restoredSession = readStoredAuthSession();

const initialState = {
  accessToken: restoredSession?.accessToken ?? null,
  expiresAtUtc: restoredSession?.expiresAtUtc ?? null,
  user: restoredSession?.user ?? null,
  isAuthenticated: isAccessTokenUsable(restoredSession, 0),
  isInitializing: true,
};

export const useAuthStore = create((set) => ({
  ...initialState,

  setSession: ({ accessToken, expiresAtUtc, user }) => {
    const session = { accessToken, expiresAtUtc, user };
    writeStoredAuthSession(session);
    set({
      ...session,
      isAuthenticated: Boolean(accessToken && user),
      isInitializing: false,
    });
  },

  updateUser: (user) => set((state) => {
    const nextState = { ...state, user };
    if (state.accessToken && state.expiresAtUtc && user) {
      writeStoredAuthSession({
        accessToken: state.accessToken,
        expiresAtUtc: state.expiresAtUtc,
        user,
      });
    }
    return nextState;
  }),

  clearSession: () => {
    clearStoredAuthSession();
    set({
      accessToken: null,
      expiresAtUtc: null,
      user: null,
      isAuthenticated: false,
      isInitializing: false,
    });
  },

  setInitializing: (isInitializing) => set({ isInitializing }),
}));
