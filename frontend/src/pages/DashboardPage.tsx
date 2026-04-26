import { Link } from "react-router-dom";
import { Suspense, useEffect, useMemo, useState, lazy } from "react";
import { useAuth } from "../auth/AuthContext";
import { getDashboardStats, type DashboardStats } from "../api/dashboardApi";
import type { NormalizedApiError } from "../api/http";
// Charts are lazy-loaded to keep the first dashboard paint fast.
const DashboardCharts = lazy(() => import("./DashboardCharts").then((m) => ({ default: m.DashboardCharts })));

export function DashboardPage() {
  const { user } = useAuth();
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

  const userInitials = useMemo(() => {
    const source = user?.userName ?? user?.email ?? "DU";
    return source
      .split(/[\s@._-]+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase() ?? "")
      .join("");
  }, [user?.email, user?.userName]);

  return (
    <div className="stack dashboard-page">
      <header className="dashboard-page-head">
        <div>
          <h1 className="dashboard-title">Dashboard</h1>
          <p className="muted small">
            Signed in as <strong>{user?.email}</strong>
          </p>
        </div>
      </header>

      <div className="dashboard-grid">
        <section className="panel dashboard-main">
          <div className="dashboard-head">
            <div>
              <h2 className="mb-1 text-xl font-semibold tracking-tight text-slate-900">Analytics</h2>
              <p className="muted small">
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
            <div className="space-y-3">
              <div className="skeleton h-24 w-full" />
              <div className="grid grid-cols-1 gap-3 xl:grid-cols-2">
                <div className="skeleton h-72 w-full" />
                <div className="skeleton h-72 w-full" />
              </div>
            </div>
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

        <aside className="dashboard-side">
          <section className="panel">
            <div className="profile-head">
              <div className="profile-avatar">{userInitials || "DU"}</div>
              <div>
                <h2 className="profile-title">{user?.email}</h2>
                <p className="profile-subtitle">{user?.roles.length ? user.roles.join(", ") : "User"}</p>
              </div>
            </div>
            <div className="profile-divider" />
            <dl className="kv">
              <dt>Workspace</dt>
              <dd>{user?.organizationName || "—"}</dd>
              <dt>Join code</dt>
              <dd>{user?.organizationJoinCode || "—"}</dd>
              <dt>Role</dt>
              <dd>{user?.roles.length ? user.roles.join(", ") : "—"}</dd>
            </dl>
          </section>

          <section className="panel">
            <h2 className="text-lg font-semibold tracking-tight text-slate-900">Workspace actions</h2>
            <div className="dashboard-actions">
              <Link to="/workspaces/create" className="dashboard-action-btn">
                <span>+</span>
                <span>Create workspace</span>
              </Link>
              <Link to="/workspaces/join" className="dashboard-action-btn">
                <span>→</span>
                <span>Join workspace</span>
              </Link>
              <Link to="/projects" className="dashboard-action-btn">
                <span>▦</span>
                <span>Manage projects</span>
              </Link>
            </div>
          </section>
        </aside>
      </div>
    </div>
  );
}
