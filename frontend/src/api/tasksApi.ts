import type { PagedResult, Task, TaskPriority, TaskStatus } from "./types";
import { http } from "./http";

export async function getTasks(params: {
  page: number;
  pageSize: number;
  projectId?: string;
  status?: TaskStatus | null;
  priority?: TaskPriority | null;
  dueFromUtc?: string | null;
  dueToUtc?: string | null;
  q?: string;
  sortBy?: string | null;
  sortDir?: "asc" | "desc";
}) {
  const { data } = await http.get<PagedResult<Task>>("/api/tasks", {
    params: {
      page: params.page,
      pageSize: params.pageSize,
      projectId: params.projectId,
      status: params.status === null || params.status === undefined ? undefined : params.status.toString(),
      priority:
        params.priority === null || params.priority === undefined
          ? undefined
          : params.priority.toString(),
      dueFromUtc: params.dueFromUtc ?? undefined,
      dueToUtc: params.dueToUtc ?? undefined,
      q: params.q ?? undefined,
      sortBy: params.sortBy ?? undefined,
      sortDir: params.sortDir,
    },
  });
  return data;
}

export async function getTaskById(taskId: string) {
  const { data } = await http.get<Task>(`/api/tasks/${taskId}`);
  return data;
}

export async function createTask(input: {
  projectId: string;
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDateUtc?: string | null;
}) {
  const { data } = await http.post<Task>("/api/tasks", {
    projectId: input.projectId,
    title: input.title,
    description: input.description ?? null,
    status: input.status,
    priority: input.priority,
    dueDateUtc: input.dueDateUtc ?? null,
  });
  return data;
}

export async function updateTask(input: {
  taskId: string;
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDateUtc?: string | null;
}) {
  const { data } = await http.put<Task>(`/api/tasks/${input.taskId}`, {
    title: input.title,
    description: input.description ?? null,
    status: input.status,
    priority: input.priority,
    dueDateUtc: input.dueDateUtc ?? null,
  });
  return data;
}

export async function deleteTask(taskId: string) {
  await http.delete(`/api/tasks/${taskId}`);
}

