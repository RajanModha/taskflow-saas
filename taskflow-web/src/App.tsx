import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import AppLayout from './components/layout/AppLayout';
import SettingsLayout from './components/settings/SettingsLayout';
import DashboardPage from './pages/DashboardPage';
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage';
import LoginPage from './pages/auth/LoginPage';
import RegisterPage from './pages/auth/RegisterPage';
import ResetPasswordPage from './pages/auth/ResetPasswordPage';
import VerifyEmailPage from './pages/auth/VerifyEmailPage';
import VerifyEmailPendingPage from './pages/auth/VerifyEmailPendingPage';
import NotificationsPage from './pages/NotificationsPage';
import ProjectsPage from './pages/ProjectsPage';
import TaskListPage from './pages/projects/TaskListPage';
import SettingsPage from './pages/SettingsPage';
import SettingsProfilePage from './pages/settings/SettingsProfilePage';
import SettingsSecurityPage from './pages/settings/SettingsSecurityPage';
import SettingsWorkspacePage from './pages/settings/SettingsWorkspacePage';
import SettingsTagsPage from './pages/settings/SettingsTagsPage';
import SettingsWebhooksPage from './pages/settings/SettingsWebhooksPage';
import SettingsTemplatesPage from './pages/settings/SettingsTemplatesPage';
import TeamPage from './pages/TeamPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route path="verify-email-pending" element={<VerifyEmailPendingPage />} />
        <Route path="verify-email" element={<VerifyEmailPage />} />
        <Route path="forgot-password" element={<ForgotPasswordPage />} />
        <Route path="reset-password" element={<ResetPasswordPage />} />

        <Route element={<AppLayout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="projects" element={<ProjectsPage />} />
          <Route path="projects/:id/list" element={<TaskListPage />} />
          <Route path="projects/:id/board" element={<Navigate to="../list" replace />} />
          <Route path="team" element={<TeamPage />} />
          <Route path="notifications" element={<NotificationsPage />} />
          <Route path="settings" element={<SettingsLayout />}>
            <Route index element={<SettingsPage />} />
            <Route path="profile" element={<SettingsProfilePage />} />
            <Route path="security" element={<SettingsSecurityPage />} />
            <Route path="workspace" element={<SettingsWorkspacePage />} />
            <Route path="tags" element={<SettingsTagsPage />} />
            <Route path="webhooks" element={<SettingsWebhooksPage />} />
            <Route path="templates" element={<SettingsTemplatesPage />} />
          </Route>
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
