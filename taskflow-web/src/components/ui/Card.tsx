import type { HTMLAttributes, ReactNode } from 'react';
import { cn } from '../../lib/utils';

export interface CardProps extends HTMLAttributes<HTMLDivElement> {
  /** Use shadow when the card should read as elevated (e.g. above dense lists). */
  elevated?: boolean;
}

export function Card({ className, children, elevated, ...props }: CardProps) {
  return (
    <div
      className={cn(
        'rounded-md border border-neutral-200 bg-white p-4',
        elevated && 'shadow-e100',
        className,
      )}
      {...props}
    >
      {children}
    </div>
  );
}

export interface CardHeaderProps {
  title: ReactNode;
  subtitle?: ReactNode;
  actions?: ReactNode;
  className?: string;
}

export function CardHeader({ title, subtitle, actions, className }: CardHeaderProps) {
  return (
    <div
      className={cn(
        'mb-3 flex items-center justify-between border-b border-neutral-100 pb-3',
        className,
      )}
    >
      <div className="min-w-0">
        <div className="text-13 font-semibold text-neutral-800">{title}</div>
        {subtitle ? <div className="text-12 text-neutral-500">{subtitle}</div> : null}
      </div>
      {actions ? <div className="ml-3 flex shrink-0 items-center gap-2">{actions}</div> : null}
    </div>
  );
}

export function CardSection({ className, children, ...props }: HTMLAttributes<HTMLDivElement>) {
  return (
    <div className={cn('flex flex-col gap-3', className)} {...props}>
      {children}
    </div>
  );
}
