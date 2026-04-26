import { cn } from '../../lib/utils';

type SpinnerSize = 'xs' | 'sm' | 'md' | 'lg';
type SpinnerColor = 'primary' | 'white' | 'neutral';

export interface SpinnerProps {
  size?: SpinnerSize;
  color?: SpinnerColor;
  className?: string;
}

const sizeMap: Record<SpinnerSize, string> = {
  xs: 'h-3 w-3 border-[1.5px]',
  sm: 'h-3.5 w-3.5 border-2',
  md: 'h-4 w-4 border-2',
  lg: 'h-5 w-5 border-2',
};

const colorMap: Record<SpinnerColor, string> = {
  primary: 'border-primary-200 border-t-primary-600',
  white: 'border-white/40 border-t-white',
  neutral: 'border-neutral-200 border-t-neutral-500',
};

export function Spinner({ size = 'sm', color = 'primary', className }: SpinnerProps) {
  return (
    <span
      className={cn('inline-block animate-spin rounded-full border-solid', sizeMap[size], colorMap[color], className)}
      aria-hidden="true"
    />
  );
}
