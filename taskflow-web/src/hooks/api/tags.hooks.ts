import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import type { CreateWorkspaceTagRequest, TagDto, UpdateWorkspaceTagRequest } from '../../types/api';

export function useTags() {
  return useQuery({
    queryKey: ['tags'],
    queryFn: async () => {
      const { data } = await api.get<TagDto[]>('/Workspaces/tags');
      return data;
    },
  });
}

export function useCreateTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateWorkspaceTagRequest) => {
      const { data } = await api.post<TagDto>('/Workspaces/tags', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
    },
  });
}

export function useUpdateTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ tagId, payload }: { tagId: string; payload: UpdateWorkspaceTagRequest }) => {
      const { data } = await api.put<TagDto>(`/Workspaces/tags/${tagId}`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
    },
  });
}

export function useDeleteTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (tagId: string) => {
      await api.delete(`/Workspaces/tags/${tagId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
    },
  });
}

export function useAddTagToTask(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (tagId: string) => {
      await api.post(`/Tasks/${taskId}/tags/${tagId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['task', taskId] });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}

export function useRemoveTagFromTask(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (tagId: string) => {
      await api.delete(`/Tasks/${taskId}/tags/${tagId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['task', taskId] });
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
    },
  });
}
