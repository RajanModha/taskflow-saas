import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import api from '../../lib/api';
import type {
  AssignTaskRequest,
  BulkAssignRequest,
  BulkDeleteRequest,
  BulkTaskDeleteResultDto,
  BulkTaskOperationResultDto,
  CreateTaskCommand,
  CreateTaskFromTemplateRequest,
  TaskDto,
  TaskDtoPagedResultDto,
  UpdateTaskRequest,
} from '../../types/api';

export interface TasksQueryParams {
  page: number;
  pageSize: number;
  projectId?: string;
  status?: number;
  priority?: number;
  dueFromUtc?: string;
  dueToUtc?: string;
  q?: string;
  sortBy?: string;
  sortDir?: string;
  assignedToMe?: boolean;
  assigneeId?: string;
  tagId?: string;
  milestoneId?: string;
  isBlocked?: boolean;
  includeDeleted?: boolean;
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

export function useTasks(params: TasksQueryParams) {
  return useQuery({
    queryKey: ['tasks', params],
    queryFn: async () => {
      const { data } = await api.get<TaskDtoPagedResultDto>('/Tasks', { params });
      return data;
    },
  });
}

export function useTask(taskId: string | null) {
  return useQuery({
    queryKey: ['task', taskId],
    enabled: Boolean(taskId),
    queryFn: async () => {
      const { data } = await api.get<TaskDto>(`/Tasks/${taskId}`);
      return data;
    },
  });
}

export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateTaskCommand) => {
      const { data } = await api.post<TaskDto>('/Tasks', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useUpdateTask(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: UpdateTaskRequest) => {
      const { data } = await api.put<TaskDto>(`/Tasks/${taskId}`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['task', taskId] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function usePatchTask(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: Partial<UpdateTaskRequest>) => {
      const { data } = await api.patch<TaskDto>(`/Tasks/${taskId}`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['task', taskId] });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useDeleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (taskId: string) => {
      await api.delete(`/Tasks/${taskId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useRestoreTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (taskId: string) => api.post(`/Tasks/${taskId}/restore`).then((response) => response.data as TaskDto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['trash'] });
      toast.success('Task restored successfully');
    },
  });
}

export function usePermanentDeleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (taskId: string) => api.delete(`/Tasks/${taskId}/permanent`),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['trash'] });
      toast.success('Task permanently deleted');
    },
  });
}

export function useAssignTask(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: AssignTaskRequest) => {
      const { data } = await api.put<TaskDto>(`/Tasks/${taskId}/assign`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['task', taskId] });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useOverdueTasks(params: Omit<TasksQueryParams, 'includeDeleted'>) {
  return useQuery({
    queryKey: ['tasks', 'overdue', params],
    queryFn: async () => {
      const { data } = await api.get<TaskDtoPagedResultDto>('/Tasks/overdue', { params });
      return data;
    },
  });
}

export function useDeletedTasks(params: { page: number; pageSize: number; projectId?: string }) {
  return useQuery({
    queryKey: ['trash', params],
    queryFn: () =>
      api
        .get('/Tasks', {
          params: { ...params, includeDeleted: true, deletedOnly: true },
        })
        .then((response) => response.data as TaskDtoPagedResultDto),
  });
}

export function useBulkUpdateTasks() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: { taskIds: string[]; patch: Partial<UpdateTaskRequest> }) => {
      const { data } = await api.post<BulkTaskOperationResultDto>('/Tasks/bulk-update', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useBulkDeleteTasks() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: BulkDeleteRequest) => {
      const { data } = await api.post<BulkTaskDeleteResultDto>('/Tasks/bulk-delete', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useBulkAssignTasks() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: BulkAssignRequest) => {
      const { data } = await api.post<BulkTaskOperationResultDto>('/Tasks/bulk-assign', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useCreateTaskFromTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateTaskFromTemplateRequest) => {
      const { data } = await api.post<TaskDto>('/Tasks/from-template', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useTaskExportUrl(params: Record<string, unknown>) {
  const search = toSearchParams(params).toString();
  const base = `${import.meta.env.VITE_API_URL ?? 'http://localhost:5005/api/v1'}/Tasks/export`;
  return search ? `${base}?${search}` : base;
}
