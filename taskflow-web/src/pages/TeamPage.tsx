import { Copy, RotateCw, UserPlus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Avatar } from '../components/ui/Avatar';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Select } from '../components/ui/Select';
import { cn } from '../lib/utils';

type MemberRole = 'Owner' | 'Admin' | 'Member';

type Member = {
  id: string;
  name: string;
  email: string;
  role: MemberRole;
  joinedAt: string;
};

const workspace = { joinCode: 'TFLOW-7H9K' };
const isOwner = true;
const isAdmin = true;

const MEMBERS: Member[] = [
  { id: 'm1', name: 'Alex Johnson', email: 'alex@taskflow.dev', role: 'Owner', joinedAt: '2025-12-01' },
  { id: 'm2', name: 'Priya Sharma', email: 'priya@taskflow.dev', role: 'Admin', joinedAt: '2026-01-12' },
  { id: 'm3', name: 'Chris Lee', email: 'chris@taskflow.dev', role: 'Member', joinedAt: '2026-02-03' },
  { id: 'm4', name: 'Nina Patel', email: 'nina@taskflow.dev', role: 'Member', joinedAt: '2026-03-14' },
];

function MemberRow({ member }: { member: Member }) {
  return (
    <tr className="h-9 border-b border-neutral-100 hover:bg-neutral-50">
      <td className="px-3">
        <div className="flex items-center gap-2">
          <Avatar name={member.name} size="sm" />
          <div className="min-w-0">
            <p className="truncate text-13 font-medium text-neutral-800">{member.name}</p>
            <p className="truncate text-12 text-neutral-500">{member.email}</p>
          </div>
        </div>
      </td>
      <td className="w-24 px-3 text-12 text-neutral-600">{member.role}</td>
      <td className="w-32 px-3 text-12 text-neutral-500">{new Date(member.joinedAt).toLocaleDateString()}</td>
      <td className="w-16 px-3 text-right">
        <Button size="xs" variant="ghost">Edit</Button>
      </td>
    </tr>
  );
}

function InvitesSection({ className }: { className?: string }) {
  return (
    <div className={cn('overflow-hidden rounded-md border border-neutral-200 bg-white', className)}>
      <div className="border-b border-neutral-200 px-4 py-2.5">
        <p className="text-13 font-medium text-neutral-700">Pending invites</p>
      </div>
      <div className="px-4 py-3 text-13 text-neutral-500">No pending invites.</div>
    </div>
  );
}

export default function TeamPage() {
  const [query, setQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState('all');

  const members = useMemo(() => {
    const q = query.trim().toLowerCase();
    return MEMBERS.filter((m) => {
      const matchesQuery = !q || m.name.toLowerCase().includes(q) || m.email.toLowerCase().includes(q);
      const matchesRole = roleFilter === 'all' || m.role.toLowerCase() === roleFilter;
      return matchesQuery && matchesRole;
    });
  }, [query, roleFilter]);

  function copyCode() {
    void navigator.clipboard?.writeText(workspace.joinCode);
  }

  return (
    <div className="page-wrapper">
      <div className="page-header">
        <div>
          <h1 className="page-title">Team</h1>
          <p className="page-subtitle">Members and roles</p>
        </div>
      </div>

      <div className="mb-4 flex items-center justify-between rounded-md border border-primary-100 bg-primary-50 p-4">
        <div>
          <p className="text-13 font-medium text-neutral-700">Workspace join code</p>
          <p className="mt-0.5 text-12 text-neutral-500">Share this code to invite people directly</p>
        </div>
        <div className="flex items-center gap-2">
          <code className="flex h-8 items-center rounded border border-primary-200 bg-white px-3 font-mono text-14 font-bold text-primary-700">
            {workspace.joinCode}
          </code>
          <Button size="sm" variant="secondary" leftIcon={<Copy className="h-3.5 w-3.5" />} onClick={copyCode}>
            Copy
          </Button>
          {isOwner ? (
            <Button size="sm" variant="ghost" leftIcon={<RotateCw className="h-3.5 w-3.5" />}>
              Regenerate
            </Button>
          ) : null}
        </div>
      </div>

      <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
        <div className="flex items-center gap-3 border-b border-neutral-200 px-4 py-2.5">
          <Input
            placeholder="Search members..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="h-7 w-48 text-12"
          />
          <Select
            value={roleFilter}
            onChange={setRoleFilter}
            options={[
              { label: 'Role', value: 'all' },
              { label: 'Owner', value: 'owner' },
              { label: 'Admin', value: 'admin' },
              { label: 'Member', value: 'member' },
            ]}
            className="w-28"
            triggerClassName="h-7 text-12"
          />
          <div className="flex-1" />
          <p className="text-12 text-neutral-500">{members.length} members</p>
          {isAdmin ? (
            <Button size="sm" variant="primary" leftIcon={<UserPlus className="h-3.5 w-3.5" />}>
              Invite
            </Button>
          ) : null}
        </div>

        <table className="w-full border-collapse text-13">
          <thead>
            <tr className="border-b border-neutral-200 bg-neutral-50">
              <th className="h-9 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Member</th>
              <th className="h-9 w-24 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Role</th>
              <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Joined</th>
              <th className="h-9 w-16 px-3" />
            </tr>
          </thead>
          <tbody>
            {members.map((m) => (
              <MemberRow key={m.id} member={m} />
            ))}
          </tbody>
        </table>
      </div>

      {isAdmin ? <InvitesSection className="mt-4" /> : null}
    </div>
  );
}
