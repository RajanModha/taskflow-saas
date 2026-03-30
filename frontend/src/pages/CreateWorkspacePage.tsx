import { type FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { NormalizedApiError } from "../api/http";
import { useAuth } from "../auth/AuthContext";

export function CreateWorkspacePage() {
  const navigate = useNavigate();
  const { createWorkspace } = useAuth();

  const [name, setName] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setFormError(null);
    setFieldErrors({});
    setSubmitting(true);

    try {
      await createWorkspace(name.trim());
      navigate("/dashboard", { replace: true });
    } catch (err) {
      const api = err as NormalizedApiError;
      setFieldErrors(api.fieldErrors ?? {});
      setFormError(api.detail ?? api.title ?? "Could not create workspace.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="auth-card">
      <h1 className="text-2xl font-bold tracking-tight text-slate-900">Create workspace</h1>
      <p className="muted small">Create a new organization and switch to it.</p>

      <form className="stack gap" onSubmit={onSubmit}>
        <label className="field">
          <span>Workspace name</span>
          <input
            autoComplete="organization"
            name="name"
            value={name}
            onChange={(ev) => setName(ev.target.value)}
            required
          />
          {fieldErrors.name?.length ? (
            <span className="error-text">{fieldErrors.name.join(" ")}</span>
          ) : null}
        </label>

        {fieldErrors.general?.length ? (
          <div className="error-banner">{fieldErrors.general.join(" ")}</div>
        ) : null}
        {formError ? <div className="error-banner">{formError}</div> : null}

        <button className="button primary" type="submit" disabled={submitting}>
          {submitting ? "Creating…" : "Create workspace"}
        </button>
      </form>
    </div>
  );
}

