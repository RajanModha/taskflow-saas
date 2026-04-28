import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { formatDistanceToNow } from 'date-fns';
import { Grid, List, MoreHorizontal, Plus } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { Input } from '../components/ui/Input';
import { Button } from '../components/ui/Button';
import { Modal } from '../components/ui/Modal';
import { Pagination } from '../components/ui/Pagination';
import { Select } from '../components/ui/Select';
import { Toolbar } from '../components/ui/Toolbar';
import { useCreateProject, useDeleteProject, useProjects } from '../hooks/api/projects.hooks';
import { useDebounce } from '../hooks/useDebounce';
import { cn } from '../lib/utils';

const createProjectSchema = z.object({
  name: z.string().min(1, 'Project name is required'),
  description: z.string().optional(),
});

type CreateProjectValues = z.infer<typeof createProjectSchema>;

function projectColor(id: string) {
  const colors = ['bg-indigo-500', 'bg-violet-500', 'bg-blue-500', 'bg-teal-500', 'bg-green-500', 'bg-amber-500', 'bg-orange-500', 'bg-rose-500'];
  const n = Number.parseInt(id.replace(/-/g, '').slice(-2), 16);
  return colors[n % colors.length];
}

function ProjectRowMenu({ onDelete }: { onDelete: () => void }) {
  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <button type="button" className="flex h-7 w-7 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100" aria-label="Project actions">
          <MoreHorizontal className="h-4 w-4" />
        </button>
      </DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content sideOffset={6} align="end" className="z-50 min-w-[160px] rounded-md border border-neutral-200 bg-white py-1 shadow-e200">
          <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50">Edit</DropdownMenu.Item>
          <DropdownMenu.Item onSelect={onDelete} className="cursor-pointer px-3 py-2 text-13 text-red-600 outline-none data-[highlighted]:bg-red-50">
            Delete
          </DropdownMenu.Item>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}

export default function ProjectsPage() {
  const navigate = useNavigate();
  const [q, setQ] = useState('');
  const [debouncedQ] = useDebounce(q, 300);
  const [sortBy, setSortBy] = useState('createdAtUtc');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [view, setView] = useState<'list' | 'grid'>(() => (localStorage.getItem('project-view') as 'list' | 'grid') ?? 'list');
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);

  const { data, isLoading } = useProjects({ page, pageSize: 20, q: debouncedQ, sortBy, sortDir });
  const createProject = useCreateProject();
  const deleteProject = useDeleteProject();

  const form = useForm<CreateProjectValues>({
    resolver: zodResolver(createProjectSchema),
    defaultValues: { name: '', description: '' },
  });

  useEffect(() => {
    localStorage.setItem('project-view', view);
  }, [view]);

  const onCreate = form.handleSubmit(async (values) => {
    try {
      await createProject.mutateAsync(values);
      toast.success('Project created');
      setCreateOpen(false);
      form.reset();
    } catch {
      toast.error('Failed to create project');
    }
  });

  const totalPages = data?.totalPages ?? 0;
  const items = data?.items ?? [];

  const sortValue = useMemo(() => `${sortBy}_${sortDir}`, [sortBy, sortDir]);

  const onSortChange = (value: string) => {
    const [nextSortBy, nextSortDir] = value.split('_');
    setSortBy(nextSortBy);
    setSortDir(nextSortDir as 'asc' | 'desc');
    setPage(1);
  };

  return (
    <div className="page-wrapper">
      <div className="page-header">
        <div>
          <h1 className="page-title">Projects</h1>
          <p className="page-subtitle">{data?.totalCount ?? 0} projects</p>
        </div>
        <Button size="sm" variant="primary" leftIcon={<Plus className="h-3.5 w-3.5" />} onClick={() => setCreateOpen(true)}>
          New project
        </Button>
      </div>

      <Toolbar
        searchValue={q}
        onSearchChange={(value) => {
          setQ(value);
          setPage(1);
        }}
        searchPlaceholder="Search projects..."
        filters={
          <Select
            className="w-44"
            value={sortValue}
            onChange={onSortChange}
            options={[
              { label: 'Newest', value: 'createdAtUtc_desc' },
              { label: 'Oldest', value: 'createdAtUtc_asc' },
              { label: 'Name A-Z', value: 'name_asc' },
              { label: 'Name Z-A', value: 'name_desc' },
            ]}
          />
        }
        actions={
          <div className="flex overflow-hidden rounded border border-neutral-200">
            <button
              type="button"
              className={cn('flex h-8 w-8 items-center justify-center text-neutral-500', view === 'list' && 'bg-neutral-100 text-neutral-800')}
              onClick={() => setView('list')}
              aria-label="List view"
            >
              <List className="h-4 w-4" />
            </button>
            <button
              type="button"
              className={cn('flex h-8 w-8 items-center justify-center border-l border-neutral-200 text-neutral-500', view === 'grid' && 'bg-neutral-100 text-neutral-800')}
              onClick={() => setView('grid')}
              aria-label="Grid view"
            >
              <Grid className="h-4 w-4" />
            </button>
          </div>
        }
      />

      {view === 'list' ? (
        <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
          <table className="w-full border-collapse text-13">
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50">
                <th className="h-9 w-10 px-3 text-left text-11 font-semibold uppercase text-neutral-500" />
                <th className="h-9 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Name</th>
                <th className="hidden h-9 px-3 text-left text-11 font-semibold uppercase text-neutral-500 xl:table-cell">Description</th>
                <th className="h-9 w-24 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Tasks</th>
                <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Updated</th>
                <th className="h-9 w-12 px-3 text-left text-11 font-semibold uppercase text-neutral-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading
                ? Array.from({ length: 8 }).map((_, index) => (
                    <tr key={index} className="h-9 border-b border-neutral-100">
                      <td className="px-3">
                        <div className="h-4 w-4 animate-pulse rounded bg-neutral-200" />
                      </td>
                      <td className="px-3">
                        <div className="h-4 w-36 animate-pulse rounded bg-neutral-200" />
                      </td>
                      <td className="hidden px-3 xl:table-cell">
                        <div className="h-4 w-56 animate-pulse rounded bg-neutral-200" />
                      </td>
                      <td className="px-3">
                        <div className="h-4 w-14 animate-pulse rounded bg-neutral-200" />
                      </td>
                      <td className="px-3">
                        <div className="h-4 w-20 animate-pulse rounded bg-neutral-200" />
                      </td>
                      <td className="px-3">
                        <div className="h-4 w-6 animate-pulse rounded bg-neutral-200" />
                      </td>
                    </tr>
                  ))
                : items.map((project) => (
                    <tr key={project.id} className="h-9 cursor-pointer border-b border-neutral-100 hover:bg-neutral-50" onClick={() => navigate(`/projects/${project.id}/board`)}>
                      <td className="px-3">
                        <input type="checkbox" />
                      </td>
                      <td className="px-3">
                        <div className="flex items-center gap-2">
                          <span className={cn('flex h-5 w-5 items-center justify-center rounded text-10 font-semibold text-white', projectColor(project.id))}>
                            {project.name[0]?.toUpperCase()}
                          </span>
                          <span className="truncate font-medium text-neutral-800">{project.name}</span>
                        </div>
                      </td>
                      <td className="hidden truncate px-3 text-neutral-500 xl:table-cell">{project.description ?? '—'}</td>
                      <td className="px-3 text-neutral-500">— tasks</td>
                      <td className="px-3 text-12 text-neutral-500">{formatDistanceToNow(new Date(project.updatedAtUtc), { addSuffix: true })}</td>
                      <td className="px-3" onClick={(event) => event.stopPropagation()}>
                        <ProjectRowMenu
                          onDelete={() => {
                            deleteProject.mutate(project.id, {
                              onSuccess: () => toast.success('Project deleted'),
                              onError: () => toast.error('Failed to delete project'),
                            });
                          }}
                        />
                      </td>
                    </tr>
                  ))}
            </tbody>
          </table>
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {items.map((project) => (
            <button
              key={project.id}
              type="button"
              className="rounded-md border border-neutral-200 bg-white p-4 text-left hover:border-neutral-300"
              onClick={() => navigate(`/projects/${project.id}/board`)}
            >
              <div className="flex items-center gap-2.5">
                <span className={cn('flex h-5 w-5 items-center justify-center rounded text-10 font-semibold text-white', projectColor(project.id))}>
                  {project.name[0]?.toUpperCase()}
                </span>
                <p className="truncate text-13 font-semibold text-neutral-800">{project.name}</p>
              </div>
              <p className="mt-2 line-clamp-2 min-h-[32px] text-12 text-neutral-500">{project.description ?? 'No description'}</p>
              <div className="mt-3 flex items-center justify-between text-12 text-neutral-500">
                <span>Updated {formatDistanceToNow(new Date(project.updatedAtUtc), { addSuffix: true })}</span>
                <span className="text-primary-600">View board -&gt;</span>
              </div>
            </button>
          ))}
        </div>
      )}

      {totalPages > 1 ? <Pagination page={page} pageSize={20} totalCount={data?.totalCount ?? 0} onPageChange={setPage} className="mt-3" /> : null}

      <Modal open={createOpen} onOpenChange={setCreateOpen} title="Create project" description="Name is required. Description is optional." size="sm">
        <form className="space-y-3" onSubmit={onCreate}>
          <Input label="Name" error={form.formState.errors.name?.message} {...form.register('name')} />
          <Input label="Description" error={form.formState.errors.description?.message} {...form.register('description')} />
          <div className="flex justify-end gap-2 pt-1">
            <Button type="button" variant="secondary" onClick={() => setCreateOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={createProject.isPending}>
              Create
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
