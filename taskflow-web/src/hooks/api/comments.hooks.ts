import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import type { CommentDtoPagedResultDto, CreateCommentRequest, UpdateCommentRequest } from '../../types/api';

export interface CommentsQueryParams {
  page: number;
  pageSize: number;
}

export function useComments(taskId: string | null, params: CommentsQueryParams) {
  return useQuery({
    queryKey: ['comments', taskId, params],
    enabled: Boolean(taskId),
    queryFn: async () => {
      const { data } = await api.get<CommentDtoPagedResultDto>(`/Tasks/${taskId}/comments`, { params });
      return data;
    },
  });
}

export function useCreateComment(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateCommentRequest) => {
      const { data } = await api.post(`/Tasks/${taskId}/comments`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', taskId] });
      queryClient.invalidateQueries({ queryKey: ['task', taskId] });
    },
  });
}

export function useUpdateComment(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ commentId, payload }: { commentId: string; payload: UpdateCommentRequest }) => {
      const { data } = await api.put(`/Tasks/${taskId}/comments/${commentId}`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', taskId] });
    },
  });
}

export function useDeleteComment(taskId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (commentId: string) => {
      await api.delete(`/Tasks/${taskId}/comments/${commentId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', taskId] });
    },
  });
}
