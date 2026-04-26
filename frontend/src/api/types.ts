export type AuthResponse = {
  accessToken: string;
  expiresAtUtc: string;
  tokenType: string;
  refreshToken?: string | null;
  refreshTokenExpiresAt?: string | null;
};

export type RegisterPendingResponse = {
  message: string;
};

export type UserProfile = {
  id: string;
  email: string;
  userName: string;
  roles: string[];
  organizationId: string;
  organizationName: string;
  organizationJoinCode: string;
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
};

export type Project = {
  id: string;
  name: string;
  description?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type TaskStatus = 0 | 1 | 2 | 3;
export type TaskPriority = 0 | 1 | 2 | 3;

export type Task = {
  id: string;
  projectId: string;
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDateUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};
