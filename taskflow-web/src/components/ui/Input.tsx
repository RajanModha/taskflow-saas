import { AlertCircle } from 'lucide-react';
import { forwardRef, useId } from 'react';
import type { InputHTMLAttributes, ReactNode } from 'react';
import { cn } from '../../lib/utils';

export interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  hint?: string;
  leftIcon?: ReactNode;
  rightIcon?: ReactNode;
  required?: boolean;
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, hint, leftIcon, rightIcon, className, id, required, ...props }, ref) => {
    const autoId = useId();
    const inputId = id ?? autoId;

    return (
      <div className="w-full">
        {label ? (
          <label htmlFor={inputId} className="mb-1 block text-12 font-medium text-neutral-700">
            {label}
            {required ? <span className="ml-0.5 text-red-600">*</span> : null}
          </label>
        ) : null}

        <div className="relative">
          {leftIcon ? (
            <span className="pointer-events-none absolute left-2.5 top-1/2 flex h-3.5 w-3.5 -translate-y-1/2 items-center text-neutral-400">
              {leftIcon}
            </span>
          ) : null}
          <input
            id={inputId}
            ref={ref}
            className={cn(
              'h-8 w-full rounded border bg-white px-3 text-13 text-neutral-800 transition-shadow duration-100',
              'placeholder:text-neutral-400',
              'focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-200',
              'disabled:cursor-not-allowed disabled:bg-neutral-50 disabled:text-neutral-500',
              leftIcon ? 'pl-8' : undefined,
              rightIcon ? 'pr-8' : undefined,
              error
                ? 'border-red-400 focus:border-red-400 focus:ring-red-100'
                : 'border-neutral-300',
              className,
            )}
            aria-invalid={Boolean(error)}
            aria-describedby={
              error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined
            }
            required={required}
            {...props}
          />
          {rightIcon ? (
            <span className="pointer-events-none absolute right-2.5 top-1/2 flex h-3.5 w-3.5 -translate-y-1/2 items-center text-neutral-400">
              {rightIcon}
            </span>
          ) : null}
        </div>

        {error ? (
          <p id={`${inputId}-error`} className="mt-1 flex items-center gap-1 text-12 text-red-600">
            <AlertCircle className="h-3.5 w-3.5 shrink-0" />
            {error}
          </p>
        ) : hint ? (
          <p id={`${inputId}-hint`} className="mt-1 text-12 text-neutral-500">
            {hint}
          </p>
        ) : null}
      </div>
    );
  },
);

Input.displayName = 'Input';
