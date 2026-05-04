import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { format, isPast } from 'date-fns';
import { AnimatePresence } from 'framer-motion';
import { ChevronDown, ChevronUp, Lock, MoreHorizontal, Plus } from 'lucide-react';
import { useState } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import api from '../../lib/api';
import { Avatar } from '../../components/ui/Avatar';
import { BulkActionBar } from '../../components/ui/BulkActionBar';
import { Button } from '../../components/ui/Button';
import { FilterChips } from '../../components/ui/FilterChips';
import { Input } from '../../components/ui/Input';
import { Pagination } from '../../components/ui/Pagination';
import { Select } from '../../components/ui/Select';
import { ProjectSubNav } from '../../components/projects/ProjectSubNav';
import { CreateTaskModal } from '../../components/tasks/CreateTaskModal';
import { TaskDetailSlideOver } from '../../components/tasks/TaskDetailSlideOver';
import { useDebounce } from '../../hooks/useDebounce';
import { useMilestones } from '../../hooks/api/projects.hooks';
import { useTags } from '../../hooks/api/tags.hooks';
import { useBulkAssignTasks, useBulkDeleteTasks, useBulkUpdateTasks, useDeleteTask, useTasks } from '../../hooks/api/tasks.hooks';
import { useMembers } from '../../hooks/api/workspace.hooks';
import { PriorityColor, TaskPriority, TaskPriorityLabel, TaskStatus, TaskStatusColor, TaskStatusLabel } from '../../types/api';
import { cn } from '../../lib/utils';

const statusByName: Record<string, TaskStatus> = {
  Backlog: TaskStatus.Backlog,
  Todo: TaskStatus.Todo,
  InProgress: TaskStatus.InProgress,
  Done: TaskStatus.Done,
  Cancelled: TaskStatus.Cancelled,
};

const priorityByName: Record<string, TaskPriority> = {
  None: TaskPriority.None,
  Low: TaskPriority.Low,
  Medium: TaskPriority.Medium,
  High: TaskPriority.High,
};

function dueClass(dueDateUtc: string | null) {
  if (!dueDateUtc) return 'text-neutral-400';
  const date = new Date(dueDateUtc);
  const inMs = date.getTime() - Date.now();
  if (isPast(date)) return 'text-red-600';
  if (inMs < 1000 * 60 * 60 * 24 * 3) return 'text-amber-600';
  return 'text-neutral-500';
}

function SortHeader({
  label,
  active,
  dir,
  onClick,
}: {
  label: string;
  active: boolean;
  dir: 'asc' | 'desc';
  onClick: () => void;
}) {
  return (
    <button type="button" className="inline-flex items-center gap-1" onClick={onClick}>
      {label}
      {active ? (dir === 'asc' ? <ChevronUp className="h-3 w-3" /> : <ChevronDown className="h-3 w-3" />) : null}
    </button>
  );
}

export default function TaskListPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const [selected, setSelected] = useState<string[]>([]);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);

  const getParam = (key: string) => searchParams.get(key) ?? undefined;
  const [searchInput, setSearchInput] = useState(getParam('q') ?? '');
  const debouncedQ = useDebounce(searchInput, 300);

  const setParam = (key: string, value?: string) => {
    const next = new URLSearchParams(searchParams);
    if (!value) next.delete(key);
    else next.set(key, value);
    if (key !== 'page') next.set('page', '1');
    setSearchParams(next);
  };

  const params = {
    page: Number(searchParams.get('page') ?? 1),
    pageSize: 20,
    projectId,
    status: getParam('status') ? statusByName[getParam('status')!] : undefined,
    priority: getParam('priority') ? priorityByName[getParam('priority')!] : undefined,
    q: getParam('q'),
    sortBy: getParam('sortBy'),
    sortDir: (getParam('sortDir') ?? 'desc') as 'asc' | 'desc',
    assignedToMe: searchParams.get('assignedToMe') === 'true' ? true : undefined,
    assigneeId: getParam('assigneeId'),
    tagId: getParam('tagId'),
    milestoneId: getParam('milestoneId'),
    isBlocked: searchParams.get('isBlocked') === 'true' ? true : undefined,
  };

  const { data, isLoading } = useTasks(params);
  const { data: members } = useMembers({ page: 1, pageSize: 100 });
  const { data: tags } = useTags();
  const { data: milestones } = useMilestones(projectId ?? null);
  const bulkUpdate = useBulkUpdateTasks();
  const bulkDelete = useBulkDeleteTasks();
  const bulkAssign = useBulkAssignTasks();
  const deleteTask = useDeleteTask();

  const items = data?.items ?? [];
  const allChecked = items.length > 0 && items.every((task) => selected.includes(task.id));

  const activeChips = [
    getParam('status') ? { key: 'status', label: `Status: ${getParam('status')}` } : null,
    getParam('priority') ? { key: 'priority', label: `Priority: ${getParam('priority')}` } : null,
    getParam('tagId') ? { key: 'tagId', label: `Tag: ${(tags ?? []).find((t) => t.id === getParam('tagId'))?.name ?? 'Selected'}` } : null,
    getParam('assigneeId')
      ? { key: 'assigneeId', label: `Assignee: ${(members?.items ?? []).find((m) => m.id === getParam('assigneeId'))?.displayName ?? 'Selected'}` }
      : null,
  ].filter(Boolean) as Array<{ key: string; label: string }>;

  const onSortToggle = (sortBy: string) => {
    const currentSortBy = getParam('sortBy');
    const currentDir = (getParam('sortDir') ?? 'desc') as 'asc' | 'desc';
    if (currentSortBy === sortBy) {
      setParam('sortDir', currentDir === 'asc' ? 'desc' : 'asc');
      return;
    }
    setParam('sortBy', sortBy);
    setParam('sortDir', 'desc');
  };

  const handleExport = async () => {
    try {
      const response = await api.get('/Tasks/export', { params: { ...params, format: 'csv' }, responseType: 'blob' });
      const url = URL.createObjectURL(new Blob([response.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `tasks-${format(new Date(), 'yyyyMMdd')}.csv`;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error('Failed to export tasks');
    }
  };

  const handleBulkStatus = async (statusValue: string) => {
    if (!selected.length) return;
    const status = Number(statusValue) as TaskStatus;
    const res = await bulkUpdate.mutateAsync({ taskIds: selected, patch: { status } });
    toast.success(`Updated ${res.succeeded} tasks`);
    if (res.failed.length > 0) toast.error(`${res.failed.length} tasks could not be updated`);
  };

  const handleBulkDelete = async () => {
    if (!selected.length) return;
    const res = await bulkDelete.mutateAsync({ taskIds: selected });
    toast.success(`Deleted ${res.deleted} tasks`);
    if (res.notFound.length > 0) toast.error(`${res.notFound.length} tasks not found`);
    setSelected([]);
  };

  const handleBulkAssign = async (assigneeId: string) => {
    if (!selected.length) return;
    const res = await bulkAssign.mutateAsync({ taskIds: selected, assigneeId: assigneeId || null });
    toast.success(`Updated ${res.succeeded} tasks`);
    if (res.failed.length > 0) toast.error(`${res.failed.length} tasks could not be assigned`);
  };

  const sortBy = getParam('sortBy');
  const sortDir = (getParam('sortDir') ?? 'desc') as 'asc' | 'desc';

  return (
    <div className="page-wrapper">
      <ProjectSubNav projectId={projectId ?? ''} activeTab="list" />

      <div className="mb-3 flex h-10 items-center gap-2">
        <div className="w-48">
          <Input
            placeholder="Search tasks..."
            value={searchInput}
            onChange={(event) => {
              const value = event.target.value;
              setSearchInput(value);
              setParam('q', value || undefined);
            }}
          />
        </div>
        <Select
          className="w-36"
          value={getParam('status') ?? ''}
          onChange={(value) => setParam('status', value || undefined)}
          options={[
            { label: 'All status', value: '' },
            { label: 'Backlog', value: 'Backlog' },
            { label: 'To Do', value: 'Todo' },
            { label: 'In Progress', value: 'InProgress' },
            { label: 'Done', value: 'Done' },
            { label: 'Cancelled', value: 'Cancelled' },
          ]}
        />
        <Select
          className="w-36"
          value={getParam('priority') ?? ''}
          onChange={(value) => setParam('priority', value || undefined)}
          options={[
            { label: 'All priority', value: '' },
            { label: 'None', value: 'None' },
            { label: 'Low', value: 'Low' },
            { label: 'Medium', value: 'Medium' },
            { label: 'High', value: 'High' },
          ]}
        />
        <Select
          className="w-40"
          value={getParam('assigneeId') ?? ''}
          onChange={(value) => setParam('assigneeId', value || undefined)}
          options={[{ label: 'All assignees', value: '' }, ...((members?.items ?? []).map((m) => ({ label: m.displayName ?? m.userName, value: m.id })))]}
        />
        <Select
          className="w-36"
          value={getParam('tagId') ?? ''}
          onChange={(value) => setParam('tagId', value || undefined)}
          options={[{ label: 'All tags', value: '' }, ...((tags ?? []).map((tag) => ({ label: tag.name, value: tag.id })))]}
        />
        <Select
          className="w-40"
          value={getParam('milestoneId') ?? ''}
          onChange={(value) => setParam('milestoneId', value || undefined)}
          options={[{ label: 'All milestones', value: '' }, ...((milestones ?? []).map((m) => ({ label: m.name, value: m.id })))]}
        />
        <Button
          size="sm"
          variant={searchParams.get('assignedToMe') === 'true' ? 'primary' : 'secondary'}
          onClick={() => setParam('assignedToMe', searchParams.get('assignedToMe') === 'true' ? undefined : 'true')}
        >
          My tasks
        </Button>
        <div className="flex-1" />
        <Button size="sm" variant="ghost" onClick={handleExport}>
          Export ↓
        </Button>
        <Button size="sm" variant="primary" leftIcon={<Plus className="h-3.5 w-3.5" />} onClick={() => setCreateOpen(true)}>
          Add task
        </Button>
      </div>

      {debouncedQ !== getParam('q') ? null : activeChips.length > 0 ? (
        <FilterChips chips={activeChips.map((chip) => ({ label: `${chip.label} ×`, onRemove: () => setParam(chip.key, undefined) }))} onClearAll={() => {
          setParam('status', undefined);
          setParam('priority', undefined);
          setParam('tagId', undefined);
          setParam('assigneeId', undefined);
          setParam('assignedToMe', undefined);
          setParam('isBlocked', undefined);
          setParam('milestoneId', undefined);
        }} />
      ) : null}

      <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
        <table className="w-full border-collapse text-13">
          <thead className="border-b border-neutral-200 bg-neutral-50">
            <tr>
              <th className="h-9 w-10 px-3 text-left">
                <input
                  type="checkbox"
                  checked={allChecked}
                  onChange={(event) => {
                    if (event.target.checked) setSelected((prev) => [...new Set([...prev, ...items.map((task) => task.id)])]);
                    else setSelected((prev) => prev.filter((id) => !items.find((task) => task.id === id)));
                  }}
                />
              </th>
              <th className="h-9 px-3 text-left text-11 font-semibold uppercase text-neutral-500">
                <SortHeader label="Title" active={sortBy === 'title'} dir={sortDir} onClick={() => onSortToggle('title')} />
              </th>
              <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Status</th>
              <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Priority</th>
              <th className="h-9 w-52 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Assignee</th>
              <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase text-neutral-500">
                <SortHeader label="Due Date" active={sortBy === 'dueDateUtc'} dir={sortDir} onClick={() => onSortToggle('dueDateUtc')} />
              </th>
              <th className="h-9 w-40 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Tags</th>
              <th className="h-9 w-12 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            {isLoading
              ? Array.from({ length: 10 }).map((_, index) => (
                  <tr key={index} className="h-9 border-b border-neutral-100">
                    <td className="px-3"><div className="h-4 w-4 animate-pulse rounded bg-neutral-200" /></td>
                    <td className="px-3"><div className="h-4 w-56 animate-pulse rounded bg-neutral-200" /></td>
                    <td className="px-3"><div className="h-4 w-20 animate-pulse rounded bg-neutral-200" /></td>
                    <td className="px-3"><div className="h-4 w-16 animate-pulse rounded bg-neutral-200" /></td>
                    <td className="px-3"><div className="h-4 w-28 animate-pulse rounded bg-neutral-200" /></td>
                    <td className="px-3"><div className="h-4 w-16 animate-pulse rounded bg-neutral-200" /></td>
                    <td className="px-3"><div className="h-4 w-20 animate-pulse rounded bg-neutral-200" /></td>
                    <td className="px-3"><div className="h-4 w-4 animate-pulse rounded bg-neutral-200" /></td>
                  </tr>
                ))
              : items.map((task) => (
                  <tr key={task.id} className="group h-9 border-b border-neutral-100 hover:bg-neutral-50">
                    <td className="px-3">
                      <input
                        type="checkbox"
                        checked={selected.includes(task.id)}
                        onChange={() => setSelected((prev) => (prev.includes(task.id) ? prev.filter((id) => id !== task.id) : [...prev, task.id]))}
                      />
                    </td>
                    <td className="px-3">
                      <button type="button" className="inline-flex max-w-[480px] items-center gap-1 truncate font-medium text-neutral-800 hover:text-primary-700" onClick={() => setSelectedTaskId(task.id)}>
                        {task.title}
                        {task.isBlocked ? <Lock className="h-3.5 w-3.5 text-orange-500" /> : null}
                      </button>
                    </td>
                    <td className="px-3">
                      <span className="inline-flex rounded px-2 py-0.5 text-11" style={{ color: TaskStatusColor[task.status], backgroundColor: `${TaskStatusColor[task.status]}22` }}>
                        {TaskStatusLabel[task.status]}
                      </span>
                    </td>
                    <td className="px-3">
                      <span className="inline-flex items-center gap-1 text-12 text-neutral-600">
                        <span className="h-2 w-2 rounded-full" style={{ backgroundColor: PriorityColor[task.priority] }} />
                        {TaskPriorityLabel[task.priority]}
                      </span>
                    </td>
                    <td className="px-3">
                      {task.assignee ? (
                        <span className="inline-flex items-center gap-1.5">
                          <Avatar size="xs" name={task.assignee.displayName ?? task.assignee.userName} />
                          <span className="truncate text-12 text-neutral-700">{task.assignee.displayName ?? task.assignee.userName}</span>
                        </span>
                      ) : (
                        <span className="text-12 text-neutral-400">—</span>
                      )}
                    </td>
                    <td className={cn('px-3 text-12', dueClass(task.dueDateUtc))}>{task.dueDateUtc ? format(new Date(task.dueDateUtc), 'MMM d') : '—'}</td>
                    <td className="px-3">
                      <div className="flex items-center gap-1">
                        {task.tags.slice(0, 2).map((tag) => (
                          <span key={tag.id} className="rounded px-1.5 py-0.5 text-10 text-white" style={{ backgroundColor: tag.color }}>
                            {tag.name}
                          </span>
                        ))}
                        {task.tags.length > 2 ? <span className="text-11 text-neutral-500">+{task.tags.length - 2}</span> : null}
                      </div>
                    </td>
                    <td className="px-3">
                      <DropdownMenu.Root>
                        <DropdownMenu.Trigger asChild>
                          <button type="button" className="opacity-0 transition-opacity group-hover:opacity-100">
                            <MoreHorizontal className="h-4 w-4 text-neutral-500" />
                          </button>
                        </DropdownMenu.Trigger>
                        <DropdownMenu.Portal>
                          <DropdownMenu.Content sideOffset={6} align="end" className="z-50 min-w-[140px] rounded-md border border-neutral-200 bg-white py-1 shadow-e200">
                            <DropdownMenu.Item onSelect={() => setSelectedTaskId(task.id)} className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50">
                              View
                            </DropdownMenu.Item>
                            <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50">
                              Edit
                            </DropdownMenu.Item>
                            <DropdownMenu.Item
                              onSelect={() =>
                                deleteTask.mutate(task.id, {
                                  onSuccess: () => toast.success('Task deleted'),
                                  onError: () => toast.error('Failed to delete task'),
                                })
                              }
                              className="cursor-pointer px-3 py-2 text-13 text-red-600 outline-none data-[highlighted]:bg-red-50"
                            >
                              Delete
                            </DropdownMenu.Item>
                          </DropdownMenu.Content>
                        </DropdownMenu.Portal>
                      </DropdownMenu.Root>
                    </td>
                  </tr>
                ))}
          </tbody>
        </table>
      </div>

      {data ? (
        <Pagination page={data.page} pageSize={data.pageSize} totalCount={data.totalCount} onPageChange={(nextPage) => setParam('page', String(nextPage))} className="mt-3" />
      ) : null}

      <AnimatePresence>
        {selected.length > 0 ? (
          <BulkActionBar
            selectedCount={selected.length}
            onDeselect={() => setSelected([])}
            bulkActions={
              <>
                <Select
                  className="w-36"
                  triggerClassName="h-7 border-white/20 bg-neutral-700 text-white"
                  value=""
                  onChange={handleBulkStatus}
                  options={[{ label: 'Change status', value: '' }, ...Object.entries(TaskStatusLabel).map(([value, label]) => ({ label, value }))]}
                />
                <Select
                  className="w-40"
                  triggerClassName="h-7 border-white/20 bg-neutral-700 text-white"
                  value=""
                  onChange={handleBulkAssign}
                  options={[{ label: 'Assign', value: '' }, ...(members?.items ?? []).map((m) => ({ label: m.displayName ?? m.userName, value: m.id }))]}
                />
                <Button size="xs" variant="danger-ghost" onClick={handleBulkDelete}>
                  Delete
                </Button>
              </>
            }
          />
        ) : null}
      </AnimatePresence>

      <CreateTaskModal open={createOpen} onOpenChange={setCreateOpen} projectId={projectId ?? ''} />
      {selectedTaskId ? <TaskDetailSlideOver taskId={selectedTaskId} onClose={() => setSelectedTaskId(null)} /> : null}
    </div>
  );
}
