import { CheckSquare, ChevronUp, ChevronsUpDown, Plus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { BulkActionBar } from '../../components/ui/BulkActionBar';
import { Button } from '../../components/ui/Button';
import { EmptyState } from '../../components/ui/EmptyState';
import { FilterChips } from '../../components/ui/FilterChips';
import { Pagination } from '../../components/ui/Pagination';
import { Select } from '../../components/ui/Select';
import { Toolbar } from '../../components/ui/Toolbar';
import { cn } from '../../lib/utils';
import { Badge } from '../../components/ui/Badge';
import { Avatar } from '../../components/ui/Avatar';
import { ProjectSubNav } from '../../components/projects/ProjectSubNav';

type TaskStatus = 'todo' | 'progress' | 'done' | 'cancelled';
type TaskPriority = 'high' | 'medium' | 'low' | 'none';

type Task = {
  id: string;
  title: string;
  status: TaskStatus;
  priority: TaskPriority;
  assignee?: string;
  dueDate?: string;
  tags: string[];
};

type SortKey = 'title' | 'status' | 'priority' | 'dueDate';

const TASKS: Task[] = [
  { id: 't1', title: 'Implement dense project list row interactions', status: 'progress', priority: 'high', assignee: 'Alex Johnson', dueDate: '2026-04-28', tags: ['frontend', 'ux'] },
  { id: 't2', title: 'Write API contract tests for project filters', status: 'todo', priority: 'medium', assignee: 'Priya Sharma', dueDate: '2026-04-30', tags: ['backend'] },
  { id: 't3', title: 'Polish dashboard velocity chart tooltip spacing', status: 'done', priority: 'low', assignee: 'Chris Lee', dueDate: '2026-04-26', tags: ['dashboard'] },
  { id: 't4', title: 'Audit a11y labels for bulk action controls', status: 'progress', priority: 'medium', assignee: 'Nina Patel', dueDate: '2026-05-01', tags: ['a11y'] },
  { id: 't5', title: 'Cleanup unused legacy list card wrappers', status: 'cancelled', priority: 'none', assignee: 'Sam Wilson', dueDate: undefined, tags: ['refactor'] },
  { id: 't6', title: 'Add keyboard sorting affordances in list tables', status: 'todo', priority: 'high', assignee: 'Alex Johnson', dueDate: '2026-05-02', tags: ['frontend', 'table'] },
  { id: 't7', title: 'Create compact status lozenges for tasks', status: 'done', priority: 'low', assignee: 'Priya Sharma', dueDate: '2026-04-27', tags: ['design-system'] },
  { id: 't8', title: 'Improve empty state copy for task list', status: 'progress', priority: 'none', assignee: undefined, dueDate: undefined, tags: ['ux'] },
  { id: 't9', title: 'Tune topbar search trigger visual hierarchy', status: 'todo', priority: 'medium', assignee: 'Chris Lee', dueDate: '2026-05-03', tags: ['ui'] },
  { id: 't10', title: 'Document table density usage rules', status: 'done', priority: 'low', assignee: 'Nina Patel', dueDate: '2026-04-24', tags: ['docs'] },
];

function formatDue(d?: string) {
  if (!d) return 'No due date';
  return new Date(d).toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

function compareValues(a: Task, b: Task, sortBy: SortKey, dir: 'asc' | 'desc') {
  const factor = dir === 'asc' ? 1 : -1;
  const valueA = sortBy === 'dueDate' ? a.dueDate ?? '9999-12-31' : String(a[sortBy] ?? '');
  const valueB = sortBy === 'dueDate' ? b.dueDate ?? '9999-12-31' : String(b[sortBy] ?? '');
  return valueA.localeCompare(valueB) * factor;
}

function TaskFilters({
  status,
  setStatus,
  assignee,
  setAssignee,
}: {
  status: string;
  setStatus: (v: string) => void;
  assignee: string;
  setAssignee: (v: string) => void;
}) {
  return (
    <>
      <Select
        className="w-36"
        value={status}
        onChange={setStatus}
        placeholder="Status"
        options={[
          { label: 'All status', value: 'all' },
          { label: 'To Do', value: 'todo' },
          { label: 'In Progress', value: 'progress' },
          { label: 'Done', value: 'done' },
          { label: 'Cancelled', value: 'cancelled' },
        ]}
      />
      <Select
        className="w-40"
        value={assignee}
        onChange={setAssignee}
        placeholder="Assignee"
        options={[
          { label: 'Any assignee', value: 'all' },
          { label: 'Alex Johnson', value: 'Alex Johnson' },
          { label: 'Priya Sharma', value: 'Priya Sharma' },
          { label: 'Chris Lee', value: 'Chris Lee' },
          { label: 'Nina Patel', value: 'Nina Patel' },
        ]}
      />
    </>
  );
}

function SortableHeader({
  label,
  sortKey,
  sortBy,
  sortDir,
  toggleSort,
  width,
  flex,
}: {
  label: string;
  sortKey: SortKey;
  sortBy: SortKey;
  sortDir: 'asc' | 'desc';
  toggleSort: (k: SortKey) => void;
  width?: string;
  flex?: boolean;
}) {
  return (
    <th
      className={cn(
        'h-9 cursor-pointer select-none px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500 hover:bg-neutral-100',
        width,
        flex && 'w-auto',
      )}
      onClick={() => toggleSort(sortKey)}
    >
      <div className="flex items-center gap-1">
        {label}
        {sortBy === sortKey ? (
          <ChevronUp
            className="h-3 w-3 text-primary-600"
            style={{ transform: sortDir === 'desc' ? 'rotate(180deg)' : undefined }}
          />
        ) : (
          <ChevronsUpDown className="h-3 w-3 text-neutral-300" />
        )}
      </div>
    </th>
  );
}

export default function TaskListPage() {
  const { id } = useParams<{ id: string }>();
  const projectId = id ?? 'project';

  const [q, setQ] = useState('');
  const [status, setStatus] = useState('all');
  const [assignee, setAssignee] = useState('all');
  const [sortBy, setSortBy] = useState<SortKey>('dueDate');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [selected, setSelected] = useState<string[]>([]);
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const filtered = useMemo(() => {
    const query = q.trim().toLowerCase();
    return TASKS.filter((t) => {
      const matchQuery = !query || t.title.toLowerCase().includes(query) || t.tags.join(' ').toLowerCase().includes(query);
      const matchStatus = status === 'all' || t.status === status;
      const matchAssignee = assignee === 'all' || t.assignee === assignee;
      return matchQuery && matchStatus && matchAssignee;
    }).sort((a, b) => compareValues(a, b, sortBy, sortDir));
  }, [assignee, q, sortBy, sortDir, status]);

  const activeFilters = [
    status !== 'all'
      ? { label: `Status: ${status}`, onRemove: () => setStatus('all') }
      : null,
    assignee !== 'all'
      ? { label: `Assignee: ${assignee}`, onRemove: () => setAssignee('all') }
      : null,
  ].filter((v): v is { label: string; onRemove: () => void } => Boolean(v));

  const pageItems = useMemo(() => {
    const start = (page - 1) * pageSize;
    return filtered.slice(start, start + pageSize);
  }, [filtered, page]);

  const allOnPageSelected = pageItems.length > 0 && pageItems.every((t) => selected.includes(t.id));

  function toggleSort(key: SortKey) {
    if (sortBy === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
      return;
    }
    setSortBy(key);
    setSortDir('asc');
  }

  function toggleRow(id: string) {
    setSelected((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
  }

  return (
    <div className="page-wrapper">
      <ProjectSubNav projectId={projectId} activeTab="list" />

      <Toolbar
        searchValue={q}
        onSearchChange={(v) => {
          setQ(v);
          setPage(1);
        }}
        searchPlaceholder="Search tasks..."
        filters={<TaskFilters status={status} setStatus={setStatus} assignee={assignee} setAssignee={setAssignee} />}
        selectedCount={selected.length}
        bulkActions={
          selected.length > 0 ? (
            <>
              <Button size="xs" variant="ghost">Assign</Button>
              <Button size="xs" variant="ghost">Move</Button>
            </>
          ) : undefined
        }
        actions={
          <Button size="sm" variant="primary" leftIcon={<Plus className="h-3.5 w-3.5" />}>
            New task
          </Button>
        }
      />

      {activeFilters.length > 0 ? (
        <FilterChips chips={activeFilters} onClearAll={() => { setStatus('all'); setAssignee('all'); }} />
      ) : null}

      <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
        <table className="w-full border-collapse text-13">
          <thead>
            <tr className="border-b border-neutral-200 bg-neutral-50">
              <th className="h-9 w-8 px-3">
                <input
                  type="checkbox"
                  className="rounded border-neutral-300 text-primary-600"
                  checked={allOnPageSelected}
                  onChange={(e) => {
                    if (e.target.checked) {
                      setSelected((prev) => [...new Set([...prev, ...pageItems.map((t) => t.id)])]);
                    } else {
                      setSelected((prev) => prev.filter((id) => !pageItems.some((t) => t.id === id)));
                    }
                  }}
                  aria-label="Select all tasks"
                />
              </th>
              <SortableHeader label="Title" sortKey="title" sortBy={sortBy} sortDir={sortDir} toggleSort={toggleSort} flex />
              <SortableHeader label="Status" sortKey="status" sortBy={sortBy} sortDir={sortDir} toggleSort={toggleSort} width="w-28" />
              <SortableHeader label="Priority" sortKey="priority" sortBy={sortBy} sortDir={sortDir} toggleSort={toggleSort} width="w-24" />
              <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Assignee</th>
              <SortableHeader label="Due" sortKey="dueDate" sortBy={sortBy} sortDir={sortDir} toggleSort={toggleSort} width="w-28" />
              <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Tags</th>
              <th className="h-9 w-10 px-3" />
            </tr>
          </thead>
          <tbody>
            {pageItems.map((task) => (
              <tr key={task.id} className="h-9 border-b border-neutral-100 hover:bg-neutral-50">
                <td className="px-3">
                  <input
                    type="checkbox"
                    className="rounded border-neutral-300 text-primary-600"
                    checked={selected.includes(task.id)}
                    onChange={() => toggleRow(task.id)}
                    aria-label={`Select ${task.title}`}
                  />
                </td>
                <td className="px-3">
                  <span className="block max-w-[36rem] truncate text-neutral-800">{task.title}</span>
                </td>
                <td className="px-3"><Badge status={task.status}>{task.status}</Badge></td>
                <td className="px-3"><Badge priority={task.priority}>{task.priority}</Badge></td>
                <td className="px-3">
                  {task.assignee ? (
                    <div className="flex items-center gap-1.5">
                      <Avatar name={task.assignee} size="xs" />
                      <span className="truncate text-12 text-neutral-700">{task.assignee}</span>
                    </div>
                  ) : (
                    <span className="text-12 text-neutral-400">Unassigned</span>
                  )}
                </td>
                <td className="px-3 text-12 text-neutral-500">{formatDue(task.dueDate)}</td>
                <td className="px-3">
                  <div className="flex flex-wrap gap-1">
                    {task.tags.slice(0, 2).map((tag) => (
                      <span key={tag} className="rounded-sm bg-neutral-100 px-1.5 text-11 text-neutral-600">{tag}</span>
                    ))}
                  </div>
                </td>
                <td className="px-3" />
              </tr>
            ))}
          </tbody>
        </table>

        {pageItems.length === 0 ? (
          <EmptyState icon={CheckSquare} title="No tasks found" description="Try adjusting your filters" size="sm" />
        ) : null}
      </div>

      <Pagination
        page={page}
        pageSize={pageSize}
        totalCount={filtered.length}
        onPageChange={setPage}
        className="mt-3"
      />

      <BulkActionBar
        selectedCount={selected.length}
        onDeselect={() => setSelected([])}
        bulkActions={
          <>
            <Button size="xs" variant="ghost">Complete</Button>
            <Button size="xs" variant="ghost">Change assignee</Button>
            <Button size="xs" variant="danger-ghost">Delete</Button>
          </>
        }
      />
    </div>
  );
}
