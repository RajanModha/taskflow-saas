import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { zodResolver } from '@hookform/resolvers/zod';
import { format, formatDistanceToNow, isPast } from 'date-fns';
import { ChevronDown, Flag, MoreHorizontal, Plus } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { ProjectSubNav } from '../../components/projects/ProjectSubNav';
import { Button } from '../../components/ui/Button';
import { EmptyState } from '../../components/ui/EmptyState';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { useCreateMilestone, useDeleteMilestone, useMilestones, useUpdateMilestone } from '../../hooks/api/projects.hooks';
import { useTasks } from '../../hooks/api/tasks.hooks';

const schema = z.object({
  name: z.string().min(1).max(100),
  description: z.string().optional(),
  dueDateUtc: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

function MilestoneTaskPreview({ milestoneId, projectId }: { milestoneId: string; projectId: string }) {
  const { data } = useTasks({ page: 1, pageSize: 10, projectId, milestoneId });
  const items = data?.items ?? [];

  if (items.length === 0) {
    return <p className="px-3 py-2 text-12 text-neutral-500">No tasks in this milestone.</p>;
  }

  return (
    <div className="border-t border-neutral-100">
      <table className="w-full border-collapse text-12">
        <thead>
          <tr className="bg-neutral-50 text-neutral-500">
            <th className="h-8 px-3 text-left font-medium">Task</th>
            <th className="h-8 w-28 px-3 text-left font-medium">Status</th>
            <th className="h-8 w-28 px-3 text-left font-medium">Updated</th>
          </tr>
        </thead>
        <tbody>
          {items.map((task) => (
            <tr key={task.id} className="border-t border-neutral-100">
              <td className="px-3 py-1.5 text-neutral-700">{task.title}</td>
              <td className="px-3 py-1.5 text-neutral-500">{task.status}</td>
              <td className="px-3 py-1.5 text-neutral-500">{formatDistanceToNow(new Date(task.updatedAtUtc), { addSuffix: true })}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default function MilestonesPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const { data: milestones = [], isLoading } = useMilestones(projectId ?? null);
  const createMilestone = useCreateMilestone(projectId ?? '');
  const updateMilestone = useUpdateMilestone(projectId ?? '');
  const deleteMilestone = useDeleteMilestone(projectId ?? '');

  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', description: '', dueDateUtc: '' },
  });

  const onCreate = () => {
    setEditingId(null);
    form.reset({ name: '', description: '', dueDateUtc: '' });
    setOpen(true);
  };

  const onEdit = (milestone: (typeof milestones)[number]) => {
    setEditingId(milestone.id);
    form.reset({
      name: milestone.name,
      description: milestone.description ?? '',
      dueDateUtc: milestone.dueDateUtc ? format(new Date(milestone.dueDateUtc), 'yyyy-MM-dd') : '',
    });
    setOpen(true);
  };

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      if (!editingId) {
        await createMilestone.mutateAsync({
          name: values.name,
          description: values.description || undefined,
          dueDateUtc: values.dueDateUtc ? new Date(values.dueDateUtc).toISOString() : undefined,
        });
        toast.success('Milestone created');
      } else {
        await updateMilestone.mutateAsync({
          milestoneId: editingId,
          payload: {
            name: values.name,
            description: values.description || undefined,
            dueDateUtc: values.dueDateUtc ? new Date(values.dueDateUtc).toISOString() : undefined,
          },
        });
        toast.success('Milestone updated');
      }
      setOpen(false);
    } catch {
      toast.error('Failed to save milestone');
    }
  });

  return (
    <div className="page-wrapper">
      <ProjectSubNav projectId={projectId ?? ''} activeTab="milestones" />
      <div className="page-header">
        <h1 className="page-title">Milestones</h1>
        <Button size="sm" variant="primary" leftIcon={<Plus className="h-3.5 w-3.5" />} onClick={onCreate}>
          New Milestone
        </Button>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
          {Array.from({ length: 4 }).map((_, index) => (
            <div key={index} className="rounded-md border border-neutral-200 bg-white p-4">
              <div className="h-4 w-40 animate-pulse rounded bg-neutral-200" />
              <div className="mt-2 h-3 w-60 animate-pulse rounded bg-neutral-200" />
              <div className="mt-4 h-2 w-full animate-pulse rounded bg-neutral-200" />
            </div>
          ))}
        </div>
      ) : milestones.length === 0 ? (
        <EmptyState icon={Flag} title="No milestones yet." description="Track deadlines and progress across key project phases." action={{ label: 'Create milestone', onClick: onCreate }} />
      ) : (
        <div className="grid grid-cols-1 gap-3 lg:grid-cols-2">
          {milestones.map((milestone) => {
            const overdue = Boolean(milestone.dueDateUtc && isPast(new Date(milestone.dueDateUtc)) && milestone.progress < 100);
            const fillClass = milestone.progress >= 100 ? 'bg-green-500' : overdue ? 'bg-red-400' : 'bg-primary-500';
            return (
              <div key={milestone.id} className="rounded-md border border-neutral-200 bg-white">
                <div className="p-4">
                  <div className="flex gap-2">
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-14 font-semibold text-neutral-800">{milestone.name}</p>
                      <p className="mt-0.5 line-clamp-2 text-12 text-neutral-500">{milestone.description ?? 'No description'}</p>
                    </div>
                    <DropdownMenu.Root>
                      <DropdownMenu.Trigger asChild>
                        <button type="button" className="flex h-7 w-7 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100">
                          <MoreHorizontal className="h-4 w-4" />
                        </button>
                      </DropdownMenu.Trigger>
                      <DropdownMenu.Portal>
                        <DropdownMenu.Content sideOffset={8} align="end" className="z-50 min-w-[140px] rounded-md border border-neutral-200 bg-white py-1 shadow-e200">
                          <DropdownMenu.Item onSelect={() => onEdit(milestone)} className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50">
                            Edit
                          </DropdownMenu.Item>
                          <DropdownMenu.Item
                            onSelect={() => {
                              const yes = window.confirm(`Delete '${milestone.name}'? Tasks will lose their milestone assignment.`);
                              if (!yes) return;
                              deleteMilestone.mutate(milestone.id, {
                                onSuccess: () => toast.success('Milestone deleted'),
                                onError: () => toast.error('Failed to delete milestone'),
                              });
                            }}
                            className="cursor-pointer px-3 py-2 text-13 text-red-600 outline-none data-[highlighted]:bg-red-50"
                          >
                            Delete
                          </DropdownMenu.Item>
                        </DropdownMenu.Content>
                      </DropdownMenu.Portal>
                    </DropdownMenu.Root>
                  </div>

                  <div className="mt-3">
                    <div className="mb-1 flex items-center justify-between">
                      <p className="text-12 text-neutral-500">
                        {milestone.completedTaskCount} of {milestone.taskCount} tasks
                      </p>
                      <p className="text-11 text-neutral-400">{milestone.progress.toFixed(0)}%</p>
                    </div>
                    <div className="h-1.5 rounded-full bg-neutral-150">
                      <div className={`h-1.5 rounded-full ${fillClass}`} style={{ width: `${milestone.progress}%` }} />
                    </div>
                  </div>

                  {milestone.dueDateUtc ? (
                    <p className={`mt-3 text-12 ${overdue ? 'text-red-600' : 'text-neutral-500'}`}>
                      Due {format(new Date(milestone.dueDateUtc), 'MMM d, yyyy')}
                    </p>
                  ) : null}

                  {overdue ? (
                    <span className="mt-2 inline-flex rounded bg-red-50 px-2 py-0.5 text-11 font-medium text-red-600">Overdue</span>
                  ) : null}

                  <button
                    type="button"
                    className="mt-3 inline-flex items-center gap-1 text-12 text-primary-600"
                    onClick={() => setExpanded((prev) => ({ ...prev, [milestone.id]: !prev[milestone.id] }))}
                  >
                    View tasks <ChevronDown className={`h-3.5 w-3.5 transition-transform ${expanded[milestone.id] ? 'rotate-180' : ''}`} />
                  </button>
                </div>

                {expanded[milestone.id] ? <MilestoneTaskPreview milestoneId={milestone.id} projectId={projectId ?? ''} /> : null}
              </div>
            );
          })}
        </div>
      )}

      <Modal open={open} onOpenChange={setOpen} title={editingId ? 'Edit milestone' : 'New milestone'} size="sm">
        <form className="space-y-3" onSubmit={onSubmit}>
          <Input label="Name" error={form.formState.errors.name?.message} {...form.register('name')} />
          <div>
            <label className="mb-1 block text-12 font-medium text-neutral-700" htmlFor="milestone-description">
              Description
            </label>
            <textarea
              id="milestone-description"
              className="min-h-[72px] w-full rounded border border-neutral-300 px-3 py-2 text-13 text-neutral-700 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-200"
              {...form.register('description')}
            />
          </div>
          <Input label="Due Date" type="date" {...form.register('dueDateUtc')} />
          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={createMilestone.isPending || updateMilestone.isPending}>
              Save
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
