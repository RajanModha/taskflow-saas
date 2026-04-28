import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthResponse, UserProfileResponse } from '../types/api';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserProfileResponse | null;
  isAuthenticated: boolean;
  setAuth: (auth: AuthResponse) => void;
  setUser: (user: UserProfileResponse) => void;
  logout: () => void;
  getRefreshToken: () => string | null;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      isAuthenticated: false,
      setAuth: (auth) =>
        set({
          accessToken: auth.accessToken,
          refreshToken: auth.refreshToken ?? null,
          isAuthenticated: true,
        }),
      setUser: (user) => set({ user }),
      logout: () => {
        set({ accessToken: null, refreshToken: null, user: null, isAuthenticated: false });
        window.location.href = '/login';
      },
      getRefreshToken: () => get().refreshToken,
    }),
    {
      name: 'taskflow-auth',
      partialize: (state) => ({ refreshToken: state.refreshToken }),
    },
  ),
);
