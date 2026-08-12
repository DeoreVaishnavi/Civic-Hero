import { create } from 'zustand';

const initialState = {
  accessToken: null,
  expiresAtUtc: null,
  user: null,
  isAuthenticated: false,
  isInitializing: true,
};

export const useAuthStore = create((set) => ({
  ...initialState,
  setSession: ({ accessToken, expiresAtUtc, user }) =>
    set({
      accessToken,
      expiresAtUtc,
      user,
      isAuthenticated: Boolean(accessToken && user),
      isInitializing: false,
    }),
  clearSession: () => set({ ...initialState, isInitializing: false }),
  setInitializing: (isInitializing) => set({ isInitializing }),
}));
