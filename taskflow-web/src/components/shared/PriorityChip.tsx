import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { ChevronDown } from 'lucide-react';
import { TaskPriority, TaskPriorityLabel } from '../../types/api';
import { cn } from '../../lib/utils';

const PRIORITY_STYLES: Record<TaskPriority, string> = {
  [TaskPriority.None]: 'bg-neutral-100 text-neutral-600',
  [TaskPriority.Low]: 'bg-blue-50 text-blue-700',
  [TaskPriority.Medium]: 'bg-amber-50 text-amber-700',
  [TaskPriority.High]: 'bg-red-50 text-red-700',
};

const PRIORITY_DOT: Record<TaskPriority, string> = {
  [TaskPriority.None]: 'bg-neutral-300',
  [TaskPriority.Low]: 'bg-blue-400',
  [TaskPriority.Medium]: 'bg-amber-400',
  [TaskPriority.High]: 'bg-red-500',
};

interface PriorityChipProps {
  priority: TaskPriority;
  onChange?: (priority: TaskPriority) => void;
  readOnly?: boolean;
  size?: 'sm' | 'md';
}

export function PriorityChip({ priority, onChange, readOnly, size = 'md' }: PriorityChipProps) {
  const chip = (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-sm font-medium',
        size === 'sm' ? 'h-[18px] px-2 text-11' : 'h-6 px-2.5 text-12',
        PRIORITY_STYLES[priority],
        !readOnly && 'cursor-pointer hover:opacity-80',
      )}
    >
      <span className={cn('h-1.5 w-1.5 flex-shrink-0 rounded-full', PRIORITY_DOT[priority])} />
      {TaskPriorityLabel[priority]}
      {!readOnly ? <ChevronDown className="h-3 w-3 opacity-60" /> : null}
    </span>
  );

  if (readOnly || !onChange) return chip;

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>{chip}</DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content className="z-50 w-40 overflow-hidden rounded-md border border-neutral-200 bg-white shadow-e300">
          {(Object.values(TaskPriority).filter((value) => typeof value === 'number') as TaskPriority[]).map((option) => (
            <DropdownMenu.Item
              key={option}
              className="flex h-8 cursor-pointer items-center gap-2 px-3 text-13 text-neutral-700 outline-none hover:bg-neutral-50"
              onSelect={() => onChange(option)}
            >
              <span className={cn('h-1.5 w-1.5 rounded-full', PRIORITY_DOT[option])} />
              {TaskPriorityLabel[option]}
            </DropdownMenu.Item>
          ))}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}
