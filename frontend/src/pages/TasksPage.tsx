import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import type { Task, TaskPriority, TaskStatus } from "../api/types";
import { createTask, deleteTask, getTasks, updateTask } from "../api/tasksApi";
import type { NormalizedApiError } from "../api/http";

const statusLabel: Record<number, string> = {
  0: "Backlog",
  1: "Todo",
  2: "In Progress",
  3: "Done",
};

const priorityLabel: Record<number, string> = {
  0: "Low",
  1: "Medium",
  2: "High",
  3: "Urgent",
};

function toDateInputValue(iso?: string | null): string {
  if (!iso) return "";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "";
  return d.toISOString().slice(0, 10);
}

function toIsoUtcFromDateInput(value: string): string | null {
  if (!value) return null;
  // Date inputs are local-free; normalize as UTC midnight.
  const d = new Date(`${value}T00:00:00Z`);
  if (Number.isNaN(d.getTime())) return null;
  return d.toISOString();
}

export function TasksPage() {
  const navigate = useNavigate();
  const params = useParams();
  const projectId = params.projectId;

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [mutationLoading, setMutationLoading] = useState(false);

  const [page, setPage] = useState(1);
  const pageSize = 20;

  const [q, setQ] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [status, setStatus] = useState<TaskStatus | null>(null);
  const [priority, setPriority] = useState<TaskPriority | null>(null);
  const [dueFromUtc, setDueFromUtc] = useState<string>("");
  const [dueToUtc, setDueToUtc] = useState<string>("");

  const [sortBy, setSortBy] = useState<string>("createdatutc");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");

  const [tasks, setTasks] = useState<Task[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const totalPages = useMemo(() => Math.max(1, Math.ceil(totalCount / pageSize)), [totalCount]);

  const [createTitle, setCreateTitle] = useState("");
  const [createDescription, setCreateDescription] = useState("");
  const [createStatus, setCreateStatus] = useState<TaskStatus>(0);
  const [createPriority, setCreatePriority] = useState<TaskPriority>(1);
  const [createDueDate, setCreateDueDate] = useState("");

  const [editingTaskId, setEditingTaskId] = useState<string | null>(null);
  const [editTitle, setEditTitle] = useState("");
  const [editDescription, setEditDescription] = useState("");
  const [editStatus, setEditStatus] = useState<TaskStatus>(0);
  const [editPriority, setEditPriority] = useState<TaskPriority>(1);
  const [editDueDate, setEditDueDate] = useState("");

  useEffect(() => {
    const id = setTimeout(() => setDebouncedQ(q), 300);
    return () => clearTimeout(id);
  }, [q]);

  useEffect(() => {
    if (!projectId) return;
    let cancelled = false;
    (async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await getTasks({
          page,
          pageSize,
          projectId,
          status,
          priority,
          dueFromUtc: dueFromUtc ? toIsoUtcFromDateInput(dueFromUtc) : null,
          dueToUtc: dueToUtc ? toIsoUtcFromDateInput(dueToUtc) : null,
          q: debouncedQ.trim() || undefined,
          sortBy,
          sortDir,
        });
        if (cancelled) return;
        setTasks(result.items);
        setTotalCount(result.totalCount);
      } catch {
        if (cancelled) return;
        setError("Failed to load tasks.");
      } finally {
        if (cancelled) return;
        setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [projectId, page, pageSize, debouncedQ, status, priority, dueFromUtc, dueToUtc, sortBy, sortDir]);

  async function onCreateTask() {
    if (!projectId) return;
    const title = createTitle.trim();
    if (!title) return;
    try {
      setMutationLoading(true);
      setError(null);
      await createTask({
        projectId,
        title,
        description: createDescription.trim() || null,
        status: createStatus,
        priority: createPriority,
        dueDateUtc: createDueDate ? toIsoUtcFromDateInput(createDueDate) : null,
      });

      setCreateTitle("");
      setCreateDescription("");
      setCreateDueDate("");
      setPage(1);
      const result = await getTasks({
        page: 1,
        pageSize,
        projectId,
        status,
        priority,
        dueFromUtc: dueFromUtc ? toIsoUtcFromDateInput(dueFromUtc) : null,
        dueToUtc: dueToUtc ? toIsoUtcFromDateInput(dueToUtc) : null,
        q: debouncedQ.trim() || undefined,
        sortBy,
        sortDir,
      });
      setTasks(result.items);
      setTotalCount(result.totalCount);
    } catch (e) {
      const apiError = e as NormalizedApiError;
      setError(apiError.detail ?? apiError.title ?? "Failed to create task.");
    } finally {
      setMutationLoading(false);
    }
  }

  function startEdit(t: Task) {
    setEditingTaskId(t.id);
    setEditTitle(t.title);
    setEditDescription(t.description ?? "");
    setEditStatus(t.status);
    setEditPriority(t.priority);
    setEditDueDate(toDateInputValue(t.dueDateUtc));
  }

  async function onSaveEdit() {
    if (!editingTaskId) return;
    try {
      setMutationLoading(true);
      setError(null);
      await updateTask({
        taskId: editingTaskId,
        title: editTitle.trim(),
        description: editDescription.trim() || null,
        status: editStatus,
        priority: editPriority,
        dueDateUtc: editDueDate ? toIsoUtcFromDateInput(editDueDate) : null,
      });
      setEditingTaskId(null);
      const result = await getTasks({
        page,
        pageSize,
        projectId: projectId!,
        status,
        priority,
        dueFromUtc: dueFromUtc ? toIsoUtcFromDateInput(dueFromUtc) : null,
        dueToUtc: dueToUtc ? toIsoUtcFromDateInput(dueToUtc) : null,
        q: debouncedQ.trim() || undefined,
        sortBy,
        sortDir,
      });
      setTasks(result.items);
      setTotalCount(result.totalCount);
    } catch (e) {
      const apiError = e as NormalizedApiError;
      setError(apiError.detail ?? apiError.title ?? "Failed to update task.");
    } finally {
      setMutationLoading(false);
    }
  }

  async function onDeleteTask(taskId: string) {
    try {
      setMutationLoading(true);
      setError(null);
      await deleteTask(taskId);
      if (editingTaskId === taskId) {
        setEditingTaskId(null);
      }
      const nextPage = page > 1 && tasks.length === 1 ? page - 1 : page;

      const result = await getTasks({
        page: nextPage,
        pageSize,
        projectId: projectId!,
        status,
        priority,
        dueFromUtc: dueFromUtc ? toIsoUtcFromDateInput(dueFromUtc) : null,
        dueToUtc: dueToUtc ? toIsoUtcFromDateInput(dueToUtc) : null,
        q: debouncedQ.trim() || undefined,
        sortBy,
        sortDir,
      });

      setTasks(result.items);
      setTotalCount(result.totalCount);
      setPage(nextPage);
    } catch (e) {
      const apiError = e as NormalizedApiError;
      setError(apiError.detail ?? apiError.title ?? "Failed to delete task.");
    } finally {
      setMutationLoading(false);
    }
  }

  return (
    <div className="stack">
      <div className="toolbar">
        <div className="stack" style={{ gap: "0.35rem", maxWidth: 520 }}>
          <h1 style={{ margin: 0 }}>Tasks</h1>
          <p className="muted small" style={{ margin: 0 }}>
            Manage tasks within your workspace project.
          </p>
        </div>

        <div className="row gap" style={{ justifyContent: "flex-end" }}>
          <button className="button" type="button" onClick={() => navigate("/projects")}>
            Back to projects
          </button>
        </div>
      </div>

      <section className="panel">
        <h2 style={{ marginTop: 0 }}>Task filters</h2>
        <div className="row gap">
          <input
            style={{ flex: 1 }}
            placeholder="Search by title…"
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />

          <select
            value={status ?? ""}
            onChange={(e) => setStatus(e.target.value === "" ? null : (Number(e.target.value) as TaskStatus))}
          >
            <option value="">All statuses</option>
            <option value="0">Backlog</option>
            <option value="1">Todo</option>
            <option value="2">In Progress</option>
            <option value="3">Done</option>
          </select>

          <select
            value={priority ?? ""}
            onChange={(e) =>
              setPriority(e.target.value === "" ? null : (Number(e.target.value) as TaskPriority))
            }
          >
            <option value="">All priorities</option>
            <option value="0">Low</option>
            <option value="1">Medium</option>
            <option value="2">High</option>
            <option value="3">Urgent</option>
          </select>
        </div>

        <div className="row gap" style={{ marginTop: "0.75rem" }}>
          <input
            type="date"
            value={dueFromUtc}
            onChange={(e) => setDueFromUtc(e.target.value)}
            placeholder="Due from"
          />
          <input
            type="date"
            value={dueToUtc}
            onChange={(e) => setDueToUtc(e.target.value)}
            placeholder="Due to"
          />
          <div className="row gap">
            <select value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
              <option value="createdatutc">Created</option>
              <option value="duedateutc">Due date</option>
              <option value="priority">Priority</option>
              <option value="status">Status</option>
            </select>
            <select value={sortDir} onChange={(e) => setSortDir(e.target.value as "asc" | "desc")}>
              <option value="desc">Desc</option>
              <option value="asc">Asc</option>
            </select>
          </div>
          <button className="button primary" type="button" onClick={() => setPage(1)}>
            Apply
          </button>
        </div>
      </section>

      <section className="panel">
        <h2 style={{ marginTop: 0 }}>Create task</h2>
        <div className="row gap">
          <input style={{ flex: 1 }} placeholder="Title" value={createTitle} onChange={(e) => setCreateTitle(e.target.value)} />
          <select value={createStatus} onChange={(e) => setCreateStatus(Number(e.target.value) as TaskStatus)}>
            <option value={0}>Backlog</option>
            <option value={1}>Todo</option>
            <option value={2}>In Progress</option>
            <option value={3}>Done</option>
          </select>
          <select value={createPriority} onChange={(e) => setCreatePriority(Number(e.target.value) as TaskPriority)}>
            <option value={0}>Low</option>
            <option value={1}>Medium</option>
            <option value={2}>High</option>
            <option value={3}>Urgent</option>
          </select>
          <input type="date" value={createDueDate} onChange={(e) => setCreateDueDate(e.target.value)} />
          <button className="button primary" type="button" onClick={onCreateTask} disabled={mutationLoading}>
            Add
          </button>
        </div>
        <div className="row gap" style={{ marginTop: "0.75rem" }}>
          <input
            style={{ flex: 1 }}
            placeholder="Description (optional)"
            value={createDescription}
            onChange={(e) => setCreateDescription(e.target.value)}
          />
        </div>
      </section>

      <section className="panel">
        <div className="toolbar">
          <div className="stack" style={{ gap: "0.25rem" }}>
            <h2 style={{ margin: 0 }}>Tasks</h2>
            <span className="muted small">
              {totalCount} total • Page {page} of {totalPages}
            </span>
          </div>
        </div>

        {error ? <div className="error-banner">{error}</div> : null}

        {loading ? (
          <div className="muted" style={{ padding: "1rem 0" }}>
            Loading…
          </div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Title</th>
                <th style={{ width: 140 }}>Status</th>
                <th style={{ width: 140 }}>Priority</th>
                <th style={{ width: 160 }}>Due</th>
                <th style={{ width: 220 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {tasks.length === 0 ? (
                <tr>
                  <td colSpan={5} className="muted">
                    No tasks found.
                  </td>
                </tr>
              ) : null}

              {tasks.map((t) => (
                <tr key={t.id}>
                  <td>
                    {editingTaskId === t.id ? (
                      <input value={editTitle} onChange={(e) => setEditTitle(e.target.value)} style={{ width: "100%" }} />
                    ) : (
                      <div style={{ display: "flex", flexDirection: "column", gap: "0.15rem" }}>
                        <strong>{t.title}</strong>
                        {t.description ? <span className="muted small">{t.description}</span> : null}
                      </div>
                    )}
                  </td>
                  <td>
                    {editingTaskId === t.id ? (
                      <select value={editStatus} onChange={(e) => setEditStatus(Number(e.target.value) as TaskStatus)}>
                        <option value={0}>Backlog</option>
                        <option value={1}>Todo</option>
                        <option value={2}>In Progress</option>
                        <option value={3}>Done</option>
                      </select>
                    ) : (
                      <span className="tag">{statusLabel[t.status] ?? "—"}</span>
                    )}
                  </td>
                  <td>
                    {editingTaskId === t.id ? (
                      <select value={editPriority} onChange={(e) => setEditPriority(Number(e.target.value) as TaskPriority)}>
                        <option value={0}>Low</option>
                        <option value={1}>Medium</option>
                        <option value={2}>High</option>
                        <option value={3}>Urgent</option>
                      </select>
                    ) : (
                      <span className="tag">{priorityLabel[t.priority] ?? "—"}</span>
                    )}
                  </td>
                  <td>
                    {editingTaskId === t.id ? (
                      <input type="date" value={editDueDate} onChange={(e) => setEditDueDate(e.target.value)} />
                    ) : (
                      <span className="muted small">{toDateInputValue(t.dueDateUtc) || "—"}</span>
                    )}
                  </td>
                  <td>
                    {editingTaskId === t.id ? (
                      <div className="actions">
                        <input
                          style={{ minWidth: 240 }}
                          placeholder="Description"
                          value={editDescription}
                          onChange={(e) => setEditDescription(e.target.value)}
                        />
                        <div className="row gap" style={{ justifyContent: "flex-end", marginTop: "0.5rem" }}>
                          <button className="button primary" type="button" onClick={onSaveEdit} disabled={mutationLoading}>
                            Save
                          </button>
                          <button className="button" type="button" onClick={() => setEditingTaskId(null)} disabled={mutationLoading}>
                            Cancel
                          </button>
                          <button className="button" type="button" onClick={() => onDeleteTask(t.id)} disabled={mutationLoading}>
                            Delete
                          </button>
                        </div>
                      </div>
                    ) : (
                      <div className="actions">
                        <button className="button" type="button" onClick={() => startEdit(t)} disabled={mutationLoading}>
                          Edit
                        </button>
                        <button className="button" type="button" onClick={() => onDeleteTask(t.id)} disabled={mutationLoading}>
                          Delete
                        </button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

        <div className="row gap" style={{ justifyContent: "space-between", marginTop: "1rem" }}>
          <button className="button" type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
            Prev
          </button>
          <button className="button" type="button" disabled={page >= totalPages} onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>
            Next
          </button>
        </div>
      </section>
    </div>
  );
}

