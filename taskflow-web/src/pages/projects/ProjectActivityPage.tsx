import { formatDistanceToNow } from 'date-fns';
import { Activity } from 'lucide-react';
import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { ProjectSubNav } from '../../components/projects/ProjectSubNav';
import { EmptyState } from '../../components/ui/EmptyState';
import { Pagination } from '../../components/ui/Pagination';
import { useProjectActivity } from '../../hooks/api/projects.hooks';

function actionDotClass(action: string) {
  if (action.startsWith('project.')) return 'border-green-500';
  if (action.startsWith('member.')) return 'border-amber-500';
  return 'border-primary-500';
}

function formatAction(action: string) {
  const map: Record<string, string> = {
    'task.created': 'created a task',
    'task.status_changed': 'updated task status',
    'task.assigned': 'assigned a task',
    'task.commented': 'commented on a task',
    'project.created': 'created project',
    'project.deleted': 'deleted project',
    'member.invited': 'invited a member',
  };
  return map[action] ?? action.replaceAll('.', ' ');
}

export default function ProjectActivityPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const [page, setPage] = useState(1);
  const { data, isLoading } = useProjectActivity(projectId ?? null, { page, pageSize: 20 });
  const items = data?.items ?? [];

  return (
    <div className="page-wrapper">
      <ProjectSubNav projectId={projectId ?? ''} activeTab="activity" />

      <div className="page-header">
        <div>
          <h1 className="page-title">Project Activity</h1>
          <p className="page-subtitle">Recent changes and events for this project.</p>
        </div>
      </div>

      {isLoading ? (
        <div className="ml-5 border-l-2 border-neutral-150 pl-4">
          {Array.from({ length: 8 }).map((_, index) => (
            <div key={index} className="relative border-b border-neutral-100 py-2">
              <span className="absolute -left-[9px] h-4 w-4 animate-pulse rounded-full border-2 border-neutral-300 bg-white" />
              <div className="h-3 w-56 animate-pulse rounded bg-neutral-200" />
              <div className="mt-1 h-3 w-28 animate-pulse rounded bg-neutral-200" />
            </div>
          ))}
        </div>
      ) : items.length === 0 ? (
        <EmptyState icon={Activity} title="No activity yet in this project." description="Actions performed on tasks and project settings will appear here." />
      ) : (
        <>
          <div className="ml-5 border-l-2 border-neutral-150 pl-4">
            {items.map((event) => {
              const metadata = (event.metadata ?? {}) as Record<string, unknown>;
              const entity = String(metadata.taskTitle ?? metadata.projectName ?? '');
              return (
                <div key={event.id} className="relative border-b border-neutral-100 py-2 last:border-b-0">
                  <span className={`absolute -left-[9px] h-4 w-4 rounded-full border-2 bg-white ${actionDotClass(event.action)}`} />
                  <p className="text-12 text-neutral-600">
                    {event.actor.userName} {formatAction(event.action)}
                    {entity ? ` '${entity}'` : ''}
                  </p>
                  <p className="text-11 text-neutral-400">{formatDistanceToNow(new Date(event.occurredAt), { addSuffix: true })}</p>
                </div>
              );
            })}
          </div>

          <Pagination page={data?.page ?? page} pageSize={data?.pageSize ?? 20} totalCount={data?.totalCount ?? 0} onPageChange={setPage} className="mt-3" />
        </>
      )}
    </div>
  );
}
