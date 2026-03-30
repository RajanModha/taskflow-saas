import axios, { type AxiosError } from "axios";
import type { ProblemDetails } from "./types";
import { clearStoredToken, getStoredToken } from "../auth/tokenStorage";

const baseURL =
  // In Docker, frontend uses `/api` and Nginx proxies to backend.
  // In local dev, this points directly to the API host.
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, "") ??
  "http://localhost:5005";

export const http = axios.create({
  baseURL,
  timeout: 15000,
  headers: {
    "Content-Type": "application/json",
  },
});

http.interceptors.request.use((config) => {
  const token = getStoredToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

http.interceptors.response.use(
  (res) => res,
  (error: AxiosError<ProblemDetails>) => {
    if (error.response?.status === 401) {
      clearStoredToken();
    }
    return Promise.reject(normalizeAxiosError(error));
  },
);

export type NormalizedApiError = {
  status?: number;
  title: string;
  detail?: string;
  fieldErrors: Record<string, string[]>;
  raw?: unknown;
};

function normalizeAxiosError(error: AxiosError<ProblemDetails>): NormalizedApiError {
  const data = error.response?.data;
  const status = error.response?.status;
  const fieldErrors = data?.errors ?? {};

  if (data && (data.title || data.detail || Object.keys(fieldErrors).length > 0)) {
    return {
      status,
      title: data.title ?? "Request failed",
      detail: data.detail,
      fieldErrors,
      raw: data,
    };
  }

  if (error.code === "ERR_NETWORK") {
    return {
      status,
      title: "Network error",
      detail: "Unable to reach the API. Is the backend running?",
      fieldErrors: {},
    };
  }

  return {
    status,
    title: error.message || "Unexpected error",
    fieldErrors: {},
    raw: error,
  };
}
