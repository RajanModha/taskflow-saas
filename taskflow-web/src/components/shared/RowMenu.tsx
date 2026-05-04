import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { MoreHorizontal, type LucideIcon } from 'lucide-react';
import { cn } from '../../lib/utils';

interface MenuItem {
  label: string;
  icon?: LucideIcon;
  onClick: () => void;
  variant?: 'default' | 'danger';
}

interface RowMenuProps {
  items: MenuItem[];
}

export function RowMenu({ items }: RowMenuProps) {
  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <button
          type="button"
          className="flex h-7 w-7 items-center justify-center rounded text-neutral-400 opacity-0 hover:bg-neutral-100 hover:text-neutral-700 group-hover:opacity-100"
        >
          <MoreHorizontal className="h-4 w-4" />
        </button>
      </DropdownMenu.Trigger>
      <DropdownMenu.Portal>
        <DropdownMenu.Content className="z-50 min-w-[140px] overflow-hidden rounded-md border border-neutral-200 bg-white shadow-e300" align="end">
          {items.map((item, index) => (
            <DropdownMenu.Item
              key={`${item.label}-${index}`}
              className={cn(
                'flex h-8 cursor-pointer items-center gap-2 px-3 text-13 outline-none',
                item.variant === 'danger' ? 'text-red-600 hover:bg-red-50' : 'text-neutral-700 hover:bg-neutral-50',
              )}
              onSelect={item.onClick}
            >
              {item.icon ? <item.icon className="h-3.5 w-3.5" /> : null}
              {item.label}
            </DropdownMenu.Item>
          ))}
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}
