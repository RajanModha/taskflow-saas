import * as Popover from '@radix-ui/react-popover';
import { Check, ChevronDown, UserCircle } from 'lucide-react';
import { useState } from 'react';
import { useMembers } from '../../hooks/api/workspace.hooks';
import { Avatar } from '../ui/Avatar';
import { cn } from '../../lib/utils';

interface AssigneePickerProps {
  value: string | null;
  onChange: (id: string | null) => void;
  projectId?: string;
  trigger?: React.ReactNode;
}

export function AssigneePicker({ value, onChange, trigger }: AssigneePickerProps) {
  const [q, setQ] = useState('');
  const { data: membersData } = useMembers({ page: 1, pageSize: 50, q });
  const members = membersData?.items ?? [];
  const selected = members.find((member) => member.id === value);

  const defaultTrigger = (
    <button type="button" className="inline-flex items-center gap-1.5 text-13 text-neutral-700 hover:text-primary-700">
      {selected ? (
        <>
          <Avatar name={selected.displayName ?? selected.userName} size="xs" />
          {selected.displayName ?? selected.userName}
        </>
      ) : (
        <>
          <UserCircle className="h-4 w-4 text-neutral-400" />
          <span className="text-neutral-400">Unassigned</span>
        </>
      )}
      <ChevronDown className="h-3 w-3 opacity-60" />
    </button>
  );

  return (
    <Popover.Root>
      <Popover.Trigger asChild>{trigger ?? defaultTrigger}</Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          className="z-50 w-56 overflow-hidden rounded-md border border-neutral-200 bg-white shadow-e400"
          sideOffset={4}
          align="start"
        >
          <div className="border-b border-neutral-100 p-2">
            <input
              autoFocus
              placeholder="Search members..."
              value={q}
              onChange={(event) => setQ(event.target.value)}
              className="h-7 w-full rounded border border-neutral-200 px-2 text-12 outline-none focus:border-primary-400"
            />
          </div>
          <div className="max-h-48 overflow-y-auto py-1">
            <button
              type="button"
              className="flex h-8 w-full items-center gap-2 px-3 text-13 text-neutral-500 hover:bg-neutral-50"
              onClick={() => onChange(null)}
            >
              <UserCircle className="h-4 w-4" /> Unassigned
            </button>
            {members.map((member) => (
              <button
                key={member.id}
                type="button"
                className={cn(
                  'flex h-8 w-full items-center gap-2 px-3 text-13 text-neutral-700 hover:bg-neutral-50',
                  member.id === value && 'bg-primary-50 text-primary-700',
                )}
                onClick={() => onChange(member.id)}
              >
                <Avatar name={member.displayName ?? member.userName} size="xs" />
                <span className="flex-1 truncate text-left">{member.displayName ?? member.userName}</span>
                {member.id === value ? <Check className="h-3 w-3 text-primary-600" /> : null}
              </button>
            ))}
          </div>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
