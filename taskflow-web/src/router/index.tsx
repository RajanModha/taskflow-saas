import { createBrowserRouter, Navigate, RouterProvider } from 'react-router-dom';
import AppLayout from '../components/layout/AppLayout';
import { ProtectedRoute } from './ProtectedRoute';
import ForgotPasswordPage from '../pages/auth/ForgotPasswordPage';
import LoginPage from '../pages/auth/LoginPage';
import RegisterPage from '../pages/auth/RegisterPage';
import ResetPasswordPage from '../pages/auth/ResetPasswordPage';
import VerifyEmailPage from '../pages/auth/VerifyEmailPage';
import VerifyEmailPendingPage from '../pages/auth/VerifyEmailPendingPage';
import DashboardPage from '../pages/DashboardPage';
import NotificationsPage from '../pages/NotificationsPage';
import ProjectsPage from '../pages/ProjectsPage';
import TeamPage from '../pages/TeamPage';
import MilestonesPage from '../pages/projects/MilestonesPage';
import ProjectActivityPage from '../pages/projects/ProjectActivityPage';
import ProjectDetailPage from '../pages/projects/ProjectDetailPage';
import TaskListPage from '../pages/projects/TaskListPage';
import ProfileSettingsPage from '../pages/settings/ProfileSettingsPage';
import SecuritySettingsPage from '../pages/settings/SecuritySettingsPage';
import SettingsTagsPage from '../pages/settings/SettingsTagsPage';
import SettingsTemplatesPage from '../pages/settings/SettingsTemplatesPage';
import SettingsWebhooksPage from '../pages/settings/SettingsWebhooksPage';
import SettingsWorkspacePage from '../pages/settings/SettingsWorkspacePage';
import CreateWorkspacePage from '../pages/workspace/CreateWorkspacePage';
import JoinWorkspacePage from '../pages/workspace/JoinWorkspacePage';
import SettingsLayout from '../layouts/SettingsLayout';
import KanbanBoardPage from '../pages/projects/KanbanBoardPage';

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  { path: '/register', element: <RegisterPage /> },
  { path: '/verify-email-pending', element: <VerifyEmailPendingPage /> },
  { path: '/verify-email', element: <VerifyEmailPage /> },
  { path: '/forgot-password', element: <ForgotPasswordPage /> },
  { path: '/reset-password', element: <ResetPasswordPage /> },
  { path: '/workspace/join', element: <JoinWorkspacePage /> },
  {
    element: <ProtectedRoute requireWorkspace={false} />,
    children: [{ path: '/workspace/create', element: <CreateWorkspacePage /> }],
  },
  {
    element: <ProtectedRoute requireWorkspace={true} />,
    children: [
      {
        element: <AppLayout />,
        children: [
          { path: '/dashboard', element: <DashboardPage /> },
          { path: '/projects', element: <ProjectsPage /> },
          { path: '/projects/:projectId', element: <ProjectDetailPage /> },
          { path: '/projects/:projectId/board', element: <KanbanBoardPage /> },
          { path: '/projects/:projectId/list', element: <TaskListPage /> },
          { path: '/projects/:projectId/activity', element: <ProjectActivityPage /> },
          { path: '/projects/:projectId/milestones', element: <MilestonesPage /> },
          { path: '/team', element: <TeamPage /> },
          { path: '/notifications', element: <NotificationsPage /> },
          {
            path: '/settings',
            element: <SettingsLayout />,
            children: [
              { index: true, element: <Navigate to="profile" replace /> },
              { path: 'profile', element: <ProfileSettingsPage /> },
              { path: 'security', element: <SecuritySettingsPage /> },
              { path: 'workspace', element: <SettingsWorkspacePage /> },
              { path: 'tags', element: <SettingsTagsPage /> },
              { path: 'webhooks', element: <SettingsWebhooksPage /> },
              { path: 'templates', element: <SettingsTemplatesPage /> },
            ],
          },
        ],
      },
    ],
  },
  { path: '*', element: <Navigate to="/login" replace /> },
]);

export function AppRouter() {
  return <RouterProvider router={router} />;
}
