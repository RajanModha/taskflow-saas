import axios from 'axios';
import { useEffect, useState } from 'react';
import type { AuthResponse, UserProfileResponse } from '../types/api';
import { useAuthStore } from '../stores/authStore';

export function AppInitializer({ children }: { children: React.ReactNode }) {
  const [ready, setReady] = useState(false);
  const { isAuthenticated, refreshToken: storedRT, setAuth, setUser, logout } = useAuthStore();

  useEffect(() => {
    async function init() {
      if (isAuthenticated) {
        setReady(true);
        return;
      }

      if (storedRT) {
        try {
          const authRes = await axios.post<AuthResponse>(
            `${import.meta.env.VITE_API_URL ?? 'http://localhost:5005/api/v1'}/Auth/refresh`,
            { refreshToken: storedRT },
          );
          setAuth(authRes.data);

          const profileRes = await axios.get<UserProfileResponse>(
            `${import.meta.env.VITE_API_URL ?? 'http://localhost:5005/api/v1'}/Auth/me`,
            { headers: { Authorization: `Bearer ${authRes.data.accessToken}` } },
          );
          setUser(profileRes.data);
        } catch {
          logout();
        }
      }
      setReady(true);
    }

    init();
  }, [isAuthenticated, storedRT, setAuth, setUser, logout]);

  if (!ready) return <SilentRefreshSpinner />;
  return <>{children}</>;
}

function SilentRefreshSpinner() {
  return (
    <div className="fixed inset-0 flex items-center justify-center bg-surface-page">
      <div className="flex flex-col items-center gap-3">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-primary-200 border-t-primary-600" />
        <p className="text-12 text-neutral-400">Loading...</p>
      </div>
    </div>
  );
}
