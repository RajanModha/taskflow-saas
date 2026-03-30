import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import * as authApi from "../api/authApi";
import type { UserProfile } from "../api/types";
import {
  clearStoredToken,
  getStoredToken,
  isTokenLikelyExpired,
  setStoredSession,
} from "./tokenStorage";
import * as workspacesApi from "../api/workspacesApi";

type AuthState = {
  user: UserProfile | null;
  isLoading: boolean;
  isAuthenticated: boolean;
};

type AuthContextValue = AuthState & {
  login: (email: string, password: string) => Promise<void>;
  register: (input: {
    email: string;
    userName: string;
    organizationName: string;
    password: string;
    confirmPassword: string;
  }) => Promise<void>;
  createWorkspace: (name: string) => Promise<void>;
  joinWorkspace: (code: string) => Promise<void>;
  logout: () => void;
  refreshProfile: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const refreshProfile = useCallback(async () => {
    const token = getStoredToken();
    if (!token || isTokenLikelyExpired()) {
      clearStoredToken();
      setUser(null);
      return;
    }

    const profile = await authApi.fetchMe();
    setUser(profile);
  }, []);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        const token = getStoredToken();
        if (!token || isTokenLikelyExpired()) {
          clearStoredToken();
          if (!cancelled) {
            setUser(null);
          }
          return;
        }
        await refreshProfile();
      } catch {
        clearStoredToken();
        if (!cancelled) {
          setUser(null);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [refreshProfile]);

  const login = useCallback(async (email: string, password: string) => {
    const auth = await authApi.login(email, password);
    setStoredSession({
      accessToken: auth.accessToken,
      expiresAtUtc: auth.expiresAtUtc,
    });
    await refreshProfile();
  }, [refreshProfile]);

  const register = useCallback(
    async (input: {
      email: string;
      userName: string;
      organizationName: string;
      password: string;
      confirmPassword: string;
    }) => {
      const auth = await authApi.register(input);
      setStoredSession({
        accessToken: auth.accessToken,
        expiresAtUtc: auth.expiresAtUtc,
      });
      await refreshProfile();
    },
    [refreshProfile],
  );

  const createWorkspace = useCallback(
    async (name: string) => {
      const auth = await workspacesApi.createWorkspace(name);
      setStoredSession({
        accessToken: auth.accessToken,
        expiresAtUtc: auth.expiresAtUtc,
      });
      await refreshProfile();
    },
    [refreshProfile],
  );

  const joinWorkspace = useCallback(
    async (code: string) => {
      const auth = await workspacesApi.joinWorkspace(code);
      setStoredSession({
        accessToken: auth.accessToken,
        expiresAtUtc: auth.expiresAtUtc,
      });
      await refreshProfile();
    },
    [refreshProfile],
  );

  const logout = useCallback(() => {
    clearStoredToken();
    setUser(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isLoading,
      isAuthenticated: Boolean(user),
      login,
      register,
      createWorkspace,
      joinWorkspace,
      logout,
      refreshProfile,
    }),
    [user, isLoading, login, register, createWorkspace, joinWorkspace, logout, refreshProfile],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }
  return ctx;
}
