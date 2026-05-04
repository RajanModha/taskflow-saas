import { differenceInDays, format, formatDistanceToNow, isPast } from 'date-fns';

export function formatDate(iso: string | null | undefined, fmt = 'MMM d, yyyy'): string {
  if (!iso) return '—';
  return format(new Date(iso), fmt);
}

export function formatRelative(iso: string | null | undefined): string {
  if (!iso) return '';
  return formatDistanceToNow(new Date(iso), { addSuffix: true });
}

export function dueDateColor(iso: string | null | undefined): string {
  if (!iso) return 'text-neutral-400';
  const date = new Date(iso);
  if (isPast(date)) return 'text-red-600';
  if (differenceInDays(date, new Date()) <= 2) return 'text-amber-600';
  return 'text-neutral-500';
}

export function formatDueDate(iso: string | null | undefined): string {
  if (!iso) return 'No due date';
  const date = new Date(iso);
  if (differenceInDays(date, new Date()) === 0) return 'Today';
  if (differenceInDays(date, new Date()) === 1) return 'Tomorrow';
  return format(date, 'MMM d');
}

export function formatActivityAction(action: string): string {
  const map: Record<string, string> = {
    'task.created': 'created task',
    'task.status_changed': 'changed status of',
    'task.priority_changed': 'changed priority of',
    'task.assigned': 'was assigned',
    'task.unassigned': 'was unassigned from',
    'task.due_date_changed': 'changed due date of',
    'task.commented': 'commented on',
    'task.deleted': 'deleted task',
    'task.restored': 'restored task',
    'task.tag_added': 'added tag to',
    'task.checklist_item_completed': 'completed checklist item in',
    'project.created': 'created project',
    'project.updated': 'updated project',
    'project.deleted': 'deleted project',
  };
  return map[action] ?? action.replace(/\./g, ' ');
}

export function avatarColor(name: string): string {
  const colors = [
    'bg-indigo-500',
    'bg-violet-500',
    'bg-blue-500',
    'bg-cyan-500',
    'bg-teal-500',
    'bg-green-500',
    'bg-amber-500',
    'bg-orange-500',
    'bg-rose-500',
    'bg-pink-500',
  ];
  let hash = 0;
  for (let i = 0; i < name.length; i += 1) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return colors[Math.abs(hash) % colors.length];
}

export function getInitials(name: string): string {
  const words = name.trim().split(/\s+/);
  if (words.length === 1) return words[0][0]?.toUpperCase() ?? '?';
  return (words[0][0] + words[words.length - 1][0]).toUpperCase();
}
