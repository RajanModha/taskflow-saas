import { forwardRef } from 'react';
import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { cn } from '../../lib/utils';
import { Spinner } from './Spinner';

export type ButtonVariant =
  | 'primary'
  | 'secondary'
  | 'ghost'
  | 'subtle'
  | 'danger'
  | 'danger-ghost'
  | 'link';

export type ButtonSize = 'xs' | 'sm' | 'md' | 'lg';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
  leftIcon?: ReactNode;
  rightIcon?: ReactNode;
}

const sizeClasses: Record<ButtonSize, string> = {
  xs: 'h-6 gap-1 px-2 text-11',
  sm: 'h-7 gap-1.5 px-3 text-12',
  md: 'h-8 gap-2 px-3 text-13',
  lg: 'h-9 gap-2 px-4 text-14',
};

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'bg-primary-600 !text-white shadow-e100 hover:bg-primary-700 active:bg-primary-800',
  secondary: 'border border-neutral-300 bg-white text-neutral-700 hover:bg-neutral-50 active:bg-neutral-100',
  ghost: 'text-neutral-600 hover:bg-neutral-100 active:bg-neutral-150',
  subtle: 'bg-neutral-100 text-neutral-700 hover:bg-neutral-150',
  danger: 'bg-red-600 !text-white hover:bg-red-700',
  'danger-ghost': 'text-red-600 hover:bg-red-50',
  link: 'h-auto p-0 text-primary-600 hover:text-primary-700 hover:underline',
};

function spinnerColor(variant: ButtonVariant): 'white' | 'neutral' {
  if (variant === 'primary' || variant === 'danger') {
    return 'white';
  }
  return 'neutral';
}

function spinnerSize(size: ButtonSize): 'xs' | 'sm' {
  return size === 'xs' || size === 'sm' ? 'xs' : 'sm';
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(
  (
    {
      className,
      variant = 'primary',
      size = 'md',
      loading = false,
      disabled,
      children,
      type,
      leftIcon,
      rightIcon,
      ...props
    },
    ref,
  ) => {
    const isDisabled = disabled || loading;
    const isLink = variant === 'link';

    return (
      <button
        ref={ref}
        type={type ?? 'button'}
        className={cn(
          'inline-flex select-none items-center justify-center whitespace-nowrap font-medium transition-colors duration-100',
          'rounded focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-1',
          'disabled:cursor-not-allowed disabled:opacity-50',
          variantClasses[variant],
          !isLink && sizeClasses[size],
          isLink && 'min-h-0 rounded-none ring-offset-0',
          className,
        )}
        disabled={isDisabled}
        aria-busy={loading}
        {...props}
      >
        {loading ? (
          <Spinner size={spinnerSize(size)} color={spinnerColor(variant)} />
        ) : (
          <>
            {leftIcon ? <span className="shrink-0">{leftIcon}</span> : null}
            {children}
            {rightIcon ? <span className="shrink-0">{rightIcon}</span> : null}
          </>
        )}
      </button>
    );
  },
);

Button.displayName = 'Button';
