import { X } from 'lucide-react';
import { cn } from '../../lib/utils';

export interface ActiveFilterChip {
  label: string;
  onRemove: () => void;
}

export interface FilterChipsProps {
  chips: ActiveFilterChip[];
  onClearAll?: () => void;
  className?: string;
}

export function FilterChips({ chips, onClearAll, className }: FilterChipsProps) {
  if (chips.length === 0) {
    return null;
  }

  return (
    <div className={cn('mb-3 flex flex-wrap items-center gap-2', className)}>
      {chips.map((chip, i) => (
        <span
          key={`${chip.label}-${i}`}
          className="inline-flex h-6 max-w-full items-center gap-1.5 rounded-sm border border-primary-200 bg-primary-50 pl-2.5 pr-1.5 text-12 font-medium text-primary-700"
        >
          <span className="min-w-0 truncate">{chip.label}</span>
          <button
            type="button"
            className="flex h-4 w-4 shrink-0 items-center justify-center rounded hover:bg-primary-200"
            onClick={chip.onRemove}
            aria-label={`Remove ${chip.label}`}
          >
            <X className="h-3 w-3" />
          </button>
        </span>
      ))}
      {onClearAll ? (
        <button
          type="button"
          className="text-12 text-neutral-500 hover:text-neutral-700"
          onClick={onClearAll}
        >
          Clear all
        </button>
      ) : null}
    </div>
  );
}
