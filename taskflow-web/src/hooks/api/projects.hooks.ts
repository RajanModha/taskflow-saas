import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import type {
  ActivityLogDtoPagedResultDto,
  CreateMilestoneRequest,
  CreateProjectCommand,
  MilestoneDto,
  MoveBoardTaskRequest,
  ProjectBoardResponse,
  ProjectDto,
  ProjectDtoPagedResultDto,
  UpdateMilestoneRequest,
  UpdateProjectRequest,
} from '../../types/api';

export interface ProjectsQueryParams {
  page: number;
  pageSize: number;
  q?: string;
  sortBy?: string;
  sortDir?: string;
}

export interface BoardFilters {
  assigneeId?: string;
  tagId?: string;
  q?: string;
}

export interface PageQueryParams {
  page: number;
  pageSize: number;
}

function toSearchParams(params: Record<string, unknown>) {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      search.set(key, String(value));
    }
  });
  return search;
}

export function useProjects(params: ProjectsQueryParams) {
  return useQuery({
    queryKey: ['projects', params],
    queryFn: async () => {
      const { data } = await api.get<ProjectDtoPagedResultDto>('/Projects', { params });
      return data;
    },
  });
}

export function useProject(projectId: string | null) {
  return useQuery({
    queryKey: ['project', projectId],
    enabled: Boolean(projectId),
    queryFn: async () => {
      const { data } = await api.get<ProjectDto>(`/Projects/${projectId}`);
      return data;
    },
  });
}

export function useCreateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateProjectCommand) => {
      const { data } = await api.post<ProjectDto>('/Projects', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });
}

export function useUpdateProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ projectId, payload }: { projectId: string; payload: UpdateProjectRequest }) => {
      const { data } = await api.put<ProjectDto>(`/Projects/${projectId}`, payload);
      return data;
    },
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      queryClient.invalidateQueries({ queryKey: ['project', vars.projectId] });
    },
  });
}

export function useDeleteProject() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (projectId: string) => {
      await api.delete(`/Projects/${projectId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
    },
  });
}

export function useRestoreProject() {
  return useMutation({
    mutationFn: async (projectId: string) => {
      await api.post(`/Projects/${projectId}/restore`);
    },
  });
}

export function useBoardData(projectId: string | null, filters: BoardFilters = {}) {
  return useQuery({
    queryKey: ['board', projectId, filters],
    enabled: Boolean(projectId),
    queryFn: async () => {
      const { data } = await api.get<ProjectBoardResponse>(`/Projects/${projectId}/board`, { params: filters });
      return data;
    },
  });
}

export function useMoveTask(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ taskId, payload }: { taskId: string; payload: MoveBoardTaskRequest }) => {
      await api.put(`/Projects/${projectId}/board/tasks/${taskId}/move`, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['board', projectId] });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
    },
  });
}

export function useProjectActivity(projectId: string | null, params: PageQueryParams) {
  return useQuery({
    queryKey: ['project-activity', projectId, params],
    enabled: Boolean(projectId),
    queryFn: async () => {
      const { data } = await api.get<ActivityLogDtoPagedResultDto>(`/Projects/${projectId}/activity`, { params });
      return data;
    },
  });
}

export function useProjectExportUrl(projectId: string) {
  return `${import.meta.env.VITE_API_URL ?? 'http://localhost:5005/api/v1'}/Projects/${projectId}/export`;
}

export function useMilestones(projectId: string | null) {
  return useQuery({
    queryKey: ['milestones', projectId],
    enabled: Boolean(projectId),
    queryFn: async () => {
      const { data } = await api.get<MilestoneDto[]>(`/Projects/${projectId}/milestones`);
      return data;
    },
  });
}

export function useCreateMilestone(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateMilestoneRequest) => {
      const { data } = await api.post<MilestoneDto>(`/Projects/${projectId}/milestones`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['milestones', projectId] });
    },
  });
}

export function useUpdateMilestone(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ milestoneId, payload }: { milestoneId: string; payload: UpdateMilestoneRequest }) => {
      const { data } = await api.put<MilestoneDto>(`/Projects/${projectId}/milestones/${milestoneId}`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['milestones', projectId] });
    },
  });
}

export function useDeleteMilestone(projectId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (milestoneId: string) => {
      await api.delete(`/Projects/${projectId}/milestones/${milestoneId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['milestones', projectId] });
    },
  });
}

export function buildProjectExportUrl(projectId: string, params: Record<string, unknown> = {}) {
  const search = toSearchParams(params).toString();
  const base = `${import.meta.env.VITE_API_URL ?? 'http://localhost:5005/api/v1'}/Projects/${projectId}/export`;
  return search ? `${base}?${search}` : base;
}
