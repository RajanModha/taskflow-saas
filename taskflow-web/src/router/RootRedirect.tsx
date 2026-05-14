import { Navigate } from 'react-router-dom';
import SilentRefreshSpinner from '../components/auth/SilentRefreshSpinner';
import { useAuthStore } from '../stores/authStore';
import { userHasWorkspace } from './ProtectedRoute';

/** `/` → dashboard when signed in, otherwise login. */
export function RootRedirect() {
  const { isAuthenticated, user } = useAuthStore();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (!user) {
    return <SilentRefreshSpinner />;
  }

  if (!userHasWorkspace(user)) {
    return <Navigate to="/workspace/create" replace />;
  }

  return <Navigate to="/dashboard" replace />;
}
