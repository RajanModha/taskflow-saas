import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import type { Project } from "../api/types";
import { getProjects, createProject, updateProject, deleteProject } from "../api/projectsApi";
import type { NormalizedApiError } from "../api/http";

export function ProjectsPage() {
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [mutationLoading, setMutationLoading] = useState(false);
  // Increment to force a list refetch after successful mutations.
  const [refreshIndex, setRefreshIndex] = useState(0);

  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [q, setQ] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [sortBy, setSortBy] = useState<string>("createdatutc");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");

  const [projects, setProjects] = useState<Project[]>([]);
  const [totalCount, setTotalCount] = useState(0);

  const [createName, setCreateName] = useState("");
  const [createDescription, setCreateDescription] = useState<string>("");

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editDescription, setEditDescription] = useState<string>("");

  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / pageSize)), [totalCount]);

  useEffect(() => {
    const id = setTimeout(() => setDebouncedQ(q), 300);
    return () => clearTimeout(id);
  }, [q]);

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await getProjects({
          page,
          pageSize,
          q: debouncedQ.trim() || undefined,
          sortBy,
          sortDir: sortDir ?? "desc",
        });

        if (cancelled) return;
        setProjects(result.items);
        setTotalCount(result.totalCount);
      } catch {
        if (cancelled) return;
        setError("Failed to load projects.");
      } finally {
        if (cancelled) return;
        setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [page, debouncedQ, sortBy, sortDir, refreshIndex]);

  async function onCreate() {
    const name = createName.trim();
    if (!name) return;
    try {
      setMutationLoading(true);
      setError(null);
      await createProject({ name, description: createDescription.trim() || null });
      setCreateName("");
      setCreateDescription("");
      setPage(1);
      setRefreshIndex((i) => i + 1);
    } catch (e) {
      const apiError = e as NormalizedApiError;
      setError(apiError.detail ?? apiError.title ?? "Failed to create project.");
    } finally {
      setMutationLoading(false);
    }
  }

  async function onDelete(projectId: string) {
    try {
      setMutationLoading(true);
      setError(null);
      await deleteProject(projectId);
      if (editingId === projectId) {
        setEditingId(null);
      }

      const nextPage = page > 1 && projects.length === 1 ? page - 1 : page;
      setPage(nextPage);
      setRefreshIndex((i) => i + 1);
    } catch (e) {
      const apiError = e as NormalizedApiError;
      setError(apiError.detail ?? apiError.title ?? "Failed to delete project.");
    } finally {
      setMutationLoading(false);
    }
  }

  function startEdit(p: Project) {
    setEditingId(p.id);
    setEditName(p.name);
    setEditDescription(p.description ?? "");
  }

  async function onSaveEdit() {
    if (!editingId) return;
    try {
      setMutationLoading(true);
      setError(null);
      await updateProject({
        projectId: editingId,
        name: editName.trim(),
        description: editDescription.trim() || null,
      });
      setEditingId(null);
      setRefreshIndex((i) => i + 1);
    } catch (e) {
      const apiError = e as NormalizedApiError;
      setError(apiError.detail ?? apiError.title ?? "Failed to update project.");
    } finally {
      setMutationLoading(false);
    }
  }

  return (
    <div className="stack">
      <div className="toolbar">
        <div className="stack" style={{ gap: "0.35rem", maxWidth: 520 }}>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">Projects</h1>
          <p className="muted small">
            Create projects and manage tasks per workspace.
          </p>
        </div>

        <div className="row gap" style={{ justifyContent: "flex-end" }}>
          <input
            style={{ minWidth: 260 }}
            placeholder="Search projects…"
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />
          <button
            className="button"
            type="button"
            onClick={() => {
              setPage(1);
            }}
          >
            Search
          </button>
        </div>
      </div>

      <section className="panel">
        <h2 className="text-lg font-semibold tracking-tight text-slate-900">Create project</h2>
        <div className="row gap">
          <input
            style={{ flex: 1 }}
            placeholder="Project name"
            value={createName}
            onChange={(e) => setCreateName(e.target.value)}
          />
        </div>
        <div className="row gap" style={{ marginTop: "0.75rem" }}>
          <input
            style={{ flex: 1 }}
            placeholder="Description (optional)"
            value={createDescription}
            onChange={(e) => setCreateDescription(e.target.value)}
          />
          <button className="button primary" type="button" onClick={onCreate} disabled={loading || mutationLoading}>
            Create
          </button>
        </div>
      </section>

      <section className="panel">
        <div className="toolbar">
          <div className="stack" style={{ gap: "0.25rem" }}>
            <h2 className="text-lg font-semibold tracking-tight text-slate-900">Your projects</h2>
            <span className="muted small">
              {totalCount} total • Page {page} of {totalPages}
            </span>
          </div>

          <div className="row gap">
            <select value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
              <option value="createdatutc">Created</option>
              <option value="name">Name</option>
            </select>
            <select value={sortDir} onChange={(e) => setSortDir(e.target.value as "asc" | "desc")}>
              <option value="desc">Desc</option>
              <option value="asc">Asc</option>
            </select>
          </div>
        </div>

        {error ? <div className="error-banner">{error}</div> : null}

        {loading ? (
          <div className="space-y-2 py-2">
            <div className="skeleton h-12 w-full" />
            <div className="skeleton h-12 w-full" />
            <div className="skeleton h-12 w-full" />
          </div>
        ) : (
          <div className="overflow-x-auto rounded-2xl border border-slate-200">
            <table className="table">
              <thead>
                <tr>
                  <th style={{ width: 340 }}>Name</th>
                  <th>Description</th>
                  <th style={{ width: 220 }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {projects.length === 0 ? (
                  <tr>
                    <td colSpan={3} className="muted">
                      No projects yet.
                    </td>
                  </tr>
                ) : null}

                {projects.map((p) => (
                  <tr key={p.id}>
                    <td>
                      {editingId === p.id ? (
                        <input value={editName} onChange={(e) => setEditName(e.target.value)} />
                      ) : (
                        <strong>{p.name}</strong>
                      )}
                    </td>
                    <td>
                      {editingId === p.id ? (
                        <input value={editDescription} onChange={(e) => setEditDescription(e.target.value)} />
                      ) : (
                        <span className="muted small">{p.description || "-"}</span>
                      )}
                    </td>
                    <td>
                      {editingId === p.id ? (
                        <div className="actions">
                          <button className="button primary" type="button" onClick={onSaveEdit} disabled={mutationLoading}>
                            Save
                          </button>
                          <button className="button" type="button" onClick={() => setEditingId(null)} disabled={mutationLoading}>
                            Cancel
                          </button>
                        </div>
                      ) : (
                        <div className="actions">
                          <button
                            className="button"
                            type="button"
                            disabled={mutationLoading}
                            onClick={() => navigate(`/projects/${p.id}/tasks`)}
                          >
                            Tasks
                          </button>
                          <button className="button" type="button" onClick={() => startEdit(p)} disabled={mutationLoading}>
                            Edit
                          </button>
                          <button className="button" type="button" onClick={() => onDelete(p.id)} disabled={mutationLoading}>
                            Delete
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="row gap" style={{ justifyContent: "space-between", marginTop: "1rem" }}>
          <button className="button" type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
            Prev
          </button>
          <button
            className="button"
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
          >
            Next
          </button>
        </div>
      </section>
    </div>
  );
}

