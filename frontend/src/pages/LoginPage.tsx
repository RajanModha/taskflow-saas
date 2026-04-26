import { type FormEvent, useMemo, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import * as authApi from "../api/authApi";
import type { NormalizedApiError } from "../api/http";
import { useAuth } from "../auth/AuthContext";

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [showResendHint, setShowResendHint] = useState(false);
  const [resendBusy, setResendBusy] = useState(false);
  const [resendMessage, setResendMessage] = useState<string | null>(null);

  const redirectTo = useMemo(() => {
    const state = location.state as { from?: string } | undefined;
    return state?.from || "/dashboard";
  }, [location.state]);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);
    setFieldErrors({});
    setShowResendHint(false);
    setResendMessage(null);
    setSubmitting(true);
    try {
      await login(email.trim(), password);
      navigate(redirectTo, { replace: true });
    } catch (err) {
      const api = err as NormalizedApiError;
      setFieldErrors(api.fieldErrors ?? {});
      setFormError(api.detail ?? api.title ?? "Could not log in.");
      setShowResendHint(api.status === 403);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-card">
      <h1 className="text-2xl font-bold tracking-tight text-slate-900">Log in</h1>
      <p className="muted small">
        New here? <Link to="/register">Create an account</Link>
      </p>
      <form className="stack gap" onSubmit={onSubmit}>
        <label className="field">
          <span>Email</span>
          <input
            autoComplete="email"
            inputMode="email"
            name="email"
            type="email"
            value={email}
            onChange={(ev) => setEmail(ev.target.value)}
            required
          />
          {fieldErrors.email?.length ? (
            <span className="error-text">{fieldErrors.email.join(" ")}</span>
          ) : null}
        </label>
        <label className="field">
          <span>Password</span>
          <input
            autoComplete="current-password"
            name="password"
            type="password"
            value={password}
            onChange={(ev) => setPassword(ev.target.value)}
            required
          />
          {fieldErrors.password?.length ? (
            <span className="error-text">{fieldErrors.password.join(" ")}</span>
          ) : null}
        </label>
        {formError ? <div className="error-banner">{formError}</div> : null}
        {showResendHint ? (
          <div className="muted small" style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem", alignItems: "center" }}>
            <button
              type="button"
              className="link-button"
              disabled={resendBusy || !email.trim()}
              onClick={async () => {
                setResendBusy(true);
                setResendMessage(null);
                try {
                  await authApi.resendVerificationEmail(email.trim());
                  setResendMessage("If an account exists for this email, we sent a new verification link.");
                } catch {
                  setResendMessage("Could not send right now. Try again in a few minutes.");
                } finally {
                  setResendBusy(false);
                }
              }}
            >
              {resendBusy ? "Sending…" : "Resend verification email"}
            </button>
            {resendMessage ? <span>{resendMessage}</span> : null}
          </div>
        ) : null}
        <button className="button primary" type="submit" disabled={submitting}>
          {submitting ? "Signing in…" : "Sign in"}
        </button>
      </form>
    </div>
  );
}
