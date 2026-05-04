import { createBrowserRouter, Navigate } from 'react-router-dom';
import AppLayout from '../components/layout/AppLayout';
import { RouteErrorBoundary } from '../components/RouteErrorBoundary';
import { ProtectedRoute } from './ProtectedRoute';
import ForgotPasswordPage from '../pages/auth/ForgotPasswordPage';
import LoginPage from '../pages/auth/LoginPage';
import RegisterPage from '../pages/auth/RegisterPage';
import ResetPasswordPage from '../pages/auth/ResetPasswordPage';
import VerifyEmailPage from '../pages/auth/VerifyEmailPage';
import VerifyEmailPendingPage from '../pages/auth/VerifyEmailPendingPage';
import DashboardPage from '../pages/DashboardPage';
import NotificationsPage from '../pages/NotificationsPage';
import OverduePage from '../pages/OverduePage';
import ProjectsPage from '../pages/ProjectsPage';
import SearchPage from '../pages/SearchPage';
import TeamPage from '../pages/TeamPage';
import TrashPage from '../pages/TrashPage';
import { NotFoundPage } from '../pages/NotFoundPage';
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
  {
    errorElement: <RouteErrorBoundary />,
    children: [
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
            errorElement: <RouteErrorBoundary />,
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
              { path: '/overdue', element: <OverduePage /> },
              { path: '/search', element: <SearchPage /> },
              { path: '/trash', element: <TrashPage /> },
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
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
