import type { PagedResult, Project } from "./types";
import { http } from "./http";

export async function getProjects(params: {
  page: number;
  pageSize: number;
  q?: string;
  sortBy?: string;
  sortDir?: "asc" | "desc";
}) {
  const { data } = await http.get<PagedResult<Project>>("/api/projects", {
    params: {
      page: params.page,
      pageSize: params.pageSize,
      q: params.q,
      sortBy: params.sortBy,
      sortDir: params.sortDir,
    },
  });
  return data;
}

export async function getProjectById(projectId: string) {
  const { data } = await http.get<Project>(`/api/projects/${projectId}`);
  return data;
}

export async function createProject(input: { name: string; description?: string | null }) {
  const { data } = await http.post<Project>("/api/projects", {
    name: input.name,
    description: input.description ?? null,
  });
  return data;
}

export async function updateProject(input: {
  projectId: string;
  name: string;
  description?: string | null;
}) {
  const { data } = await http.put<Project>(`/api/projects/${input.projectId}`, {
    name: input.name,
    description: input.description ?? null,
  });
  return data;
}

export async function deleteProject(projectId: string) {
  await http.delete(`/api/projects/${projectId}`);
}

