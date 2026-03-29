import { http } from "./http";
import type { AuthResponse, UserProfile } from "./types";

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
  password: string;
  confirmPassword: string;
}) {
  const { data } = await http.post<AuthResponse>("/api/auth/register", payload);
  return data;
}

export async function fetchMe() {
  const { data } = await http.get<UserProfile>("/api/auth/me");
  return data;
}
