import { NavLink, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { DashboardPage } from "./pages/DashboardPage";
import { HomePage } from "./pages/HomePage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";

export default function App() {
  return (
    <div className="shell">
      <nav className="top-nav">
        <NavLink to="/" end className={({ isActive }) => (isActive ? "active" : undefined)}>
          Home
        </NavLink>
        <NavLink to="/login" className={({ isActive }) => (isActive ? "active" : undefined)}>
          Log in
        </NavLink>
        <NavLink to="/register" className={({ isActive }) => (isActive ? "active" : undefined)}>
          Register
        </NavLink>
        <NavLink to="/dashboard" className={({ isActive }) => (isActive ? "active" : undefined)}>
          Dashboard
        </NavLink>
      </nav>
      <main className="content">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route element={<ProtectedRoute />}>
            <Route path="/dashboard" element={<DashboardPage />} />
          </Route>
        </Routes>
      </main>
    </div>
  );
}
