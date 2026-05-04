import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { ChevronDown } from 'lucide-react';
import { TaskStatus, TaskStatusLabel } from '../../types/api';
import { cn } from '../../lib/utils';

const STATUS_STYLES: Record<TaskStatus, string> = {
  [TaskStatus.Backlog]: 'bg-neutral-100 text-neutral-600',
  [TaskStatus.Todo]: 'bg-primary-50 text-primary-700',
  [TaskStatus.InProgress]: 'bg-amber-50 text-amber-700',
  [TaskStatus.Done]: 'bg-green-50 text-green-700',
  [TaskStatus.Cancelled]: 'bg-neutral-100 text-neutral-500',
};

const STATUS_DOT: Record<TaskStatus, string> = {
  [TaskStatus.Backlog]: 'bg-neutral-400',
  [TaskStatus.Todo]: 'bg-primary-500',
  [TaskStatus.InProgress]: 'bg-amber-500',
  [TaskStatus.Done]: 'bg-green-500',
  [TaskStatus.Cancelled]: 'bg-neutral-400',
};

interface StatusChipProps {
  status: TaskStatus;
  onChange?: (status: TaskStatus) => void;
  readOnly?: boolean;
  size?: 'sm' | 'md';
}

export function StatusChip({ status, onChange, readOnly, size = 'md' }: StatusChipProps) {
  const chip = (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-sm font-medium',
        size === 'sm' ? 'h-[18px] px-2 text-11' : 'h-6 px-2.5 text-12',
        STATUS_STYLES[status],
        !readOnly && 'cursor-pointer hover:opacity-80',
      )}
    >
      <span className={cn('h-1.5 w-1.5 flex-shrink-0 rounded-full', STATUS_DOT[status])} />
      {TaskStatusLabel[status]}
      {!readOnly ? <ChevronDown className="h-3 w-3 opacity-60" /> : null}
    </span>
  );

  if (readOnly || !onChange) return chip;

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>{chip}</DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content className="z-50 w-40 overflow-hidden rounded-md border border-neutral-200 bg-white shadow-e300">
          {(Object.values(TaskStatus).filter((value) => typeof value === 'number') as TaskStatus[]).map((option) => (
            <DropdownMenu.Item
              key={option}
              className="flex h-8 cursor-pointer items-center gap-2 px-3 text-13 text-neutral-700 outline-none hover:bg-neutral-50"
              onSelect={() => onChange(option)}
            >
              <span className={cn('h-1.5 w-1.5 rounded-full', STATUS_DOT[option])} />
              {TaskStatusLabel[option]}
            </DropdownMenu.Item>
          ))}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}
