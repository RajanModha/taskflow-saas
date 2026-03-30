import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function ProtectedRoute() {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="page-center" role="status">
        <div className="w-full max-w-xl space-y-3">
          <div className="skeleton h-6 w-40" />
          <div className="skeleton h-20 w-full" />
          <div className="skeleton h-20 w-full" />
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
    );
  }

  return <Outlet />;
}
