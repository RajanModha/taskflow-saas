import { LayoutList, KanbanSquare } from 'lucide-react';
import { NavLink } from 'react-router-dom';
import { cn } from '../../lib/utils';

interface ProjectSubNavProps {
  projectId: string;
  activeTab: 'list' | 'board';
}

export function ProjectSubNav({ projectId, activeTab }: ProjectSubNavProps) {
  const base =
    'inline-flex h-8 items-center gap-1.5 rounded border px-3 text-12 font-medium transition-colors';

  return (
    <div className="mb-3 flex items-center gap-2 border-b border-neutral-150 pb-3">
      <NavLink
        to={`/projects/${projectId}/list`}
        className={cn(
          base,
          activeTab === 'list'
            ? 'border-primary-300 bg-primary-50 text-primary-700'
            : 'border-neutral-200 bg-white text-neutral-600 hover:bg-neutral-50',
        )}
      >
        <LayoutList className="h-3.5 w-3.5" />
        List
      </NavLink>
      <NavLink
        to={`/projects/${projectId}/board`}
        className={cn(
          base,
          activeTab === 'board'
            ? 'border-primary-300 bg-primary-50 text-primary-700'
            : 'border-neutral-200 bg-white text-neutral-600 hover:bg-neutral-50',
        )}
      >
        <KanbanSquare className="h-3.5 w-3.5" />
        Board
      </NavLink>
    </div>
  );
}
