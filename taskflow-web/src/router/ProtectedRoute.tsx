import { useEffect, useState } from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import SilentRefreshSpinner from '../components/auth/SilentRefreshSpinner';
import api from '../lib/api';
import { useAuthStore } from '../stores/authStore';
import type { AuthResponse, UserProfileResponse } from '../types/api';

interface ProtectedRouteProps {
  requireWorkspace: boolean;
}

const EMPTY_ORG_ID = '00000000-0000-0000-0000-000000000000';

export function ProtectedRoute({ requireWorkspace }: ProtectedRouteProps) {
  const { isAuthenticated, user, getRefreshToken } = useAuthStore();
  const [checking, setChecking] = useState(true);

  useEffect(() => {
    const refreshToken = getRefreshToken();

    if (!isAuthenticated && refreshToken) {
      api
        .post<AuthResponse>('/Auth/refresh', { refreshToken })
        .then((refreshResponse) => {
          useAuthStore.getState().setAuth(refreshResponse.data);
          return api.get<UserProfileResponse>('/Auth/me');
        })
        .then((meResponse) => {
          useAuthStore.getState().setUser(meResponse.data);
          setChecking(false);
        })
        .catch(() => {
          useAuthStore.getState().logout();
        });
      return;
    }

    setChecking(false);
  }, [getRefreshToken, isAuthenticated]);

  if (checking) {
    return <SilentRefreshSpinner />;
  }

  if (!useAuthStore.getState().isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  const currentUser = useAuthStore.getState().user ?? user;
  const hasWorkspace = Boolean(currentUser?.organizationId && currentUser.organizationId !== EMPTY_ORG_ID);

  if (requireWorkspace && !hasWorkspace) {
    return <Navigate to="/workspace/create" replace />;
  }

  if (!requireWorkspace && hasWorkspace) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
}
