import { http } from "./http";

export type TasksByStatus = {
  status: string;
  count: number;
};

export type DashboardStats = {
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  tasksByStatus: TasksByStatus[];
};

export async function getDashboardStats() {
  const { data } = await http.get<DashboardStats>("/api/dashboard/stats");
  return data;
}

