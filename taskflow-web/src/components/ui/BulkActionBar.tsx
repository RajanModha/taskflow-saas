import { AnimatePresence, motion } from 'framer-motion';
import { X } from 'lucide-react';
import type { ReactNode } from 'react';
import { cn } from '../../lib/utils';

export interface BulkActionBarProps {
  selectedCount: number;
  bulkActions?: ReactNode;
  onDeselect: () => void;
  className?: string;
}

export function BulkActionBar({ selectedCount, bulkActions, onDeselect, className }: BulkActionBarProps) {
  const open = selectedCount > 0;

  return (
    <AnimatePresence>
      {open ? (
        <motion.div
          role="toolbar"
          aria-label="Bulk actions"
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: 16 }}
          transition={{ type: 'spring', damping: 28, stiffness: 320 }}
          className={cn(
            'fixed bottom-4 left-1/2 z-40 flex h-11 max-w-[calc(100vw-2rem)] -translate-x-1/2 items-center gap-3 rounded-lg bg-neutral-800 px-4 text-13 text-white shadow-e400',
            className,
          )}
        >
          <span className="shrink-0 font-medium whitespace-nowrap">{selectedCount} selected</span>
          {bulkActions ? <span className="h-4 w-px shrink-0 bg-neutral-600" aria-hidden /> : null}
          {bulkActions ? (
            <div className="flex min-w-0 flex-1 items-center gap-2 text-white [&_button]:text-white [&_button:hover]:bg-white/10">
              {bulkActions}
            </div>
          ) : (
            <div className="min-w-0 flex-1" />
          )}
          <button
            type="button"
            onClick={onDeselect}
            className="ml-1 flex h-7 w-7 shrink-0 items-center justify-center rounded text-white hover:bg-white/10 hover:text-neutral-200"
            aria-label="Deselect all"
          >
            <X className="h-4 w-4" />
          </button>
        </motion.div>
      ) : null}
    </AnimatePresence>
  );
}
