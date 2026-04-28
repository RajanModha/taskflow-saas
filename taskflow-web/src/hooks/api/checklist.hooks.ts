import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import type { AddChecklistItemRequest, ChecklistItemDto, ReorderChecklistRequest, UpdateChecklistItemRequest } from '../../types/api';

export function useChecklist(taskId: string | null) {
  return useQuery({
    queryKey: ['checklist', taskId],
    enabled: Boolean(taskId),
    queryFn: async () => {
      const { data } = await api.get<ChecklistItemDto[]>(`/Tasks/${taskId}/checklist`);
      return data;
    },
  });
}

export function useAddChecklistItem(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: AddChecklistItemRequest) => {
      const { data } = await api.post<ChecklistItemDto>(`/Tasks/${taskId}/checklist`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklist', taskId] });
    },
  });
}

export function useUpdateChecklistItem(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ itemId, payload }: { itemId: string; payload: UpdateChecklistItemRequest }) => {
      const { data } = await api.put<ChecklistItemDto>(`/Tasks/${taskId}/checklist/${itemId}`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklist', taskId] });
    },
  });
}

export function useDeleteChecklistItem(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (itemId: string) => {
      await api.delete(`/Tasks/${taskId}/checklist/${itemId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklist', taskId] });
    },
  });
}

export function useReorderChecklist(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: ReorderChecklistRequest) => {
      await api.post(`/Tasks/${taskId}/checklist/reorder`, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['checklist', taskId] });
    },
  });
}
