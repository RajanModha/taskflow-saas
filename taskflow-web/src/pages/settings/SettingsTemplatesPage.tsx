import { zodResolver } from '@hookform/resolvers/zod';
import { DndContext, PointerSensor, closestCenter, useSensor, useSensors } from '@dnd-kit/core';
import { SortableContext, arrayMove, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Copy, GripVertical, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { Select } from '../../components/ui/Select';
import { useTags } from '../../hooks/api/tags.hooks';
import { useCreateTemplate, useDeleteTemplate, useTemplates, useUpdateTemplate } from '../../hooks/api/templates.hooks';
import { TaskPriority, TaskPriorityLabel } from '../../types/api';

const schema = z.object({
  name: z.string().min(1),
  description: z.string().optional(),
  defaultTitle: z.string().min(1),
  defaultDescription: z.string().optional(),
  defaultPriority: z.nativeEnum(TaskPriority),
  defaultDueDaysFromNow: z.string().optional(),
  tagIds: z.array(z.string()).optional(),
});

type FormValues = z.infer<typeof schema>;

function SortableChecklistItem({
  id,
  value,
  onChange,
  onDelete,
}: {
  id: string;
  value: string;
  onChange: (next: string) => void;
  onDelete: () => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id });
  return (
    <div ref={setNodeRef} style={{ transform: CSS.Transform.toString(transform), transition }} className="flex items-center gap-2">
      <button type="button" {...attributes} {...listeners}>
        <GripVertical className="h-4 w-4 text-neutral-400" />
      </button>
      <Input value={value} onChange={(event) => onChange(event.target.value)} />
      <button type="button" className="rounded p-1 text-red-600 hover:bg-red-50" onClick={onDelete}>
        <Trash2 className="h-3.5 w-3.5" />
      </button>
    </div>
  );
}

export default function SettingsTemplatesPage() {
  const { data: templates = [] } = useTemplates();
  const { data: tags = [] } = useTags();
  const createTemplate = useCreateTemplate();
  const updateTemplate = useUpdateTemplate();
  const deleteTemplate = useDeleteTemplate();
  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [checklistItems, setChecklistItems] = useState<Array<{ id: string; title: string }>>([]);

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 5 } }));

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      description: '',
      defaultTitle: '',
      defaultDescription: '',
      defaultPriority: TaskPriority.None,
      defaultDueDaysFromNow: '',
      tagIds: [],
    },
  });

  const selectedTags = form.watch('tagIds') ?? [];

  const onCreate = () => {
    setEditingId(null);
    setChecklistItems([]);
    form.reset({
      name: '',
      description: '',
      defaultTitle: '',
      defaultDescription: '',
      defaultPriority: TaskPriority.None,
      defaultDueDaysFromNow: '',
      tagIds: [],
    });
    setOpen(true);
  };

  const onEdit = (template: (typeof templates)[number]) => {
    setEditingId(template.id);
    setChecklistItems(template.checklistItems.map((item) => ({ id: item.id, title: item.title })));
    form.reset({
      name: template.name,
      description: template.description ?? '',
      defaultTitle: template.defaultTitle,
      defaultDescription: template.defaultDescription ?? '',
      defaultPriority: template.defaultPriority,
      defaultDueDaysFromNow: template.defaultDueDaysFromNow ? String(template.defaultDueDaysFromNow) : '',
      tagIds: template.tags.map((tag) => tag.id),
    });
    setOpen(true);
  };

  const onSubmit = form.handleSubmit(async (values) => {
    const payload = {
      ...values,
      description: values.description || undefined,
      defaultDescription: values.defaultDescription || undefined,
      defaultDueDaysFromNow: values.defaultDueDaysFromNow ? Number(values.defaultDueDaysFromNow) : undefined,
      checklistItems: checklistItems.map((item) => item.title).filter(Boolean),
      tagIds: selectedTags.length > 0 ? selectedTags : undefined,
    };

    try {
      if (!editingId) {
        await createTemplate.mutateAsync(payload);
        toast.success('Template created');
      } else {
        await updateTemplate.mutateAsync({ id: editingId, payload });
        toast.success('Template updated');
      }
      setOpen(false);
    } catch {
      toast.error('Failed to save template');
    }
  });

  return (
    <div>
      <div className="page-header">
        <h2 className="page-title">Templates</h2>
        <Button size="sm" leftIcon={<Plus className="h-3.5 w-3.5" />} onClick={onCreate}>
          Create template
        </Button>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {templates.map((template) => (
          <div key={template.id} className="rounded-md border border-neutral-200 bg-white p-4">
            <div className="mb-2 flex h-8 w-8 items-center justify-center rounded-full bg-primary-100 text-primary-700">
              <Copy className="h-4 w-4" />
            </div>
            <p className="text-14 font-semibold text-neutral-800">{template.name}</p>
            <p className="mt-1 line-clamp-2 text-12 text-neutral-500">{template.description ?? 'No description'}</p>
            <div className="mt-2 flex flex-wrap gap-1">
              <span className="rounded bg-primary-50 px-2 py-0.5 text-11 text-primary-700">{TaskPriorityLabel[template.defaultPriority]}</span>
              <span className="rounded bg-neutral-100 px-2 py-0.5 text-11 text-neutral-600">{template.checklistItems.length} checklist items</span>
              {template.defaultDueDaysFromNow ? (
                <span className="rounded bg-neutral-100 px-2 py-0.5 text-11 text-neutral-600">Due in {template.defaultDueDaysFromNow} days</span>
              ) : null}
            </div>
            <div className="mt-2 flex flex-wrap gap-1">
              {template.tags.slice(0, 3).map((tag) => (
                <span key={tag.id} className="rounded px-1.5 py-0.5 text-10 text-white" style={{ backgroundColor: tag.color }}>
                  {tag.name}
                </span>
              ))}
              {template.tags.length > 3 ? <span className="text-11 text-neutral-500">+{template.tags.length - 3}</span> : null}
            </div>
            <div className="mt-3 flex items-center justify-between text-11 text-neutral-500">
              <span>By {template.createdBy.userName}</span>
              <div className="flex gap-1">
                <Button size="xs" variant="ghost" onClick={() => onEdit(template)}>
                  Edit
                </Button>
                <Button
                  size="xs"
                  variant="danger-ghost"
                  onClick={() =>
                    deleteTemplate.mutate(template.id, {
                      onSuccess: () => toast.success('Template deleted'),
                    })
                  }
                >
                  Delete
                </Button>
              </div>
            </div>
          </div>
        ))}
      </div>

      <Modal open={open} onOpenChange={setOpen} title={editingId ? 'Edit template' : 'Create template'} size="lg">
        <form className="space-y-3" onSubmit={onSubmit}>
          <Input label="Name" error={form.formState.errors.name?.message} {...form.register('name')} />
          <div>
            <label className="mb-1 block text-12 font-medium text-neutral-700">Description</label>
            <textarea className="min-h-[60px] w-full rounded border border-neutral-300 px-3 py-2 text-13" {...form.register('description')} />
          </div>
          <Input label="Default title" error={form.formState.errors.defaultTitle?.message} {...form.register('defaultTitle')} />
          <div>
            <label className="mb-1 block text-12 font-medium text-neutral-700">Default description</label>
            <textarea className="min-h-[60px] w-full rounded border border-neutral-300 px-3 py-2 text-13" {...form.register('defaultDescription')} />
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            <Select
              value={String(form.watch('defaultPriority'))}
              onChange={(value) => form.setValue('defaultPriority', Number(value) as TaskPriority)}
              options={Object.entries(TaskPriorityLabel).map(([value, label]) => ({ value, label }))}
            />
            <Input type="number" label="Due days from now" {...form.register('defaultDueDaysFromNow')} />
          </div>

          <div>
            <p className="mb-1 text-12 font-medium text-neutral-700">Tags</p>
            <div className="flex flex-wrap gap-1.5">
              {tags.map((tag) => {
                const active = selectedTags.includes(tag.id);
                return (
                  <button
                    key={tag.id}
                    type="button"
                    className={`rounded px-2 py-1 text-11 text-white ${active ? 'ring-2 ring-primary-300' : ''}`}
                    style={{ backgroundColor: tag.color }}
                    onClick={() =>
                      form.setValue(
                        'tagIds',
                        active ? selectedTags.filter((id) => id !== tag.id) : [...selectedTags, tag.id],
                      )
                    }
                  >
                    {tag.name}
                  </button>
                );
              })}
            </div>
          </div>

          <div>
            <p className="mb-1 text-12 font-medium text-neutral-700">Checklist items</p>
            <DndContext
              sensors={sensors}
              collisionDetection={closestCenter}
              onDragEnd={({ active, over }) => {
                if (!over || active.id === over.id) return;
                const oldIndex = checklistItems.findIndex((item) => item.id === active.id);
                const newIndex = checklistItems.findIndex((item) => item.id === over.id);
                if (oldIndex < 0 || newIndex < 0) return;
                setChecklistItems((items) => arrayMove(items, oldIndex, newIndex));
              }}
            >
              <SortableContext items={checklistItems.map((item) => item.id)} strategy={verticalListSortingStrategy}>
                <div className="space-y-2">
                  {checklistItems.map((item) => (
                    <SortableChecklistItem
                      key={item.id}
                      id={item.id}
                      value={item.title}
                      onChange={(next) =>
                        setChecklistItems((items) => items.map((entry) => (entry.id === item.id ? { ...entry, title: next } : entry)))
                      }
                      onDelete={() => setChecklistItems((items) => items.filter((entry) => entry.id !== item.id))}
                    />
                  ))}
                </div>
              </SortableContext>
            </DndContext>
            <Button
              type="button"
              size="sm"
              variant="secondary"
              className="mt-2"
              disabled={checklistItems.length >= 20}
              onClick={() => setChecklistItems((items) => [...items, { id: crypto.randomUUID(), title: '' }])}
            >
              Add item
            </Button>
          </div>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={createTemplate.isPending || updateTemplate.isPending}>
              {editingId ? 'Save' : 'Create'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
