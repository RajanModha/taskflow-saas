import { ChevronLeft, ChevronRight } from 'lucide-react';
import { cn } from '../../lib/utils';

export interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  className?: string;
}

function buildPageList(current: number, total: number): Array<number | 'ellipsis'> {
  if (total <= 0) {
    return [];
  }
  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  const set = new Set<number>();
  set.add(1);
  set.add(total);
  for (let p = current - 2; p <= current + 2; p++) {
    if (p >= 1 && p <= total) {
      set.add(p);
    }
  }

  const nums = [...set].sort((a, b) => a - b);
  const out: Array<number | 'ellipsis'> = [];
  for (let i = 0; i < nums.length; i++) {
    if (i > 0 && nums[i] - nums[i - 1] > 1) {
      out.push('ellipsis');
    }
    out.push(nums[i]);
  }
  return out;
}

export function Pagination({ page, pageSize, totalCount, onPageChange, className }: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const safePage = Math.min(Math.max(1, page), totalPages);
  const from = totalCount === 0 ? 0 : (safePage - 1) * pageSize + 1;
  const to = Math.min(safePage * pageSize, totalCount);
  const items = buildPageList(safePage, totalPages);

  return (
    <div className={cn('mt-4 flex flex-col gap-3 text-13 sm:flex-row sm:items-center sm:justify-between', className)}>
      <p className="text-12 text-neutral-500">
        Showing{' '}
        <span className="text-neutral-700">
          {from}-{to}
        </span>{' '}
        of <span className="text-neutral-700">{totalCount}</span> results
      </p>

      <div className="flex items-center gap-1">
        <button
          type="button"
          className="flex h-7 w-7 items-center justify-center rounded border border-neutral-200 text-neutral-600 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-40"
          disabled={safePage <= 1}
          onClick={() => onPageChange(safePage - 1)}
          aria-label="Previous page"
        >
          <ChevronLeft className="h-3.5 w-3.5" />
        </button>

        {items.map((item, idx) =>
          item === 'ellipsis' ? (
            <span key={`e-${idx}`} className="flex w-7 shrink-0 items-center justify-center text-12 text-neutral-400">
              …
            </span>
          ) : (
            <button
              key={item}
              type="button"
              className={cn(
                'flex h-7 w-7 items-center justify-center rounded border text-12',
                item === safePage
                  ? 'border-primary-600 bg-primary-600 font-medium text-white'
                  : 'border-neutral-200 text-neutral-600 hover:bg-neutral-50',
              )}
              onClick={() => onPageChange(item)}
              aria-label={`Page ${item}`}
              aria-current={item === safePage ? 'page' : undefined}
            >
              {item}
            </button>
          ),
        )}

        <button
          type="button"
          className="flex h-7 w-7 items-center justify-center rounded border border-neutral-200 text-neutral-600 hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-40"
          disabled={safePage >= totalPages}
          onClick={() => onPageChange(safePage + 1)}
          aria-label="Next page"
        >
          <ChevronRight className="h-3.5 w-3.5" />
        </button>
      </div>
    </div>
  );
}
