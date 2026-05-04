import { useMutation, useQueryClient } from '@tanstack/react-query';
import { AlertCircle, AlertTriangle, CheckCircle, Download, Eye } from 'lucide-react';
import { useState } from 'react';
import toast from 'react-hot-toast';
import { RowMenu } from '../components/shared/RowMenu';
import { StatusChip } from '../components/shared/StatusChip';
import { Avatar } from '../components/ui/Avatar';
import { BulkActionBar } from '../components/ui/BulkActionBar';
import { Button } from '../components/ui/Button';
import { Pagination } from '../components/ui/Pagination';
import { Skeleton } from '../components/ui/Skeleton';
import { TaskDetailSlideOver } from '../components/tasks/TaskDetailSlideOver';
import { useBulkDeleteTasks, useBulkUpdateTasks, useOverdueTasks } from '../hooks/api/tasks.hooks';
import api from '../lib/api';
import { formatDate, formatRelative } from '../lib/formatters';
import { PriorityColor, TaskPriorityLabel, TaskStatus } from '../types/api';

export default function OverduePage() {
  const [page, setPage] = useState(1);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);
  const [selected, setSelected] = useState<string[]>([]);
  const { data, isLoading } = useOverdueTasks({ page, pageSize: 20, sortBy: 'dueDateUtc', sortDir: 'asc' });
  const bulkUpdate = useBulkUpdateTasks();
  const bulkDelete = useBulkDeleteTasks();
  const queryClient = useQueryClient();

  const patchTask = useMutation({
    mutationFn: async (taskId: string) => {
      await api.patch(`/Tasks/${taskId}`, { status: TaskStatus.Done });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] });
      queryClient.invalidateQueries({ queryKey: ['board'] });
      toast.success('Task marked done');
    },
    onError: () => toast.error('Failed to update task'),
  });

  const rows = data?.items ?? [];
  const allSelected = selected.length === rows.length && selected.length > 0;

  const onExport = async () => {
    try {
      const res = await api.get('/Tasks/overdue', {
        params: { page: 1, pageSize: 10000 },
        responseType: 'blob',
      });
      const url = URL.createObjectURL(new Blob([res.data]));
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'overdue-tasks.csv';
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error('Failed to export overdue tasks');
    }
  };

  const onMarkAllDone = async () => {
    if (!selected.length) return;
    const res = await bulkUpdate.mutateAsync({ taskIds: selected, patch: { status: TaskStatus.Done } });
    toast.success(`Updated ${res.succeeded} tasks`);
    setSelected([]);
  };

  const onBulkDelete = async () => {
    if (!selected.length) return;
    const res = await bulkDelete.mutateAsync({ taskIds: selected });
    toast.success(`Deleted ${res.deleted} tasks`);
    setSelected([]);
  };

  return (
    <div className="page-wrapper">
      <div className="page-header">
        <div>
          <h1 className="page-title flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-red-500" />
            Overdue Tasks
          </h1>
          <p className="page-subtitle">{data?.totalCount ?? 0} tasks past their due date</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="sm" leftIcon={<Download className="h-3.5 w-3.5" />} onClick={onExport}>
            Export
          </Button>
        </div>
      </div>

      {(data?.totalCount ?? 0) > 10 ? (
        <div className="mb-4 flex items-center gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3">
          <AlertTriangle className="h-4 w-4 flex-shrink-0 text-red-600" />
          <p className="text-13 text-red-700">
            You have <strong>{data?.totalCount}</strong> overdue tasks. Consider bulk-updating their due dates or status.
          </p>
        </div>
      ) : null}

      <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
        <table className="w-full border-collapse text-13">
          <thead>
            <tr className="h-9 border-b border-neutral-200 bg-neutral-50">
              <th className="w-8 px-3">
                <input
                  type="checkbox"
                  checked={allSelected}
                  onChange={(event) => setSelected(event.target.checked ? rows.map((task) => task.id) : [])}
                  className="h-3.5 w-3.5 rounded border-neutral-300 text-primary-600"
                />
              </th>
              <th className="h-9 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Title</th>
              <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Status</th>
              <th className="h-9 w-24 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Priority</th>
              <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Project</th>
              <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Was due</th>
              <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Assignee</th>
              <th className="w-10 px-3" />
            </tr>
          </thead>
          <tbody>
            {isLoading
              ? Array.from({ length: 8 }).map((_, i) => (
                  <tr key={i} className="h-9 border-b border-neutral-100">
                    {Array.from({ length: 8 }).map((__, j) => (
                      <td key={`${i}-${j}`} className="px-3">
                        <Skeleton className="h-3 w-full" />
                      </td>
                    ))}
                  </tr>
                ))
              : rows.map((task) => (
                  <tr
                    key={task.id}
                    className="group h-9 cursor-pointer border-b border-neutral-100 hover:bg-red-50/40"
                    onClick={() => setSelectedTaskId(task.id)}
                  >
                    <td className="w-8 px-3" onClick={(event) => event.stopPropagation()}>
                      <input
                        type="checkbox"
                        checked={selected.includes(task.id)}
                        className="h-3.5 w-3.5 rounded border-neutral-300 text-primary-600"
                        onChange={(event) =>
                          setSelected((current) =>
                            event.target.checked ? [...current, task.id] : current.filter((id) => id !== task.id),
                          )
                        }
                      />
                    </td>
                    <td className="max-w-0 w-full px-3">
                      <span className="block truncate text-13 font-medium text-neutral-800">{task.title}</span>
                    </td>
                    <td className="px-3">
                      <StatusChip status={task.status} readOnly size="sm" />
                    </td>
                    <td className="px-3">
                      <span className="flex items-center gap-1.5 text-12">
                        <span className="h-2 w-2 rounded-full" style={{ backgroundColor: PriorityColor[task.priority] }} />
                        {TaskPriorityLabel[task.priority]}
                      </span>
                    </td>
                    <td className="px-3 text-12 text-neutral-500">—</td>
                    <td className="px-3">
                      <span className="text-12 font-medium text-red-600">
                        {formatDate(task.dueDateUtc, 'MMM d')}
                        <span className="ml-1 text-11 text-red-400">({formatRelative(task.dueDateUtc)})</span>
                      </span>
                    </td>
                    <td className="px-3">
                      {task.assignee ? (
                        <div className="flex items-center gap-1.5 text-12 text-neutral-700">
                          <Avatar name={task.assignee.displayName ?? task.assignee.userName} size="xs" />
                          {task.assignee.displayName ?? task.assignee.userName}
                        </div>
                      ) : (
                        <span className="text-12 text-neutral-300">—</span>
                      )}
                    </td>
                    <td className="w-10 px-3" onClick={(event) => event.stopPropagation()}>
                      <RowMenu
                        items={[
                          { label: 'View', icon: Eye, onClick: () => setSelectedTaskId(task.id) },
                          { label: 'Mark done', icon: CheckCircle, onClick: () => patchTask.mutate(task.id) },
                        ]}
                      />
                    </td>
                  </tr>
                ))}
          </tbody>
        </table>
        {rows.length === 0 && !isLoading ? (
          <div className="py-12 text-center">
            <CheckCircle className="mx-auto mb-3 h-10 w-10 text-green-400" />
            <p className="text-14 font-medium text-neutral-600">No overdue tasks!</p>
            <p className="mt-1 text-13 text-neutral-400">Everything is on track.</p>
          </div>
        ) : null}
      </div>

      <Pagination
        page={page}
        pageSize={20}
        totalCount={data?.totalCount ?? 0}
        onPageChange={setPage}
        className="mt-3"
      />

      <BulkActionBar
        selectedCount={selected.length}
        onDeselect={() => setSelected([])}
        bulkActions={
          <>
            <Button variant="ghost" size="sm" className="text-white" onClick={onMarkAllDone}>
              Mark all Done
            </Button>
            <Button variant="ghost" size="sm" className="text-red-300" onClick={onBulkDelete}>
              Delete
            </Button>
          </>
        }
      />

      {selectedTaskId ? <TaskDetailSlideOver taskId={selectedTaskId} onClose={() => setSelectedTaskId(null)} /> : null}
    </div>
  );
}
