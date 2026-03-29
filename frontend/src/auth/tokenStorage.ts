const ACCESS_TOKEN_KEY = "taskflow.accessToken";
const EXPIRES_AT_KEY = "taskflow.expiresAtUtc";

export function getStoredToken(): string | null {
  return sessionStorage.getItem(ACCESS_TOKEN_KEY);
}

export function setStoredSession(auth: { accessToken: string; expiresAtUtc: string }) {
  sessionStorage.setItem(ACCESS_TOKEN_KEY, auth.accessToken);
  sessionStorage.setItem(EXPIRES_AT_KEY, auth.expiresAtUtc);
}

export function clearStoredToken() {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(EXPIRES_AT_KEY);
}

export function isTokenLikelyExpired(): boolean {
  const raw = sessionStorage.getItem(EXPIRES_AT_KEY);
  if (!raw) {
    return true;
  }
  const expires = Date.parse(raw);
  if (Number.isNaN(expires)) {
    return true;
  }
  return Date.now() >= expires - 30_000;
}
