import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import type { CreateTaskTemplateRequest, TaskTemplateDto } from '../../types/api';

export function useTemplates() {
  return useQuery({
    queryKey: ['templates'],
    queryFn: async () => {
      const { data } = await api.get<TaskTemplateDto[]>('/Workspaces/task-templates');
      return data;
    },
  });
}

export function useTemplate(id: string | null) {
  return useQuery({
    queryKey: ['template', id],
    enabled: Boolean(id),
    queryFn: async () => {
      const { data } = await api.get<TaskTemplateDto>(`/Workspaces/task-templates/${id}`);
      return data;
    },
  });
}

export function useCreateTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateTaskTemplateRequest) => {
      const { data } = await api.post<TaskTemplateDto>('/Workspaces/task-templates', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['templates'] });
    },
  });
}

export function useUpdateTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, payload }: { id: string; payload: Partial<CreateTaskTemplateRequest> }) => {
      const { data } = await api.put<TaskTemplateDto>(`/Workspaces/task-templates/${id}`, payload);
      return data;
    },
    onSuccess: (_, vars) => {
      queryClient.invalidateQueries({ queryKey: ['templates'] });
      queryClient.invalidateQueries({ queryKey: ['template', vars.id] });
    },
  });
}

export function useDeleteTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/Workspaces/task-templates/${id}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['templates'] });
    },
  });
}
