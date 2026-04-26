import type { HTMLAttributes } from 'react';
import { cn } from '../../lib/utils';
import type { AvatarSize } from './Avatar';

const avatarSizeClass: Record<AvatarSize, string> = {
  xs: 'h-5 w-5',
  sm: 'h-6 w-6',
  md: 'h-7 w-7',
  lg: 'h-8 w-8',
  xl: 'h-10 w-10',
};

export interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {}

function SkeletonBase({ className, ...props }: SkeletonProps) {
  return <div className={cn('animate-pulse rounded bg-neutral-200', className)} {...props} />;
}

function SkeletonBlock(props: Omit<SkeletonProps, 'children'>) {
  return <SkeletonBase className={cn('h-4 w-full', props.className)} {...props} />;
}

function SkeletonText(props: Omit<SkeletonProps, 'children'>) {
  return <SkeletonBase className={cn('h-3 w-3/4', props.className)} {...props} />;
}

function SkeletonAvatar({ size = 'sm', className, ...props }: Omit<SkeletonProps, 'children'> & { size?: AvatarSize }) {
  return (
    <SkeletonBase
      className={cn('rounded-full', avatarSizeClass[size], className)}
      {...props}
    />
  );
}

export const Skeleton = Object.assign(SkeletonBase, {
  Block: SkeletonBlock,
  Text: SkeletonText,
  Avatar: SkeletonAvatar,
});
