import { type FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import type { NormalizedApiError } from "../api/http";
import { useAuth } from "../auth/AuthContext";

export function RegisterPage() {
  const navigate = useNavigate();
  const { register } = useAuth();
  const [email, setEmail] = useState("");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);
    setFieldErrors({});
    setSubmitting(true);
    try {
      await register({
        email: email.trim(),
        userName: userName.trim(),
        password,
        confirmPassword,
      });
      navigate("/dashboard", { replace: true });
    } catch (err) {
      const api = err as NormalizedApiError;
      setFieldErrors(api.fieldErrors ?? {});
      setFormError(api.detail ?? api.title ?? "Could not register.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-card">
      <h1>Create account</h1>
      <p className="muted small">
        Already have an account? <Link to="/login">Log in</Link>
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
          <span>User name</span>
          <input
            autoComplete="username"
            name="userName"
            value={userName}
            onChange={(ev) => setUserName(ev.target.value)}
            required
          />
          {fieldErrors.userName?.length ? (
            <span className="error-text">{fieldErrors.userName.join(" ")}</span>
          ) : null}
        </label>
        <label className="field">
          <span>Password</span>
          <input
            autoComplete="new-password"
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
        <label className="field">
          <span>Confirm password</span>
          <input
            autoComplete="new-password"
            name="confirmPassword"
            type="password"
            value={confirmPassword}
            onChange={(ev) => setConfirmPassword(ev.target.value)}
            required
          />
          {fieldErrors.confirmPassword?.length ? (
            <span className="error-text">{fieldErrors.confirmPassword.join(" ")}</span>
          ) : null}
        </label>
        {fieldErrors.general?.length ? (
          <div className="error-banner">{fieldErrors.general.join(" ")}</div>
        ) : null}
        {formError ? <div className="error-banner">{formError}</div> : null}
        <button className="button primary" type="submit" disabled={submitting}>
          {submitting ? "Creating account…" : "Register"}
        </button>
      </form>
    </div>
  );
}
