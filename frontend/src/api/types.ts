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
  role: string;
  organizationId: string;
  organizationName: string;
  organizationJoinCode: string;
  displayName?: string | null;
  avatarUrl?: string | null;
  createdAt: string;
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

export type ChecklistItem = {
  id: string;
  title: string;
  isCompleted: boolean;
  order: number;
  completedAt?: string | null;
};

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
  /** Present when API returns checklist summary on tasks. */
  checklistTotal?: number;
  checklistCompleted?: number;
  checklistProgress?: number;
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};
