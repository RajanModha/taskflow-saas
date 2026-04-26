import { http } from "./http";
import type { AuthResponse, RegisterPendingResponse, UserProfile } from "./types";

export async function login(email: string, password: string) {
  const { data } = await http.post<AuthResponse>("/api/auth/login", {
    email,
    password,
  });
  return data;
}

export async function register(payload: {
  email: string;
  userName: string;
  organizationName: string;
  password: string;
  confirmPassword: string;
}) {
  const { data } = await http.post<RegisterPendingResponse>("/api/auth/register", payload);
  return data;
}

export async function verifyEmail(token: string, options?: { signal?: AbortSignal }) {
  const { data } = await http.post<AuthResponse>(
    "/api/auth/verify-email",
    { token },
    { signal: options?.signal },
  );
  return data;
}

export async function resendVerificationEmail(email: string) {
  await http.post("/api/auth/resend-verification", { email });
}

export async function fetchMe() {
  const { data } = await http.get<UserProfile>("/api/auth/me");
  return data;
}
