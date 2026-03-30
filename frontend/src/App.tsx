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
import { useAuth } from "./auth/AuthContext";

export default function App() {
  const location = useLocation();
  const { isAuthenticated, isLoading, logout } = useAuth();
  const path = location.pathname;

  const showAppNav =
    path === "/dashboard" ||
    path.startsWith("/workspaces/") ||
    path === "/workspaces" ||
    path.startsWith("/projects");

  const showLandingHeader = path === "/";

  return (
    <div className="shell">
      {showAppNav ? (
        <nav className="top-nav">
          <Link to={isAuthenticated ? "/dashboard" : "/"} className="brand-link">
            TaskFlow
          </Link>

          {isLoading ? (
            <div className="skeleton h-8 w-28" />
          ) : isAuthenticated ? (
            <>
              <NavLink
                to="/dashboard"
                className={({ isActive }) => (isActive ? "active" : undefined)}
              >
                Dashboard
              </NavLink>
              <button type="button" className="link-button" onClick={logout}>
                Log out
              </button>
            </>
          ) : (
            <NavLink
              to="/login"
              className={({ isActive }) => (isActive ? "active" : undefined)}
            >
              Log in
            </NavLink>
          )}
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
