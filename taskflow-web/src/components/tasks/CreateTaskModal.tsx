import * as Popover from '@radix-ui/react-popover';
import { zodResolver } from '@hookform/resolvers/zod';
import { Sparkles } from 'lucide-react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { useMilestones } from '../../hooks/api/projects.hooks';
import { useTags } from '../../hooks/api/tags.hooks';
import { useCreateTask } from '../../hooks/api/tasks.hooks';
import { useMembers } from '../../hooks/api/workspace.hooks';
import { TaskPriority, TaskPriorityLabel, TaskStatus, TaskStatusLabel } from '../../types/api';
import { Button } from '../ui/Button';
import { Input } from '../ui/Input';
import { Modal } from '../ui/Modal';
import { Select } from '../ui/Select';

const schema = z.object({
  title: z.string().min(1).max(500),
  description: z.string().optional(),
  status: z.nativeEnum(TaskStatus),
  priority: z.nativeEnum(TaskPriority),
  dueDateUtc: z.string().optional(),
  assigneeId: z.string().uuid().optional().or(z.literal('')),
  tagIds: z.array(z.string().uuid()).optional(),
  milestoneId: z.string().uuid().optional().or(z.literal('')),
});

type FormValues = z.infer<typeof schema>;

interface CreateTaskModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  projectId: string;
}

export function CreateTaskModal({ open, onOpenChange, projectId }: CreateTaskModalProps) {
  const createTask = useCreateTask();
  const { data: members } = useMembers({ page: 1, pageSize: 100 });
  const { data: tags } = useTags();
  const { data: milestones } = useMilestones(projectId);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      title: '',
      description: '',
      status: TaskStatus.Todo,
      priority: TaskPriority.None,
      dueDateUtc: '',
      assigneeId: '',
      tagIds: [],
      milestoneId: '',
    },
  });

  const selectedTags = form.watch('tagIds') ?? [];

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      await createTask.mutateAsync({
        projectId,
        title: values.title,
        description: values.description || undefined,
        status: values.status,
        priority: values.priority,
        dueDateUtc: values.dueDateUtc || undefined,
        assigneeId: values.assigneeId || undefined,
        tagIds: values.tagIds?.length ? values.tagIds : undefined,
        milestoneId: values.milestoneId || undefined,
      });
      toast.success('Task created');
      form.reset();
      onOpenChange(false);
    } catch {
      toast.error('Failed to create task');
    }
  });

  return (
    <Modal open={open} onOpenChange={onOpenChange} title="Create task" size="md">
      <form onSubmit={onSubmit} className="space-y-3">
        <Input autoFocus label="Title" error={form.formState.errors.title?.message} {...form.register('title')} />
        <div>
          <label className="mb-1 block text-12 font-medium text-neutral-700" htmlFor="task-description">
            Description
          </label>
          <textarea
            id="task-description"
            className="min-h-[60px] w-full rounded border border-neutral-300 px-3 py-2 text-13 text-neutral-800 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-200"
            {...form.register('description')}
          />
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Select
            value={String(form.watch('status'))}
            onChange={(value) => form.setValue('status', Number(value) as TaskStatus)}
            options={Object.entries(TaskStatusLabel).map(([value, label]) => ({ value, label }))}
          />
          <Select
            value={String(form.watch('priority'))}
            onChange={(value) => form.setValue('priority', Number(value) as TaskPriority)}
            options={Object.entries(TaskPriorityLabel).map(([value, label]) => ({ value, label }))}
          />
          <Select
            value={form.watch('assigneeId') ?? ''}
            onChange={(value) => form.setValue('assigneeId', value)}
            options={[{ label: 'Unassigned', value: '' }, ...((members?.items ?? []).map((m) => ({ label: m.displayName ?? m.userName, value: m.id })))]}
          />
          <Input type="date" value={form.watch('dueDateUtc') ?? ''} onChange={(event) => form.setValue('dueDateUtc', event.target.value)} />
          <Select
            value={form.watch('milestoneId') ?? ''}
            onChange={(value) => form.setValue('milestoneId', value)}
            options={[{ label: 'No milestone', value: '' }, ...((milestones ?? []).map((m) => ({ label: m.name, value: m.id })))]}
          />
        </div>

        <div>
          <p className="mb-1 text-12 font-medium text-neutral-700">Tags</p>
          <div className="flex flex-wrap gap-1.5">
            {(tags ?? []).map((tag) => {
              const active = selectedTags.includes(tag.id);
              return (
                <button
                  key={tag.id}
                  type="button"
                  onClick={() =>
                    form.setValue(
                      'tagIds',
                      active ? selectedTags.filter((id) => id !== tag.id) : [...selectedTags, tag.id],
                    )
                  }
                  className={`rounded px-2 py-1 text-11 text-white ${active ? 'ring-2 ring-primary-300' : ''}`}
                  style={{ backgroundColor: tag.color }}
                >
                  {tag.name}
                </button>
              );
            })}
          </div>
        </div>

        <div className="flex items-center justify-between pt-1">
          <Popover.Root>
            <Popover.Trigger asChild>
              <Button type="button" size="sm" variant="secondary" leftIcon={<Sparkles className="h-3.5 w-3.5" />}>
                Use template
              </Button>
            </Popover.Trigger>
            <Popover.Portal>
              <Popover.Content className="z-50 w-56 rounded-md border border-neutral-200 bg-white p-3 shadow-e200" sideOffset={8}>
                <p className="text-12 text-neutral-600">Template picker will be wired in the next step.</p>
              </Popover.Content>
            </Popover.Portal>
          </Popover.Root>

          <div className="flex gap-2">
            <Button type="button" variant="secondary" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={createTask.isPending}>
              Create task
            </Button>
          </div>
        </div>
      </form>
    </Modal>
  );
}
