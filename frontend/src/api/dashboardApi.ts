import { http } from "./http";

export type TasksByStatus = {
  status: string;
  count: number;
};

export type TasksByPriority = {
  priority: string;
  count: number;
};

export type DashboardVelocity = {
  completedLast7Days: number;
  completedPrev7Days: number;
  trendPercent: number;
};

export type DashboardUpcomingTask = {
  id: string;
  title: string;
  projectId: string;
  projectName: string;
  dueDateUtc: string | null;
  priority: string;
  assignee: {
    id: string;
    userName: string;
    displayName: string | null;
  } | null;
};

export type DashboardRecentActivity = {
  action: string;
  actorName: string;
  occurredAt: string;
  entityTitle: string | null;
};

export type DashboardProjectSummary = {
  projectId: string;
  projectName: string;
  totalTasks: number;
  completedTasks: number;
  overdueCount: number;
  progress: number;
};

export type DashboardTopContributor = {
  userId: string;
  userName: string;
  displayName: string | null;
  tasksCompleted: number;
};

export type DashboardStats = {
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  tasksByStatus: TasksByStatus[];
  inProgressTasks: number;
  cancelledTasks: number;
  tasksByPriority: TasksByPriority[];
  overdueCount: number;
  dueSoonCount: number;
  completionRate: number;
  velocity: DashboardVelocity;
  upcomingTasks: DashboardUpcomingTask[];
  recentActivity: DashboardRecentActivity[];
  projectSummaries: DashboardProjectSummary[];
  topContributors: DashboardTopContributor[];
};

export type MyTasksSummary = {
  total: number;
  completed: number;
  overdue: number;
  dueSoon: number;
};

export type DashboardMyStats = {
  myTasks: MyTasksSummary;
  myTasksByStatus: TasksByStatus[];
  myTasksByPriority: TasksByPriority[];
  myRecentActivity: DashboardRecentActivity[];
};

export async function getDashboardStats() {
  const { data } = await http.get<DashboardStats>("/api/dashboard/stats");
  return data;
}

export async function getDashboardMyStats() {
  const { data } = await http.get<DashboardMyStats>("/api/dashboard/my-stats");
  return data;
}
