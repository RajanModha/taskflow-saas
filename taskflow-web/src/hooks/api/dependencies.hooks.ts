import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import type { AddTaskDependencyRequest, TaskDependenciesResponse } from '../../types/api';

export function useTaskDependencies(taskId: string | null) {
  return useQuery({
    queryKey: ['dependencies', taskId],
    enabled: Boolean(taskId),
    queryFn: async () => {
      const { data } = await api.get<TaskDependenciesResponse>(`/Tasks/${taskId}/dependencies`);
      return data;
    },
  });
}

export function useAddDependency(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: AddTaskDependencyRequest) => {
      await api.post(`/Tasks/${taskId}/dependencies`, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dependencies', taskId] });
    },
  });
}

export function useRemoveDependency(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (blockingTaskId: string) => {
      await api.delete(`/Tasks/${taskId}/dependencies/${blockingTaskId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dependencies', taskId] });
    },
  });
}
