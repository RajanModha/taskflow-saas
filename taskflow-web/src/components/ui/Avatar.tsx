import { useState } from 'react';
import { cn } from '../../lib/utils';

export type AvatarSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

export interface AvatarProps {
  src?: string;
  name: string;
  size?: AvatarSize;
  className?: string;
}

const sizes: Record<AvatarSize, string> = {
  xs: 'h-5 w-5 text-10',
  sm: 'h-6 w-6 text-11',
  md: 'h-7 w-7 text-12',
  lg: 'h-8 w-8 text-13',
  xl: 'h-10 w-10 text-14',
};

/** Deterministic palette: indigo / violet / blue / teal / green / amber tones */
const bgPalette = [
  'bg-indigo-100 text-indigo-800',
  'bg-violet-100 text-violet-800',
  'bg-blue-100 text-blue-800',
  'bg-teal-100 text-teal-800',
  'bg-green-100 text-green-800',
  'bg-amber-100 text-amber-900',
  'bg-indigo-200 text-indigo-900',
  'bg-violet-200 text-violet-900',
  'bg-blue-200 text-blue-900',
  'bg-teal-200 text-teal-900',
];

function getInitials(name: string) {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return '?';
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 1).toUpperCase();
  }
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

function getColorClass(name: string) {
  const hash = name.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0);
  return bgPalette[hash % bgPalette.length];
}

export function Avatar({ src, name, size = 'md', className }: AvatarProps) {
  const [imageError, setImageError] = useState(false);
  const showImg = Boolean(src) && !imageError;

  if (showImg) {
    return (
      <img
        src={src}
        alt={name}
        onError={() => setImageError(true)}
        className={cn('rounded-full object-cover', sizes[size], className)}
      />
    );
  }

  return (
    <div
      aria-label={name}
      title={name}
      className={cn(
        'inline-flex items-center justify-center rounded-full font-semibold',
        sizes[size],
        getColorClass(name),
        className,
      )}
    >
      {getInitials(name)}
    </div>
  );
}
