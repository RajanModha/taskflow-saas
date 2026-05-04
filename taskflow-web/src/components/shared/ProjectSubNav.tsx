import { useNavigate } from 'react-router-dom';
import { cn } from '../../lib/utils';

interface ProjectSubNavProps {
  projectId: string;
  activeTab: 'board' | 'list' | 'activity' | 'milestones';
}

const TABS = [
  { key: 'board', label: 'Board', path: 'board' },
  { key: 'list', label: 'List', path: 'list' },
  { key: 'activity', label: 'Activity', path: 'activity' },
  { key: 'milestones', label: 'Milestones', path: 'milestones' },
] as const;

export function ProjectSubNav({ projectId, activeTab }: ProjectSubNavProps) {
  const navigate = useNavigate();
  return (
    <div className="-mx-8 mb-4 flex items-center border-b border-neutral-200 px-8">
      {TABS.map((tab) => (
        <button
          key={tab.key}
          type="button"
          onClick={() => navigate(`/projects/${projectId}/${tab.path}`)}
          className={cn(
            'h-9 border-b-2 -mb-px px-4 text-13 transition-colors',
            activeTab === tab.key
              ? 'border-primary-600 font-medium text-primary-700'
              : 'border-transparent text-neutral-500 hover:border-neutral-300 hover:text-neutral-800',
          )}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}
