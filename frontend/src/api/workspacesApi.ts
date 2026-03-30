import type { AuthResponse } from "./types";
import { http } from "./http";

export async function createWorkspace(name: string) {
  const { data } = await http.post<AuthResponse>("/api/workspaces", {
    name,
  });
  return data;
}

export async function joinWorkspace(code: string) {
  const { data } = await http.post<AuthResponse>("/api/workspaces/join", {
    code,
  });
  return data;
}

