import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';
import { useAuthStore } from '../../stores/authStore';
import type {
  AuthResponse,
  ChangePasswordRequest,
  ChangePasswordResponse,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  LoginRequest,
  LogoutRequest,
  RegisterPendingResponse,
  RegisterRequest,
  ResendVerificationRequest,
  ResetPasswordRequest,
  ResetPasswordResponse,
  UpdateProfileRequest,
  UserProfileResponse,
  UserSessionItemDto,
  VerifyEmailRequest,
} from '../../types/api';

export function useMe() {
  return useQuery({
    queryKey: ['me'],
    queryFn: async () => {
      const { data } = await api.get<UserProfileResponse>('/Auth/me');
      return data;
    },
  });
}

export function useLogin() {
  return useMutation({
    mutationFn: async (payload: LoginRequest) => {
      const { data } = await api.post<AuthResponse>('/Auth/login', payload);
      return data;
    },
  });
}

export function useRegister() {
  return useMutation({
    mutationFn: async (payload: RegisterRequest) => {
      const { data } = await api.post<RegisterPendingResponse>('/Auth/register', payload);
      return data;
    },
  });
}

export function useVerifyEmail() {
  return useMutation({
    mutationFn: async (payload: VerifyEmailRequest) => {
      const { data } = await api.post<AuthResponse>('/Auth/verify-email', payload);
      return data;
    },
  });
}

export function useResendVerification() {
  return useMutation({
    mutationFn: async (payload: ResendVerificationRequest) => {
      await api.post('/Auth/resend-verification', payload);
    },
  });
}

export function useForgotPassword() {
  return useMutation({
    mutationFn: async (payload: ForgotPasswordRequest) => {
      const { data } = await api.post<ForgotPasswordResponse>('/Auth/forgot-password', payload);
      return data;
    },
  });
}

export function useResetPassword() {
  return useMutation({
    mutationFn: async (payload: ResetPasswordRequest) => {
      const { data } = await api.post<ResetPasswordResponse>('/Auth/reset-password', payload);
      return data;
    },
  });
}

export function useSessions(refreshToken: string | null) {
  return useQuery({
    queryKey: ['auth', 'sessions', refreshToken],
    enabled: Boolean(refreshToken),
    queryFn: async () => {
      const { data } = await api.post<UserSessionItemDto[]>('/Auth/sessions/query', { refreshToken });
      return data;
    },
  });
}

export function useRevokeSession() {
  return useMutation({
    mutationFn: async (sessionId: string) => {
      await api.delete(`/Auth/sessions/${sessionId}`);
    },
  });
}

export function useLogout() {
  return useMutation({
    mutationFn: async (payload: LogoutRequest) => {
      await api.post('/Auth/logout', payload);
    },
  });
}

export function useLogoutAll() {
  return useMutation({
    mutationFn: async () => {
      await api.post('/Auth/logout-all');
    },
  });
}

export function useUpdateProfile() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (payload: UpdateProfileRequest) => {
      const { data } = await api.put<UserProfileResponse>('/Auth/profile', payload);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['me'] });
    },
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: async (payload: ChangePasswordRequest) => {
      const { data } = await api.put<ChangePasswordResponse>('/Auth/password', payload);
      return data;
    },
  });
}

export function useAuthSessionSetter() {
  const setAuth = useAuthStore((state) => state.setAuth);
  const setUser = useAuthStore((state) => state.setUser);
  return { setAuth, setUser };
}
