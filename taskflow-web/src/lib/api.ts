import axios from 'axios';
import { useAuthStore } from '../stores/authStore';

const fallbackBaseUrl = 'http://localhost:5005/api/v1';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? fallbackBaseUrl,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let refreshing: Promise<string> | null = null;

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as
      | (typeof error.config & { _retry?: boolean })
      | undefined;

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true;

      if (!refreshing) {
        refreshing = (async () => {
          const rt = useAuthStore.getState().getRefreshToken();
          if (!rt) {
            useAuthStore.getState().logout();
            throw error;
          }

          const response = await axios.post(`${originalRequest.baseURL ?? fallbackBaseUrl}/Auth/refresh`, {
            refreshToken: rt,
          });

          useAuthStore.getState().setAuth(response.data);
          return response.data.accessToken as string;
        })().finally(() => {
          refreshing = null;
        });
      }

      const token = await refreshing;
      originalRequest.headers.Authorization = `Bearer ${token}`;
      return api(originalRequest);
    }

    return Promise.reject(error);
  },
);

export function getApiError(error: unknown): string {
  const data = (error as { response?: { data?: unknown } })?.response?.data as
    | { detail?: string; title?: string; errors?: Record<string, string[]> }
    | undefined;

  if (!data) {
    return 'Something went wrong. Please try again.';
  }

  if (data.errors) {
    const messages = Object.values(data.errors).flat();
    return messages[0] ?? data.title ?? 'Validation error';
  }

  return data.detail ?? data.title ?? 'Something went wrong.';
}

export function getFieldErrors(error: unknown): Record<string, string> {
  const data = (error as { response?: { data?: { errors?: Record<string, string[]> } } })?.response?.data
    ?.errors;

  return Object.fromEntries(
    Object.entries(data ?? {}).map(([key, value]) => [key.charAt(0).toLowerCase() + key.slice(1), value[0]]),
  );
}

export default api;
