import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import type {
  CreateWorkspaceWebhookRequest,
  WebhookDeliveryLogDtoPagedResultDto,
  WebhookDto,
  WebhookTestResponse,
  UpdateWorkspaceWebhookRequest,
} from '../../types/api';

export interface WebhookDeliveriesParams {
  page: number;
  pageSize: number;
}

export function useWebhooks() {
  return useQuery({
    queryKey: ['webhooks'],
    queryFn: async () => {
      const { data } = await api.get<WebhookDto[]>('/Workspaces/webhooks');
      return data;
    },
  });
}

export function useCreateWebhook() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: CreateWorkspaceWebhookRequest) => {
      const { data } = await api.post<WebhookDto>('/Workspaces/webhooks', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['webhooks'] });
    },
  });
}

export function useUpdateWebhook() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ webhookId, payload }: { webhookId: string; payload: UpdateWorkspaceWebhookRequest }) => {
      const { data } = await api.put<WebhookDto>(`/Workspaces/webhooks/${webhookId}`, payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['webhooks'] });
    },
  });
}

export function useDeleteWebhook() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (webhookId: string) => {
      await api.delete(`/Workspaces/webhooks/${webhookId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['webhooks'] });
    },
  });
}

export function useWebhookDeliveries(webhookId: string | null, params: WebhookDeliveriesParams) {
  return useQuery({
    queryKey: ['webhook-deliveries', webhookId, params],
    enabled: Boolean(webhookId),
    queryFn: async () => {
      const { data } = await api.get<WebhookDeliveryLogDtoPagedResultDto>(`/Workspaces/webhooks/${webhookId}/deliveries`, {
        params,
      });
      return data;
    },
  });
}

export function useTestWebhook() {
  return useMutation({
    mutationFn: async (webhookId: string) => {
      const { data } = await api.post<WebhookTestResponse>(`/Workspaces/webhooks/${webhookId}/test`);
      return data;
    },
  });
}
