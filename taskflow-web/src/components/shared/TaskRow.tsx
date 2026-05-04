import { Eye, Lock, MessageSquare, Trash2 } from 'lucide-react';
import { dueDateColor, formatDueDate } from '../../lib/formatters';
import { cn } from '../../lib/utils';
import { PriorityColor, TaskPriorityLabel, type TaskDto } from '../../types/api';
import { Tooltip } from '../ui/Tooltip';
import { Avatar } from '../ui/Avatar';
import { RowMenu } from './RowMenu';
import { StatusChip } from './StatusChip';
import { TagList } from './TagChip';

interface TaskRowProps {
  task: TaskDto;
  selected?: boolean;
  onSelect?: (id: string, checked: boolean) => void;
  onOpen?: (id: string) => void;
  onDelete?: (id: string) => void;
  showProject?: boolean;
}

export function TaskRow({ task, selected, onSelect, onOpen, onDelete }: TaskRowProps) {
  const tags = task.tags ?? [];

  return (
    <tr className="group h-9 cursor-pointer border-b border-neutral-100 hover:bg-neutral-50" onClick={() => onOpen?.(task.id)}>
      {onSelect ? (
        <td className="w-8 px-3" onClick={(event) => event.stopPropagation()}>
          <input
            type="checkbox"
            checked={selected}
            className="h-3.5 w-3.5 rounded border-neutral-300 text-primary-600"
            onChange={(event) => onSelect(task.id, event.target.checked)}
          />
        </td>
      ) : null}
      <td className="w-full max-w-0 px-3">
        <div className="flex items-center gap-2 truncate">
          {task.isBlocked ? (
            <Tooltip content={`Blocked by ${task.blockerCount} task(s)`}>
              <Lock className="h-3 w-3 flex-shrink-0 text-amber-500" />
            </Tooltip>
          ) : null}
          <span className="truncate text-13 font-medium text-neutral-800">{task.title}</span>
          {task.commentCount > 0 ? (
            <span className="flex flex-shrink-0 items-center gap-0.5 text-11 text-neutral-400">
              <MessageSquare className="h-3 w-3" />
              {task.commentCount}
            </span>
          ) : null}
        </div>
      </td>
      <td className="w-28 px-3">
        <StatusChip status={task.status} readOnly size="sm" />
      </td>
      <td className="w-24 px-3">
        <span className="flex items-center gap-1.5 text-12">
          <span className="h-2 w-2 flex-shrink-0 rounded-full" style={{ backgroundColor: PriorityColor[task.priority] }} />
          {TaskPriorityLabel[task.priority]}
        </span>
      </td>
      <td className="w-28 px-3">
        {task.assignee ? (
          <div className="flex items-center gap-1.5 text-12 text-neutral-700">
            <Avatar name={task.assignee.displayName ?? task.assignee.userName} size="xs" />
            <span className="truncate">{task.assignee.displayName ?? task.assignee.userName}</span>
          </div>
        ) : (
          <span className="text-12 text-neutral-300">—</span>
        )}
      </td>
      <td className="w-24 px-3">
        <span className={cn('text-12', dueDateColor(task.dueDateUtc))}>
          {task.dueDateUtc ? formatDueDate(task.dueDateUtc) : <span className="text-neutral-300">—</span>}
        </span>
      </td>
      <td className="w-32 px-3">
        <TagList tags={tags} maxVisible={2} />
      </td>
      <td className="w-10 px-3" onClick={(event) => event.stopPropagation()}>
        <RowMenu
          items={[
            { label: 'View', icon: Eye, onClick: () => onOpen?.(task.id) },
            { label: 'Delete', icon: Trash2, variant: 'danger', onClick: () => onDelete?.(task.id) },
          ]}
        />
      </td>
    </tr>
  );
}
