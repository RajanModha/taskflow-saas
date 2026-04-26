import type { LucideIcon } from 'lucide-react';
import { Bell, CheckSquare, FolderOpen, Users } from 'lucide-react';
import { cn } from '../../lib/utils';
import { Button } from './Button';

export interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description?: string;
  action?: { label: string; onClick: () => void };
  size?: 'sm' | 'md';
  className?: string;
}

function EmptyStateRoot({ icon: Icon, title, description, action, size = 'md', className }: EmptyStateProps) {
  const hasDescription = Boolean(description?.trim());

  return (
    <div className={cn('text-center', size === 'md' ? 'py-16' : 'py-8', className)}>
      <Icon className="mx-auto mb-3 h-10 w-10 text-neutral-300" aria-hidden />
      <h2 className="mb-1 text-14 font-medium text-neutral-600">{title}</h2>
      {hasDescription ? (
        <p className="mx-auto mb-4 max-w-xs text-13 text-neutral-400">{description}</p>
      ) : action ? (
        <div className="mb-4" />
      ) : null}
      {action ? (
        <Button type="button" size="sm" variant="primary" onClick={action.onClick}>
          {action.label}
        </Button>
      ) : null}
    </div>
  );
}

function NoProjects(props: Omit<EmptyStateProps, 'icon' | 'title' | 'description'>) {
  return (
    <EmptyStateRoot
      icon={FolderOpen}
      title="No projects yet"
      description="Create your first project"
      {...props}
    />
  );
}

function NoTasks(props: Omit<EmptyStateProps, 'icon' | 'title' | 'description'>) {
  return (
    <EmptyStateRoot
      icon={CheckSquare}
      title="No tasks found"
      description="Try adjusting your filters"
      {...props}
    />
  );
}

function NoNotifications(props: Omit<EmptyStateProps, 'icon' | 'title' | 'description'>) {
  return <EmptyStateRoot icon={Bell} title="You're all caught up" {...props} />;
}

function NoMembers(props: Omit<EmptyStateProps, 'icon' | 'title' | 'description'>) {
  return <EmptyStateRoot icon={Users} title="No members found" {...props} />;
}

export const EmptyState = Object.assign(EmptyStateRoot, {
  NoProjects,
  NoTasks,
  NoNotifications,
  NoMembers,
});
