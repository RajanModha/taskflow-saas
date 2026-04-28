import { zodResolver } from '@hookform/resolvers/zod';
import { Pencil, Plus, Tag, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { EmptyState } from '../../components/ui/EmptyState';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { useCreateTag, useDeleteTag, useTags, useUpdateTag } from '../../hooks/api/tags.hooks';
import { useWorkspaceMe } from '../../hooks/api/workspace.hooks';

const PRESET_COLORS = ['#6366F1', '#8B5CF6', '#3B82F6', '#06B6D4', '#10B981', '#22C55E', '#EAB308', '#F59E0B', '#F97316', '#EF4444', '#F43F5E', '#EC4899'];

const schema = z.object({
  name: z.string().min(1).max(30),
  color: z.string().regex(/^#[0-9A-Fa-f]{6}$/, 'Use valid hex color'),
});

type FormValues = z.infer<typeof schema>;

export default function SettingsTagsPage() {
  const { data: tags = [] } = useTags();
  const { data: workspace } = useWorkspaceMe();
  const createTag = useCreateTag();
  const updateTag = useUpdateTag();
  const deleteTag = useDeleteTag();

  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', color: PRESET_COLORS[0] },
  });

  const isAdmin = ['Owner', 'Admin'].includes(workspace?.currentUserRole ?? '');
  const selectedColor = form.watch('color');

  const onCreate = () => {
    setEditingId(null);
    form.reset({ name: '', color: PRESET_COLORS[0] });
    setOpen(true);
  };

  const onEdit = (tag: (typeof tags)[number]) => {
    setEditingId(tag.id);
    form.reset({ name: tag.name, color: tag.color });
    setOpen(true);
  };

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      if (!editingId) {
        await createTag.mutateAsync(values);
        toast.success('Tag created');
      } else {
        await updateTag.mutateAsync({ tagId: editingId, payload: values });
        toast.success('Tag updated');
      }
      setOpen(false);
    } catch {
      toast.error('Failed to save tag');
    }
  });

  return (
    <div>
      <div className="page-header">
        <h2 className="page-title">Tags</h2>
        {isAdmin ? (
          <Button size="sm" leftIcon={<Plus className="h-3.5 w-3.5" />} onClick={onCreate}>
            New Tag
          </Button>
        ) : null}
      </div>

      {tags.length === 0 ? (
        <EmptyState icon={Tag} title="No tags yet." action={isAdmin ? { label: 'Create tag', onClick: onCreate } : undefined} />
      ) : (
        <div className="space-y-2">
          {tags.map((tag) => (
            <div key={tag.id} className="flex items-center gap-3 rounded-md border border-neutral-200 bg-white px-4 py-2.5">
              <span className="h-4 w-4 rounded-full" style={{ backgroundColor: tag.color }} />
              <span className="flex-1 text-13 font-medium text-neutral-700">{tag.name}</span>
              {isAdmin ? (
                <>
                  <button type="button" className="rounded p-1 text-neutral-500 hover:bg-neutral-100" onClick={() => onEdit(tag)}>
                    <Pencil className="h-3.5 w-3.5" />
                  </button>
                  <button
                    type="button"
                    className="rounded p-1 text-red-600 hover:bg-red-50"
                    onClick={() => {
                      const yes = window.confirm(`Delete '${tag.name}' tag?`);
                      if (!yes) return;
                      deleteTag.mutate(tag.id, {
                        onSuccess: () => toast.success('Tag deleted'),
                        onError: () => toast.error('Failed to delete tag'),
                      });
                    }}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </>
              ) : null}
            </div>
          ))}
        </div>
      )}

      <Modal open={open} onOpenChange={setOpen} title={editingId ? 'Edit tag' : 'Create tag'} size="sm">
        <form className="space-y-3" onSubmit={onSubmit}>
          <Input label="Name" error={form.formState.errors.name?.message} {...form.register('name')} />

          <div>
            <p className="mb-1 text-12 font-medium text-neutral-700">Color</p>
            <div className="grid grid-cols-6 gap-2">
              {PRESET_COLORS.map((color) => (
                <button
                  key={color}
                  type="button"
                  className={`h-6 w-6 rounded-full ${selectedColor === color ? 'ring-2 ring-primary-400 ring-offset-1' : ''}`}
                  style={{ backgroundColor: color }}
                  onClick={() => form.setValue('color', color)}
                />
              ))}
            </div>
          </div>

          <div className="flex items-center gap-2">
            <span className="h-4 w-4 rounded-full" style={{ backgroundColor: selectedColor }} />
            <Input label="Custom hex" error={form.formState.errors.color?.message} {...form.register('color')} />
          </div>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={createTag.isPending || updateTag.isPending}>
              {editingId ? 'Save' : 'Create'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
