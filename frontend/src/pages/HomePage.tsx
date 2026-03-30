import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function HomePage() {
  const { isAuthenticated, user } = useAuth();

  return (
    <div className="stack py-10 sm:py-16">
      <div className="stack gap max-w-3xl">
        <span className="inline-flex w-fit items-center rounded-full border border-brand-200 bg-brand-50 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-brand-700">
          Modern SaaS Workspace
        </span>
        <h1 className="text-4xl font-black tracking-tight text-slate-900 sm:text-5xl">
          TaskFlow
        </h1>
        <p className="lead">Workspaces, roles, and data isolation - built for fast-moving teams.</p>

        {isAuthenticated ? (
          <div className="panel">
            <p className="muted small">
              Signed in as <strong>{user?.email}</strong>
            </p>
            <div className="row gap mt-4">
              <Link to="/dashboard" className="button primary">
                Go to Dashboard
              </Link>
            </div>
            {user?.organizationName ? (
              <p className="muted small mt-4">
                Workspace: <strong>{user.organizationName}</strong>
              </p>
            ) : null}
          </div>
        ) : (
          <div className="panel">
            <div className="row gap">
              <Link to="/login" className="button primary">
                Log in
              </Link>
              <Link to="/register" className="button">
                Create account
              </Link>
            </div>
            <p className="muted small mt-4">
              Need an account?{" "}
              <Link to="/register" className="font-semibold">
                Create one
              </Link>
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
