import { useQuery } from '@tanstack/react-query';
import api from '../../lib/api';
import type { ActivityLogDtoPagedResultDto } from '../../types/api';

export interface ActivityQueryParams {
  page: number;
  pageSize: number;
}

export function useTaskActivity(taskId: string | null, params: ActivityQueryParams) {
  return useQuery({
    queryKey: ['task-activity', taskId, params],
    enabled: Boolean(taskId),
    queryFn: async () => {
      const { data } = await api.get<ActivityLogDtoPagedResultDto>(`/Tasks/${taskId}/activity`, { params });
      return data;
    },
  });
}

export function useProjectActivity(projectId: string | null, params: ActivityQueryParams) {
  return useQuery({
    queryKey: ['project-activity', projectId, params],
    enabled: Boolean(projectId),
    queryFn: async () => {
      const { data } = await api.get<ActivityLogDtoPagedResultDto>(`/Projects/${projectId}/activity`, { params });
      return data;
    },
  });
}
