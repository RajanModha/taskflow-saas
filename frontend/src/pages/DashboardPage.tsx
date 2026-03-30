import { Link } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";

export function DashboardPage() {
  const { user, logout } = useAuth();

  return (
    <div className="stack">
      <header className="row-between">
        <div>
          <h1>Dashboard</h1>
          <p className="muted small">
            Signed in as <strong>{user?.email}</strong>
          </p>
        </div>
        <div className="row gap">
          <Link to="/">Home</Link>
          <button type="button" className="link-button" onClick={logout}>
            Log out
          </button>
        </div>
      </header>
      <section className="panel">
        <h2>Profile</h2>
        <dl className="kv">
          <dt>User name</dt>
          <dd>{user?.userName}</dd>
          <dt>Workspace</dt>
          <dd>{user?.organizationName || "—"}</dd>
          <dt>Join code</dt>
          <dd>{user?.organizationJoinCode || "—"}</dd>
          <dt>Roles</dt>
          <dd>{user?.roles.length ? user.roles.join(", ") : "—"}</dd>
        </dl>
      </section>

      <section className="panel">
        <h2>Workspace actions</h2>
        <div className="row gap">
          <Link to="/workspaces/create">Create workspace</Link>
          <Link to="/workspaces/join">Join workspace</Link>
        </div>
      </section>
    </div>
  );
}
