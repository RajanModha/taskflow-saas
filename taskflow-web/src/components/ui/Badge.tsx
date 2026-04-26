import type { ReactNode } from 'react';
import { cn } from '../../lib/utils';

export type TaskStatusKey = 'todo' | 'progress' | 'done' | 'cancelled';
export type TaskPriorityKey = 'high' | 'medium' | 'low' | 'none';

const statusBadge: Record<TaskStatusKey, string> = {
  todo: 'bg-status-todo-bg text-status-todo-text',
  progress: 'bg-status-progress-bg text-status-progress-text',
  done: 'bg-status-done-bg text-status-done-text',
  cancelled: 'bg-status-cancelled-bg text-status-cancelled-text',
};

const priorityBadge: Record<TaskPriorityKey, string> = {
  high: 'bg-status-high-bg text-status-high-text',
  medium: 'bg-status-medium-bg text-status-medium-text',
  low: 'bg-status-low-bg text-status-low-text',
  none: 'bg-status-none-bg text-status-none-text',
};

const priorityDot: Record<TaskPriorityKey, string> = {
  high: 'bg-red-500',
  medium: 'bg-amber-500',
  low: 'bg-emerald-500',
  none: 'bg-neutral-400',
};

export interface BadgeProps {
  children: ReactNode;
  status?: TaskStatusKey;
  priority?: TaskPriorityKey;
  className?: string;
}

export function Badge({ children, status, priority, className }: BadgeProps) {
  const mode = status ? 'status' : priority ? 'priority' : 'neutral';
  const palette =
    mode === 'status' && status
      ? statusBadge[status]
      : mode === 'priority' && priority
        ? priorityBadge[priority]
        : 'border border-neutral-200 bg-neutral-50 text-neutral-700';

  return (
    <span
      className={cn(
        'inline-flex h-[18px] max-w-full items-center rounded-sm px-2 text-11 font-medium',
        palette,
        className,
      )}
    >
      {mode === 'priority' && priority ? (
        <span
          className={cn('mr-1 h-2 w-2 shrink-0 rounded-full', priorityDot[priority])}
          aria-hidden
        />
      ) : null}
      <span className="min-w-0 truncate">{children}</span>
    </span>
  );
}
