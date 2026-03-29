export type AuthResponse = {
  accessToken: string;
  expiresAtUtc: string;
  tokenType: string;
};

export type UserProfile = {
  id: string;
  email: string;
  userName: string;
  roles: string[];
};

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
};
