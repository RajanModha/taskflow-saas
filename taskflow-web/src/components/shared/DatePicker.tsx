import * as Popover from '@radix-ui/react-popover';
import { Calendar } from 'lucide-react';
import { dueDateColor, formatDueDate } from '../../lib/formatters';
import { cn } from '../../lib/utils';

interface DatePickerProps {
  value: string | null;
  onChange: (iso: string | null) => void;
  label?: string;
}

export function DatePicker({ value, onChange, label }: DatePickerProps) {
  const displayDate = value ? formatDueDate(value) : label ?? 'No due date';
  const colorClass = value ? dueDateColor(value) : 'text-neutral-400';

  return (
    <Popover.Root>
      <Popover.Trigger asChild>
        <button type="button" className={cn('inline-flex items-center gap-1.5 text-13 hover:underline', colorClass)}>
          <Calendar className="h-3.5 w-3.5" />
          {displayDate}
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content className="z-50 rounded-md border border-neutral-200 bg-white p-3 shadow-e400" sideOffset={4}>
          <input
            type="date"
            defaultValue={value ? value.slice(0, 10) : ''}
            className="h-8 rounded border border-neutral-200 px-3 text-13 outline-none focus:border-primary-400"
            onChange={(event) => {
              if (!event.target.value) {
                onChange(null);
                return;
              }
              onChange(new Date(event.target.value).toISOString());
            }}
          />
          {value ? (
            <button
              type="button"
              onClick={() => onChange(null)}
              className="mt-2 w-full text-center text-12 text-red-500 hover:text-red-700"
            >
              Clear due date
            </button>
          ) : null}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
