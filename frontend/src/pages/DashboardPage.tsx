import { Link } from "react-router-dom";
import { Suspense, useEffect, useMemo, useState, lazy } from "react";
import { useAuth } from "../auth/AuthContext";
import { getDashboardStats, type DashboardStats } from "../api/dashboardApi";
import type { NormalizedApiError } from "../api/http";
const DashboardCharts = lazy(() => import("./DashboardCharts").then((m) => ({ default: m.DashboardCharts })));

export function DashboardPage() {
  const { user, logout } = useAuth();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [isLoadingStats, setIsLoadingStats] = useState(true);
  const [statsError, setStatsError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setIsLoadingStats(true);
        setStatsError(null);
        const data = await getDashboardStats();
        if (cancelled) return;
        setStats(data);
      } catch (e) {
        if (cancelled) return;
        const apiError = e as NormalizedApiError;
        setStatsError(apiError.detail ?? apiError.title ?? "Failed to load dashboard analytics.");
      } finally {
        if (!cancelled) setIsLoadingStats(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  const completionRate = useMemo(() => {
    if (!stats || stats.totalTasks === 0) return 0;
    return Math.round((stats.completedTasks / stats.totalTasks) * 100);
  }, [stats]);

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
        <div className="dashboard-head">
          <div>
            <h2 style={{ marginBottom: "0.2rem" }}>Analytics</h2>
            <p className="muted small" style={{ margin: 0 }}>
              Real-time task performance for your workspace.
            </p>
          </div>
          <div className="metric-chip">
            <span className="muted small">Completion rate</span>
            <strong>{completionRate}%</strong>
          </div>
        </div>

        {statsError ? <div className="error-banner">{statsError}</div> : null}

        {isLoadingStats ? (
          <div className="muted">Loading analytics…</div>
        ) : (
          <>
            <div className="metrics-grid">
              <article className="metric-card">
                <p>Total tasks</p>
                <h3>{stats?.totalTasks ?? 0}</h3>
              </article>
              <article className="metric-card success">
                <p>Completed</p>
                <h3>{stats?.completedTasks ?? 0}</h3>
              </article>
              <article className="metric-card warning">
                <p>Pending</p>
                <h3>{stats?.pendingTasks ?? 0}</h3>
              </article>
            </div>

            <Suspense fallback={<div className="muted">Loading charts…</div>}>
              {stats ? <DashboardCharts stats={stats} /> : null}
            </Suspense>
          </>
        )}
      </section>

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
          <Link to="/projects">Manage projects</Link>
        </div>
      </section>
    </div>
  );
}
