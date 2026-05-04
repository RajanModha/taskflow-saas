import { Info, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { ConfirmDialog } from '../components/shared/ConfirmDialog';
import { StatusChip } from '../components/shared/StatusChip';
import { Button } from '../components/ui/Button';
import { Pagination } from '../components/ui/Pagination';
import { Skeleton } from '../components/ui/Skeleton';
import { useMe } from '../hooks/api/auth.hooks';
import { useDeletedTasks, usePermanentDeleteTask, useRestoreTask } from '../hooks/api/tasks.hooks';
import { formatRelative } from '../lib/formatters';

export default function TrashPage() {
  const { data: me } = useMe();
  const isAdmin = ['Owner', 'Admin'].includes(me?.role ?? '');
  const [page, setPage] = useState(1);
  const [confirmPermanent, setConfirmPermanent] = useState<string | null>(null);
  const { data, isLoading } = useDeletedTasks({ page, pageSize: 20 });
  const restoreTask = useRestoreTask();
  const permanentDelete = usePermanentDeleteTask();

  const deletedItems = data?.items?.filter((task) => task.isDeleted) ?? [];

  return (
    <>
      <div className="page-wrapper">
        <div className="page-header">
          <div>
            <h1 className="page-title flex items-center gap-2">
              <Trash2 className="h-5 w-5 text-neutral-400" />
              Trash
            </h1>
            <p className="page-subtitle">Soft-deleted tasks — restore or permanently remove them</p>
          </div>
        </div>

        <div className="mb-4 flex items-center gap-3 rounded-md border border-amber-200 bg-amber-50 px-4 py-3">
          <Info className="h-4 w-4 flex-shrink-0 text-amber-600" />
          <p className="text-13 text-amber-700">
            Deleted tasks are kept here for recovery. Permanently deleted tasks cannot be restored.
          </p>
        </div>

        <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
          <table className="w-full border-collapse text-13">
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50">
                <th className="h-9 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Task</th>
                <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Status</th>
                <th className="h-9 w-36 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Deleted</th>
                <th className="h-9 w-32 px-3" />
              </tr>
            </thead>
            <tbody>
              {isLoading
                ? Array.from({ length: 5 }).map((_, i) => (
                    <tr key={i} className="h-9 border-b">
                      <td colSpan={4} className="px-3">
                        <Skeleton className="h-3 w-full" />
                      </td>
                    </tr>
                  ))
                : deletedItems.map((task) => (
                    <tr key={task.id} className="h-10 border-b border-neutral-100 hover:bg-neutral-50">
                      <td className="px-3">
                        <div className="flex items-center gap-2">
                          <span className="truncate text-13 font-medium text-neutral-500 line-through">{task.title}</span>
                        </div>
                      </td>
                      <td className="px-3">
                        <StatusChip status={task.status} readOnly size="sm" />
                      </td>
                      <td className="px-3 text-12 text-neutral-400">
                        {task.deletedAt ? formatRelative(task.deletedAt) : '—'}
                      </td>
                      <td className="px-3">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            loading={restoreTask.isPending}
                            onClick={() => restoreTask.mutate(task.id)}
                          >
                            Restore
                          </Button>
                          {isAdmin ? (
                            <Button variant="danger-ghost" size="sm" onClick={() => setConfirmPermanent(task.id)}>
                              Delete forever
                            </Button>
                          ) : null}
                        </div>
                      </td>
                    </tr>
                  ))}
            </tbody>
          </table>

          {deletedItems.length === 0 && !isLoading ? (
            <div className="py-16 text-center">
              <Trash2 className="mx-auto mb-3 h-10 w-10 text-neutral-200" />
              <p className="text-14 font-medium text-neutral-500">Trash is empty</p>
              <p className="mt-1 text-12 text-neutral-400">Deleted tasks will appear here.</p>
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
      </div>

      <ConfirmDialog
        open={Boolean(confirmPermanent)}
        onOpenChange={() => setConfirmPermanent(null)}
        title="Permanently delete task?"
        description="This task will be deleted forever and cannot be recovered. This action is irreversible."
        confirmLabel="Delete forever"
        variant="danger"
        loading={permanentDelete.isPending}
        onConfirm={() => {
          if (!confirmPermanent) return;
          permanentDelete.mutate(confirmPermanent, {
            onSuccess: () => setConfirmPermanent(null),
          });
        }}
      />
    </>
  );
}
