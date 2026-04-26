import { ChevronDown, ChevronUp } from 'lucide-react';
import type { HTMLAttributes, ThHTMLAttributes, TdHTMLAttributes, ReactNode } from 'react';
import { cn } from '../../lib/utils';

export type SortDir = 'asc' | 'desc';

export function Table({ className, ...props }: HTMLAttributes<HTMLTableElement>) {
  return <table className={cn('w-full border-collapse text-13', className)} {...props} />;
}

export function TableHead({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <thead {...props} className={className} />;
}

export function TableHeadRow({ className, ...props }: HTMLAttributes<HTMLTableRowElement>) {
  return <tr className={cn('border-b border-neutral-200', className)} {...props} />;
}

export interface TableHeadCellProps extends ThHTMLAttributes<HTMLTableCellElement> {
  children: ReactNode;
  sortable?: boolean;
  sortDir?: SortDir | null;
  active?: boolean;
  onSortClick?: () => void;
}

export function TableHeadCell({
  children,
  sortable,
  sortDir,
  active,
  onSortClick,
  className,
  onClick,
  ...props
}: TableHeadCellProps) {
  const sortIcons = sortable ? (
    <span className="inline-flex flex-col leading-none" aria-hidden>
      <ChevronUp
        className={cn('h-3 w-3', active && sortDir === 'asc' ? 'text-primary-600' : 'text-neutral-400')}
      />
      <ChevronDown
        className={cn('-mt-1 h-3 w-3', active && sortDir === 'desc' ? 'text-primary-600' : 'text-neutral-400')}
      />
    </span>
  ) : null;

  return (
    <th
      role={sortable ? 'columnheader' : undefined}
      aria-sort={
        sortable
          ? active && sortDir === 'asc'
            ? 'ascending'
            : active && sortDir === 'desc'
              ? 'descending'
              : 'none'
          : undefined
      }
      className={cn(
        'h-9 whitespace-nowrap bg-neutral-50 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500',
        'first:rounded-tl-md last:rounded-tr-md',
        sortable && 'cursor-pointer select-none hover:bg-neutral-100',
        className,
      )}
      onClick={sortable ? onSortClick : onClick}
      {...props}
    >
      <span className="inline-flex items-center gap-1">
        {children}
        {sortIcons}
      </span>
    </th>
  );
}

export function TableBody({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) {
  return <tbody {...props} className={className} />;
}

export function TableRow({ className, ...props }: HTMLAttributes<HTMLTableRowElement>) {
  return (
    <tr
      className={cn(
        'group h-9 cursor-pointer border-b border-neutral-100 transition-colors duration-75 hover:bg-neutral-50',
        className,
      )}
      {...props}
    />
  );
}

export function TableCell({ className, ...props }: TdHTMLAttributes<HTMLTableCellElement>) {
  return <td className={cn('px-3 text-13 text-neutral-800', className)} {...props} />;
}
