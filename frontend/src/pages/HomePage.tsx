import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function HomePage() {
  const { isAuthenticated, user, logout } = useAuth();

  return (
    <div className="stack">
      <header className="row-between">
        <h1>TaskFlow</h1>
        <nav className="row gap">
          {isAuthenticated ? (
            <>
              <span className="muted small">
                {user?.email} · {user?.roles.join(", ") || "—"}
              </span>
              <Link to="/dashboard">Dashboard</Link>
              <button type="button" className="link-button" onClick={logout}>
                Log out
              </button>
            </>
          ) : (
            <>
              <Link to="/login">Log in</Link>
              <Link to="/register" className="button">
                Register
              </Link>
            </>
          )}
        </nav>
      </header>
      <p className="lead">
        Manage work in one place. Sign in to continue, or create an account to get started.
      </p>
      {!isAuthenticated ? (
        <p className="muted">
          Demo API: configure PostgreSQL and run the backend, then use the auth screens to obtain a JWT.
        </p>
      ) : null}
    </div>
  );
}
