import * as Dialog from '@radix-ui/react-dialog';
import { X } from 'lucide-react';
import type { ReactNode } from 'react';
import { cn } from '../../lib/utils';

type ModalSize = 'sm' | 'md' | 'lg';

export interface ModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description?: string;
  size?: ModalSize;
  children: ReactNode;
}

const sizeMap: Record<ModalSize, string> = {
  sm: 'max-w-md',
  md: 'max-w-xl',
  lg: 'max-w-3xl',
};

export function Modal({
  open,
  onOpenChange,
  title,
  description,
  size = 'md',
  children,
}: ModalProps) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-surface-overlay data-[state=open]:animate-fade-in" />
        <Dialog.Content
          className={cn(
            'fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] -translate-x-1/2 -translate-y-1/2',
            'rounded-lg border border-neutral-200 bg-surface-raised p-4 shadow-modal',
            'focus:outline-none data-[state=open]:animate-scale-in',
            sizeMap[size],
          )}
        >
          <div className="mb-3 flex items-start justify-between gap-2">
            <div className="min-w-0">
              <Dialog.Title className="text-13 font-semibold text-neutral-800">{title}</Dialog.Title>
              {description ? (
                <Dialog.Description className="mt-0.5 text-12 text-neutral-500">
                  {description}
                </Dialog.Description>
              ) : null}
            </div>

            <Dialog.Close className="rounded p-1 text-neutral-500 transition-colors hover:bg-neutral-100 hover:text-neutral-700">
              <X className="h-3.5 w-3.5" />
              <span className="sr-only">Close</span>
            </Dialog.Close>
          </div>

          {children}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
