import { ChevronDown, Search, X } from 'lucide-react';
import type { ReactNode } from 'react';
import { cn } from '../../lib/utils';

export interface ToolbarProps {
  searchValue: string;
  onSearchChange: (v: string) => void;
  searchPlaceholder?: string;
  filters?: ReactNode;
  actions?: ReactNode;
  selectedCount?: number;
  bulkActions?: ReactNode;
  className?: string;
}

export function Toolbar({
  searchValue,
  onSearchChange,
  searchPlaceholder = 'Search…',
  filters,
  actions,
  selectedCount,
  bulkActions,
  className,
}: ToolbarProps) {
  const hasSelection = typeof selectedCount === 'number' && selectedCount > 0;

  return (
    <div className={cn('mb-4 flex h-10 flex-wrap items-center gap-2 lg:flex-nowrap', className)}>
      <div className="relative min-w-0 shrink-0">
        <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-neutral-400" />
        <input
          type="search"
          value={searchValue}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder={searchPlaceholder}
          className={cn(
            'h-8 w-56 rounded border border-neutral-200 bg-white pl-8 text-13 text-neutral-800',
            searchValue.length > 0 ? 'pr-8' : 'pr-3',
            'placeholder:text-neutral-400',
            'focus:border-primary-400 focus:outline-none focus:ring-2 focus:ring-primary-100',
          )}
          aria-label="Search"
        />
        {searchValue.length > 0 ? (
          <button
            type="button"
            className="absolute right-2.5 top-1/2 flex h-3.5 w-3.5 -translate-y-1/2 items-center justify-center rounded text-neutral-400 hover:bg-neutral-100 hover:text-neutral-600"
            onClick={() => onSearchChange('')}
            aria-label="Clear search"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        ) : null}
      </div>

      {filters ? <div className="flex min-w-0 shrink-0 flex-wrap items-center gap-2">{filters}</div> : null}

      <div className="min-h-0 min-w-[1rem] flex-1 basis-full lg:basis-auto" aria-hidden />

      {hasSelection ? (
        <span className="shrink-0 text-13 text-neutral-600">
          <span className="font-medium text-neutral-800">{selectedCount}</span> selected
        </span>
      ) : null}

      {bulkActions ? <div className="flex shrink-0 items-center gap-2">{bulkActions}</div> : null}

      {actions ? <div className="flex shrink-0 items-center gap-2">{actions}</div> : null}
    </div>
  );
}

export interface ToolbarFilterTriggerProps {
  children: ReactNode;
  active?: boolean;
  onClick?: () => void;
  className?: string;
}

/** Use inside the `filters` slot of `Toolbar`. */
export function ToolbarFilterTrigger({
  children,
  active,
  onClick,
  className,
}: ToolbarFilterTriggerProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        'flex h-8 items-center gap-1.5 rounded border px-3 text-13',
        'border-neutral-200 bg-white text-neutral-600 hover:bg-neutral-50',
        active && 'border-primary-400 bg-primary-50 text-primary-700',
        className,
      )}
    >
      {children}
      <ChevronDown className="h-3 w-3 shrink-0 text-neutral-400" aria-hidden />
    </button>
  );
}
