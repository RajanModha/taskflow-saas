import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function HomePage() {
  const { isAuthenticated, user } = useAuth();

  return (
    <div className="stack">
      <div className="stack gap">
        <h1>TaskFlow</h1>
        <p className="lead">Workspaces, roles, and data isolation — built for teams.</p>

        {isAuthenticated ? (
          <div className="panel">
            <p className="muted small" style={{ margin: 0 }}>
              Signed in as <strong>{user?.email}</strong>
            </p>
            <div className="row gap" style={{ marginTop: "0.9rem" }}>
              <Link to="/dashboard" className="button primary">
                Go to Dashboard
              </Link>
            </div>
            {user?.organizationName ? (
              <p className="muted small" style={{ margin: "0.9rem 0 0" }}>
                Workspace: <strong>{user.organizationName}</strong>
              </p>
            ) : null}
          </div>
        ) : (
          <div className="panel">
            <div className="row gap" style={{ marginTop: "0.2rem" }}>
              <Link to="/login" className="button primary">
                Log in
              </Link>
            </div>
            <p className="muted small" style={{ margin: "0.85rem 0 0" }}>
              Need an account?{" "}
              <Link to="/register" style={{ fontWeight: 700 }}>
                Create one
              </Link>
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
