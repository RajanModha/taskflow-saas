import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import { useAuthStore } from '../../stores/authStore';
import type {
  AcceptInviteRequest,
  AuthResponse,
  CreateWorkspaceRequest,
  InviteMemberRequest,
  JoinWorkspaceRequest,
  MyWorkspaceResponse,
  RegenerateJoinCodeResponse,
  ResendInviteRequest,
  UpdateMemberRoleRequest,
  UpdateWorkspaceRequest,
  UpdateWorkspaceResponse,
  WorkspaceInviteRowDto,
  WorkspaceMembersPageResponse,
} from '../../types/api';

export interface MembersQueryParams {
  page: number;
  pageSize: number;
  q?: string;
  role?: string;
}

export function useWorkspaceMe() {
  return useQuery({
    queryKey: ['workspace', 'me'],
    queryFn: async () => {
      const { data } = await api.get<MyWorkspaceResponse>('/Workspaces/me');
      return data;
    },
  });
}

export function useMembers(params: MembersQueryParams) {
  return useQuery({
    queryKey: ['workspace', 'members', params],
    queryFn: async () => {
      const { data } = await api.get<WorkspaceMembersPageResponse>('/Workspaces/members', { params });
      return data;
    },
  });
}

export function useInvites() {
  return useQuery({
    queryKey: ['workspace', 'invites'],
    queryFn: async () => {
      const { data } = await api.get<WorkspaceInviteRowDto[]>('/Workspaces/invites');
      return data;
    },
  });
}

export function useInviteMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: InviteMemberRequest) => {
      await api.post('/Workspaces/invite', payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspace', 'invites'] });
    },
  });
}

export function useResendInvite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: ResendInviteRequest) => {
      await api.post('/Workspaces/invite/resend', payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspace', 'invites'] });
    },
  });
}

export function useCancelInvite() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (inviteId: string) => {
      await api.delete(`/Workspaces/invites/${inviteId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspace', 'invites'] });
    },
  });
}

export function useAcceptInvite() {
  return useMutation({
    mutationFn: async (payload: AcceptInviteRequest) => {
      await api.post('/Workspaces/invites/accept', payload);
    },
  });
}

export function useUpdateMemberRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ memberId, payload }: { memberId: string; payload: UpdateMemberRoleRequest }) => {
      await api.put(`/Workspaces/members/${memberId}/role`, payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspace', 'members'] });
    },
  });
}

export function useRemoveMember() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (memberId: string) => {
      await api.delete(`/Workspaces/members/${memberId}`);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspace', 'members'] });
    },
  });
}

export function useRegenerateJoinCode() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const { data } = await api.post<RegenerateJoinCodeResponse>('/Workspaces/invite-code/regenerate');
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspace', 'me'] });
    },
  });
}

export function useUpdateWorkspace() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: UpdateWorkspaceRequest) => {
      const { data } = await api.put<UpdateWorkspaceResponse>('/Workspaces', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspace', 'me'] });
    },
  });
}

export function useCreateWorkspace() {
  const setAuth = useAuthStore((state) => state.setAuth);
  return useMutation({
    mutationFn: async (payload: CreateWorkspaceRequest) => {
      const { data } = await api.post<AuthResponse>('/Workspaces', payload);
      return data;
    },
    onSuccess: (data) => {
      setAuth(data);
    },
  });
}

export function useJoinWorkspace() {
  const setAuth = useAuthStore((state) => state.setAuth);
  return useMutation({
    mutationFn: async (payload: JoinWorkspaceRequest) => {
      const { data } = await api.post<AuthResponse>('/Workspaces/join', payload);
      return data;
    },
    onSuccess: (data) => {
      setAuth(data);
    },
  });
}
