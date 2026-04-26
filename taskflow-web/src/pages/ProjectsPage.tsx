import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { formatDistanceToNow } from 'date-fns';
import { FolderOpen, Grid, List, MoreHorizontal, Plus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '../components/ui/Button';
import { EmptyState } from '../components/ui/EmptyState';
import { Pagination } from '../components/ui/Pagination';
import { Select } from '../components/ui/Select';
import { Toolbar } from '../components/ui/Toolbar';
import { cn } from '../lib/utils';

type Project = {
  id: string;
  name: string;
  description?: string;
  taskCount: number;
  progress: number;
  createdAtUtc: string;
};

const workspaceName = 'Product Engineering';

const ALL_PROJECTS: Project[] = [
  {
    id: 'p-1',
    name: 'TaskFlow Core Platform',
    description: 'Stability, auth and billing hardening',
    taskCount: 42,
    progress: 68,
    createdAtUtc: '2026-03-18T09:00:00Z',
  },
  {
    id: 'p-2',
    name: 'TaskFlow Web',
    description: 'Dense Atlassian-inspired UI rollout',
    taskCount: 31,
    progress: 54,
    createdAtUtc: '2026-04-01T10:30:00Z',
  },
  {
    id: 'p-3',
    name: 'Mobile Companion',
    description: 'Offline-first task tracking',
    taskCount: 18,
    progress: 34,
    createdAtUtc: '2026-04-04T07:15:00Z',
  },
  {
    id: 'p-4',
    name: 'Customer Workspace Templates',
    description: 'Pre-baked workflows and automation packs',
    taskCount: 12,
    progress: 78,
    createdAtUtc: '2026-04-08T12:10:00Z',
  },
  {
    id: 'p-5',
    name: 'Realtime Notifications',
    description: 'In-app feed and email digest parity',
    taskCount: 23,
    progress: 46,
    createdAtUtc: '2026-04-12T06:25:00Z',
  },
  {
    id: 'p-6',
    name: 'Analytics Dashboards',
    description: 'Weekly velocity and SLA tracking',
    taskCount: 16,
    progress: 29,
    createdAtUtc: '2026-04-20T08:45:00Z',
  },
  {
    id: 'p-7',
    name: 'Enterprise SSO',
    description: 'SAML and SCIM integration',
    taskCount: 20,
    progress: 62,
    createdAtUtc: '2026-04-24T11:20:00Z',
  },
  {
    id: 'p-8',
    name: 'Automation Rules Engine',
    description: 'Event-driven actions and auditability',
    taskCount: 27,
    progress: 41,
    createdAtUtc: '2026-04-26T05:05:00Z',
  },
];

function projectColor(id: string) {
  const palette = [
    'bg-indigo-500',
    'bg-blue-500',
    'bg-violet-500',
    'bg-teal-500',
    'bg-emerald-500',
    'bg-amber-500',
  ];
  const hash = id.split('').reduce((acc, ch) => acc + ch.charCodeAt(0), 0);
  return palette[hash % palette.length];
}

function ProjectRowMenu() {
  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <button
          type="button"
          className="flex h-7 w-7 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100"
          aria-label="Project actions"
        >
          <MoreHorizontal className="h-4 w-4" />
        </button>
      </DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content
          sideOffset={6}
          align="end"
          className="z-50 min-w-[160px] rounded-md border border-neutral-200 bg-white py-1 shadow-e200"
        >
          <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50">
            Open
          </DropdownMenu.Item>
          <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50">
            Edit
          </DropdownMenu.Item>
          <DropdownMenu.Separator className="my-1 h-px bg-neutral-150" />
          <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-red-600 outline-none data-[highlighted]:bg-red-50">
            Archive
          </DropdownMenu.Item>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}

function ProjectCard({ project, onOpen }: { project: Project; onOpen: () => void }) {
  return (
    <button
      type="button"
      onClick={onOpen}
      className="rounded-md border border-neutral-200 bg-white p-4 text-left hover:bg-neutral-50"
    >
      <div className="flex items-center gap-2.5">
        <div
          className={cn(
            'flex h-6 w-6 shrink-0 items-center justify-center rounded text-11 font-bold text-white',
            projectColor(project.id),
          )}
        >
          {project.name[0]?.toUpperCase()}
        </div>
        <p className="min-w-0 flex-1 truncate text-13 font-medium text-neutral-800">{project.name}</p>
      </div>
      {project.description ? <p className="mt-2 line-clamp-2 text-12 text-neutral-500">{project.description}</p> : null}
      <div className="mt-3 flex items-center justify-between text-12 text-neutral-500">
        <span>{project.taskCount} tasks</span>
        <span>{project.progress}%</span>
      </div>
      <div className="mt-1.5 h-1.5 rounded-full bg-neutral-150">
        <div className="h-1.5 rounded-full bg-primary-500" style={{ width: `${project.progress}%` }} />
      </div>
    </button>
  );
}

export default function ProjectsPage() {
  const navigate = useNavigate();
  const [q, setQ] = useState('');
  const [sortBy, setSortBy] = useState('name_asc');
  const [view, setView] = useState<'list' | 'grid'>('list');
  const [page, setPage] = useState(1);
  const pageSize = 8;

  const projects = useMemo(() => {
    const needle = q.trim().toLowerCase();
    const filtered = ALL_PROJECTS.filter(
      (p) =>
        p.name.toLowerCase().includes(needle) ||
        p.description?.toLowerCase().includes(needle),
    );

    const sorted = [...filtered].sort((a, b) => {
      if (sortBy === 'name_asc') return a.name.localeCompare(b.name);
      if (sortBy === 'name_desc') return b.name.localeCompare(a.name);
      if (sortBy === 'createdAt_desc') return +new Date(b.createdAtUtc) - +new Date(a.createdAtUtc);
      if (sortBy === 'createdAt_asc') return +new Date(a.createdAtUtc) - +new Date(b.createdAtUtc);
      return 0;
    });

    const start = (page - 1) * pageSize;
    const items = sorted.slice(start, start + pageSize);

    return { totalCount: sorted.length, items };
  }, [page, q, sortBy]);

  return (
    <div className="page-wrapper">
      <div className="page-header">
        <div>
          <h1 className="page-title">Projects</h1>
          <p className="page-subtitle">
            {projects.totalCount} projects in {workspaceName}
          </p>
        </div>
        <Button size="sm" variant="primary" leftIcon={<Plus className="h-3.5 w-3.5" />}>
          New project
        </Button>
      </div>

      <Toolbar
        searchValue={q}
        onSearchChange={(v) => {
          setQ(v);
          setPage(1);
        }}
        searchPlaceholder="Search projects..."
        filters={
          <Select
            className="w-44"
            placeholder="Sort"
            value={sortBy}
            onChange={(v) => {
              setSortBy(v);
              setPage(1);
            }}
            options={[
              { label: 'Name A-Z', value: 'name_asc' },
              { label: 'Name Z-A', value: 'name_desc' },
              { label: 'Newest', value: 'createdAt_desc' },
              { label: 'Oldest', value: 'createdAt_asc' },
            ]}
          />
        }
        actions={
          <div className="flex overflow-hidden rounded border border-neutral-200">
            <button
              type="button"
              className={cn(
                'flex h-8 w-8 items-center justify-center text-neutral-500',
                view === 'list' && 'bg-neutral-100 text-neutral-800',
              )}
              onClick={() => setView('list')}
              aria-label="List view"
            >
              <List className="h-4 w-4" />
            </button>
            <button
              type="button"
              className={cn(
                'flex h-8 w-8 items-center justify-center border-l border-neutral-200 text-neutral-500',
                view === 'grid' && 'bg-neutral-100 text-neutral-800',
              )}
              onClick={() => setView('grid')}
              aria-label="Grid view"
            >
              <Grid className="h-4 w-4" />
            </button>
          </div>
        }
      />

      {projects.items.length === 0 ? (
        <EmptyState
          icon={FolderOpen}
          title="No projects found"
          description="No matching projects. Try a different search."
          action={{ label: 'Create project', onClick: () => undefined }}
        />
      ) : null}

      {projects.items.length > 0 && view === 'list' ? (
        <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
          <table className="w-full border-collapse text-13">
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50">
                <th className="h-9 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Name</th>
                <th className="h-9 w-24 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Tasks</th>
                <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Progress</th>
                <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Created</th>
                <th className="h-9 w-10 px-3" />
              </tr>
            </thead>
            <tbody>
              {projects.items.map((p) => (
                <tr
                  key={p.id}
                  className="h-9 cursor-pointer border-b border-neutral-100 hover:bg-neutral-50"
                  onClick={() => navigate(`/projects/${p.id}/board`)}
                >
                  <td className="px-3">
                    <div className="flex items-center gap-2.5">
                      <div
                        className={cn(
                          'flex h-5 w-5 shrink-0 items-center justify-center rounded text-10 font-bold text-white',
                          projectColor(p.id),
                        )}
                      >
                        {p.name[0]?.toUpperCase()}
                      </div>
                      <span className="truncate font-medium text-neutral-800">{p.name}</span>
                      {p.description ? (
                        <span className="hidden truncate text-12 text-neutral-400 xl:block">{p.description}</span>
                      ) : null}
                    </div>
                  </td>
                  <td className="px-3 text-neutral-600">{p.taskCount ?? 0} tasks</td>
                  <td className="px-3">
                    <div className="flex items-center gap-2">
                      <div className="h-1.5 flex-1 rounded-full bg-neutral-150">
                        <div className="h-1.5 rounded-full bg-primary-500" style={{ width: `${p.progress ?? 0}%` }} />
                      </div>
                      <span className="w-8 text-right text-12 text-neutral-500">{p.progress ?? 0}%</span>
                    </div>
                  </td>
                  <td className="px-3 text-12 text-neutral-500">
                    {formatDistanceToNow(new Date(p.createdAtUtc), { addSuffix: true })}
                  </td>
                  <td className="px-3" onClick={(e) => e.stopPropagation()}>
                    <ProjectRowMenu />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      {projects.items.length > 0 && view === 'grid' ? (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {projects.items.map((p) => (
            <ProjectCard key={p.id} project={p} onOpen={() => navigate(`/projects/${p.id}/board`)} />
          ))}
        </div>
      ) : null}

      <Pagination
        page={page}
        pageSize={pageSize}
        totalCount={projects.totalCount}
        onPageChange={setPage}
        className="mt-3"
      />
    </div>
  );
}
