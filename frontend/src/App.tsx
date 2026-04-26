import { Link, NavLink, Route, Routes, useLocation } from "react-router-dom";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { DashboardPage } from "./pages/DashboardPage";
import { HomePage } from "./pages/HomePage";
import { CreateWorkspacePage } from "./pages/CreateWorkspacePage";
import { JoinWorkspacePage } from "./pages/JoinWorkspacePage";
import { ProjectsPage } from "./pages/ProjectsPage";
import { TasksPage } from "./pages/TasksPage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { VerifyEmailPage } from "./pages/VerifyEmailPage";
import { useAuth } from "./auth/AuthContext";

export default function App() {
  const location = useLocation();
  const { user, isAuthenticated, isLoading, logout } = useAuth();
  const path = location.pathname;

  const showAppNav =
    path === "/dashboard" ||
    path.startsWith("/workspaces/") ||
    path === "/workspaces" ||
    path.startsWith("/projects");

  const showLandingHeader = path === "/";
  const initials = (user?.userName ?? user?.email ?? "U")
    .split(/[\s@._-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? "")
    .join("");

  return (
    <div className="shell">
      {showAppNav ? (
        <nav className="top-nav">
          <div className="top-nav-left">
            <Link to={isAuthenticated ? "/dashboard" : "/"} className="brand-link">
              TaskFlow
            </Link>
          </div>

          <div className="top-nav-center">
            {isLoading ? (
              <div className="skeleton h-8 w-40" />
            ) : isAuthenticated ? (
              <div className="top-nav-tabs">
                <NavLink
                  to="/dashboard"
                  className={({ isActive }) => (isActive ? "top-tab active" : "top-tab")}
                >
                  Dashboard
                </NavLink>
                <NavLink
                  to="/projects"
                  className={({ isActive }) => (isActive ? "top-tab active" : "top-tab")}
                >
                  Projects
                </NavLink>
                <NavLink
                  to="/workspaces/create"
                  className={({ isActive }) => (isActive ? "top-tab active" : "top-tab")}
                >
                  Workspaces
                </NavLink>
              </div>
            ) : (
              <NavLink
                to="/login"
                className={({ isActive }) => (isActive ? "active" : undefined)}
              >
                Log in
              </NavLink>
            )}
          </div>

          <div className="top-nav-right">
            {isLoading ? (
              <div className="skeleton h-8 w-28" />
            ) : isAuthenticated ? (
              <>
                <div className="user-chip" title={user?.email}>
                  <span className="user-avatar">{initials || "U"}</span>
                  <span className="user-email">{user?.email}</span>
                </div>
              <button type="button" className="link-button" onClick={logout}>
                Log out
              </button>
              </>
            ) : null}
          </div>
        </nav>
      ) : null}

      {showLandingHeader ? (
        <div className="landing-header">
          <div className="landing-brand">TaskFlow</div>

          {isLoading ? (
            <div className="skeleton h-9 w-24" />
          ) : isAuthenticated ? (
            <div className="landing-actions">
              <Link to="/dashboard" className="button primary">
                Dashboard
              </Link>
            </div>
          ) : (
            <div className="landing-actions">
              <Link to="/login" className="button primary">
                Log in
              </Link>
            </div>
          )}
        </div>
      ) : null}
      <main className="content">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/verify-email" element={<VerifyEmailPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/workspaces/create" element={<CreateWorkspacePage />} />
            <Route path="/workspaces/join" element={<JoinWorkspacePage />} />
            <Route path="/projects" element={<ProjectsPage />} />
            <Route path="/projects/:projectId/tasks" element={<TasksPage />} />
          </Route>
        </Routes>
      </main>
    </div>
  );
}
