import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import * as authApi from "../api/authApi";
import type { NormalizedApiError } from "../api/http";
import { useAuth } from "../auth/AuthContext";

export function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const { completeAuthSignIn } = useAuth();
  const [status, setStatus] = useState<"idle" | "working" | "done" | "error">("idle");
  const [error, setError] = useState<string | null>(null);

  const token = searchParams.get("token")?.trim() ?? "";

  useEffect(() => {
    if (!token) {
      setStatus("error");
      setError("Missing verification token. Open the link from your email, or request a new one.");
      return;
    }

    const controller = new AbortController();
    setStatus("working");
    setError(null);

    (async () => {
      try {
        const auth = await authApi.verifyEmail(token, { signal: controller.signal });
        if (controller.signal.aborted) {
          return;
        }
        await completeAuthSignIn(auth);
        if (controller.signal.aborted) {
          return;
        }
        setStatus("done");
      } catch (err) {
        if (controller.signal.aborted) {
          return;
        }
        const api = err as NormalizedApiError;
        setStatus("error");
        setError(api.detail ?? api.title ?? "Verification failed.");
      }
    })();

    return () => controller.abort();
  }, [token, completeAuthSignIn]);

  if (status === "done") {
    return (
      <div className="auth-card">
        <h1 className="text-2xl font-bold tracking-tight text-slate-900">Email verified</h1>
        <p className="muted small" style={{ marginTop: "0.75rem" }}>
          You are signed in. Continue to your dashboard.
        </p>
        <p style={{ marginTop: "1.25rem" }}>
          <Link to="/dashboard" className="button primary">
            Go to dashboard
          </Link>
        </p>
      </div>
    );
  }

  if (status === "error") {
    return (
      <div className="auth-card">
        <h1 className="text-2xl font-bold tracking-tight text-slate-900">Verification</h1>
        {error ? <div className="error-banner" style={{ marginTop: "1rem" }}>{error}</div> : null}
        <p className="muted small" style={{ marginTop: "1rem" }}>
          <Link to="/login">Back to log in</Link>
        </p>
      </div>
    );
  }

  return (
    <div className="auth-card">
      <h1 className="text-2xl font-bold tracking-tight text-slate-900">Verifying your email…</h1>
      <p className="muted small" style={{ marginTop: "0.75rem" }}>
        Please wait a moment.
      </p>
    </div>
  );
}
