import { type FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { NormalizedApiError } from "../api/http";
import { useAuth } from "../auth/AuthContext";

export function JoinWorkspacePage() {
  const navigate = useNavigate();
  const { joinWorkspace } = useAuth();

  const [code, setCode] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);
    setFieldErrors({});
    setSubmitting(true);

    try {
      await joinWorkspace(code.trim().toUpperCase());
      navigate("/dashboard", { replace: true });
    } catch (err) {
      const api = err as NormalizedApiError;
      setFieldErrors(api.fieldErrors ?? {});
      setFormError(api.detail ?? api.title ?? "Could not join workspace.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-card">
      <h1>Join workspace</h1>
      <p className="muted small">Join using your workspace join code.</p>

      <form className="stack gap" onSubmit={onSubmit}>
        <label className="field">
          <span>Join code</span>
          <input
            autoComplete="off"
            name="code"
            value={code}
            onChange={(ev) => setCode(ev.target.value)}
            required
          />
          {fieldErrors.code?.length ? (
            <span className="error-text">{fieldErrors.code.join(" ")}</span>
          ) : null}
        </label>

        {fieldErrors.general?.length ? (
          <div className="error-banner">{fieldErrors.general.join(" ")}</div>
        ) : null}
        {formError ? <div className="error-banner">{formError}</div> : null}

        <button className="button primary" type="submit" disabled={submitting}>
          {submitting ? "Joining…" : "Join workspace"}
        </button>
      </form>
    </div>
  );
}

