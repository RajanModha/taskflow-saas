import { X } from 'lucide-react';
import type { TagDto } from '../../types/api';
import { cn } from '../../lib/utils';

interface TagChipProps {
  tag: TagDto;
  onRemove?: () => void;
  size?: 'sm' | 'md';
}

export function TagChip({ tag, onRemove, size = 'md' }: TagChipProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-sm font-medium',
        size === 'sm' ? 'h-[16px] px-1.5 text-10' : 'h-[18px] px-2 text-11',
      )}
      style={{ backgroundColor: `${tag.color}22`, color: tag.color, border: `1px solid ${tag.color}44` }}
    >
      {tag.name}
      {onRemove ? (
        <button
          type="button"
          onClick={(event) => {
            event.stopPropagation();
            onRemove();
          }}
          className="ml-0.5 rounded hover:opacity-70"
        >
          <X className="h-2.5 w-2.5" />
        </button>
      ) : null}
    </span>
  );
}

interface TagListProps {
  tags: TagDto[];
  maxVisible?: number;
  onRemove?: (tagId: string) => void;
}

export function TagList({ tags, maxVisible = 2, onRemove }: TagListProps) {
  const visible = tags.slice(0, maxVisible);
  const overflow = tags.length - maxVisible;

  return (
    <div className="flex flex-wrap items-center gap-1">
      {visible.map((tag) => (
        <TagChip key={tag.id} tag={tag} onRemove={onRemove ? () => onRemove(tag.id) : undefined} size="sm" />
      ))}
      {overflow > 0 ? <span className="text-10 font-medium text-neutral-400">+{overflow}</span> : null}
    </div>
  );
}
