import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { zodResolver } from '@hookform/resolvers/zod';
import { formatDistanceToNow } from 'date-fns';
import { Trash2, UserPlus } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { Avatar } from '../components/ui/Avatar';
import { Button } from '../components/ui/Button';
import { Input } from '../components/ui/Input';
import { Modal } from '../components/ui/Modal';
import { JoinCodeCard } from '../components/settings/JoinCodeCard';
import { Select } from '../components/ui/Select';
import { useMe } from '../hooks/api/auth.hooks';
import {
  useCancelInvite,
  useInviteMember,
  useInvites,
  useMembers,
  useRegenerateJoinCode,
  useRemoveMember,
  useResendInvite,
  useUpdateMemberRole,
  useWorkspaceMe,
} from '../hooks/api/workspace.hooks';
import type { MyWorkspaceResponse, WorkspaceMemberRowDto } from '../types/api';

const inviteSchema = z.object({
  email: z.string().email('Enter a valid email'),
  role: z.enum(['Admin', 'Member']),
});

type InviteValues = z.infer<typeof inviteSchema>;

function roleBadge(role: string) {
  if (role === 'Owner') return 'bg-violet-100 text-violet-700';
  if (role === 'Admin') return 'bg-indigo-100 text-indigo-700';
  return 'bg-neutral-100 text-neutral-600';
}

export default function TeamPage() {
  const queryClient = useQueryClient();
  const { data: workspace } = useWorkspaceMe();
  const [q, setQ] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const { data: membersData } = useMembers({ page: 1, pageSize: 50, q, role: roleFilter || undefined });
  const { data: invites = [] } = useInvites();
  const { data: me } = useMe();

  const inviteMember = useInviteMember();
  const resendInvite = useResendInvite();
  const cancelInvite = useCancelInvite();
  const regenerateJoinCode = useRegenerateJoinCode();
  const updateMemberRole = useUpdateMemberRole();
  const removeMember = useRemoveMember();

  const [inviteOpen, setInviteOpen] = useState(false);
  const [roleDialogMember, setRoleDialogMember] = useState<WorkspaceMemberRowDto | null>(null);
  const [nextRole, setNextRole] = useState<'Admin' | 'Member'>('Member');
  const [removeDialogMember, setRemoveDialogMember] = useState<WorkspaceMemberRowDto | null>(null);

  const inviteForm = useForm<InviteValues>({
    resolver: zodResolver(inviteSchema),
    defaultValues: { email: '', role: 'Member' },
  });

  const isAdmin = ['Owner', 'Admin'].includes(workspace?.currentUserRole ?? '');
  const isOwner = workspace?.currentUserRole === 'Owner';
  const members = membersData?.items ?? [];
  const pendingInvites = invites.filter((invite) => invite.status === 'Pending');

  const onRegenerateJoinCode = () => {
    if (!workspace) return;
    const yes = window.confirm('Old code will stop working immediately.');
    if (!yes) return;
    regenerateJoinCode.mutate(undefined, {
      onSuccess: (data) => {
        queryClient.setQueryData<MyWorkspaceResponse | undefined>(['workspace', 'me'], (current) =>
          current ? { ...current, joinCode: data.joinCode } : current,
        );
        toast.success('Join code regenerated');
      },
      onError: () => toast.error('Failed to regenerate code'),
    });
  };

  const onInviteSubmit = inviteForm.handleSubmit(async (values) => {
    try {
      await inviteMember.mutateAsync(values);
      toast.success('Invite sent');
      inviteForm.reset({ email: '', role: 'Member' });
      setInviteOpen(false);
    } catch {
      toast.error('Failed to send invite');
    }
  });

  const memberRows = useMemo(
    () =>
      members.map((member) => ({
        ...member,
        name: member.displayName ?? member.userName,
        isMe: me?.id === member.id,
        canManage: isAdmin && member.id !== me?.id && member.role !== 'Owner',
      })),
    [isAdmin, me?.id, members],
  );

  return (
    <div className="page-wrapper">
      <div className="page-header">
        <div>
          <h1 className="page-title">Team</h1>
          <p className="page-subtitle">{workspace?.name ?? 'Workspace'}</p>
        </div>
        {isAdmin ? (
          <Button size="sm" variant="primary" leftIcon={<UserPlus className="h-3.5 w-3.5" />} onClick={() => setInviteOpen(true)}>
            Invite member
          </Button>
        ) : null}
      </div>

      <JoinCodeCard joinCode={workspace?.joinCode} isOwner={isOwner} onRegenerate={onRegenerateJoinCode} regenerating={regenerateJoinCode.isPending} className="mb-4" />

      <div className="mb-4 rounded-md border border-neutral-200 bg-white">
        <div className="flex items-center gap-2 border-b border-neutral-200 px-4 py-2.5">
          <Input placeholder="Search members..." value={q} onChange={(event) => setQ(event.target.value)} className="h-7 w-48 text-12" />
          <Select
            className="w-36"
            value={roleFilter}
            onChange={(value) => setRoleFilter(value)}
            options={[
              { label: 'All roles', value: '' },
              { label: 'Owner', value: 'Owner' },
              { label: 'Admin', value: 'Admin' },
              { label: 'Member', value: 'Member' },
            ]}
            triggerClassName="h-7 text-12"
          />
          <div className="ml-auto text-12 text-neutral-500">{membersData?.totalCount ?? 0} members</div>
        </div>

        <table className="w-full border-collapse text-13">
          <thead>
            <tr className="border-b border-neutral-200 bg-neutral-50">
              <th className="h-9 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Member</th>
              <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Role</th>
              <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Joined</th>
              <th className="h-9 w-20 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Actions</th>
            </tr>
          </thead>
          <tbody>
            {memberRows.map((member) => (
              <tr key={member.id} className="h-9 border-b border-neutral-100 hover:bg-neutral-50">
                <td className="px-3">
                  <div className="flex items-center gap-2">
                    <Avatar name={member.name} size="md" />
                    <div className="min-w-0">
                      <div className="flex items-center gap-1.5">
                        <p className="truncate text-13 font-medium text-neutral-800">{member.name}</p>
                        {member.isMe ? <span className="rounded bg-neutral-100 px-1.5 py-0.5 text-10 text-neutral-600">(You)</span> : null}
                      </div>
                      <p className="truncate text-12 text-neutral-400">{member.email}</p>
                    </div>
                  </div>
                </td>
                <td className="px-3">
                  <span className={`inline-flex rounded-full px-2 py-0.5 text-11 font-medium ${roleBadge(member.role)}`}>{member.role}</span>
                </td>
                <td className="px-3 text-12 text-neutral-500">{formatDistanceToNow(new Date(member.joinedAt), { addSuffix: true })}</td>
                <td className="px-3">
                  {member.canManage ? (
                    <DropdownMenu.Root>
                      <DropdownMenu.Trigger asChild>
                        <button type="button" className="rounded px-2 py-1 text-12 text-neutral-600 hover:bg-neutral-100">
                          Manage
                        </button>
                      </DropdownMenu.Trigger>
                      <DropdownMenu.Portal>
                        <DropdownMenu.Content sideOffset={8} align="end" className="z-50 min-w-[160px] rounded-md border border-neutral-200 bg-white py-1 shadow-e200">
                          <DropdownMenu.Item
                            onSelect={() => {
                              setRoleDialogMember(member);
                              setNextRole(member.role === 'Admin' ? 'Admin' : 'Member');
                            }}
                            className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50"
                          >
                            Change role
                          </DropdownMenu.Item>
                          <DropdownMenu.Separator className="my-1 h-px bg-neutral-150" />
                          <DropdownMenu.Item
                            onSelect={() => setRemoveDialogMember(member)}
                            className="cursor-pointer px-3 py-2 text-13 text-red-600 outline-none data-[highlighted]:bg-red-50"
                          >
                            Remove member
                          </DropdownMenu.Item>
                        </DropdownMenu.Content>
                      </DropdownMenu.Portal>
                    </DropdownMenu.Root>
                  ) : (
                    <span className="text-12 text-neutral-400">—</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {isAdmin && pendingInvites.length > 0 ? (
        <div className="overflow-hidden rounded-md border border-neutral-200 bg-white">
          <div className="border-b border-neutral-200 px-4 py-2.5">
            <p className="text-13 font-medium text-neutral-700">Pending Invites</p>
          </div>
          <table className="w-full border-collapse text-13">
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50">
                <th className="h-9 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Email</th>
                <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Role</th>
                <th className="h-9 w-32 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Sent</th>
                <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Status</th>
                <th className="h-9 w-28 px-3 text-left text-11 font-semibold uppercase tracking-wide text-neutral-500">Actions</th>
              </tr>
            </thead>
            <tbody>
              {pendingInvites.map((invite) => {
                const isExpired = invite.status === 'Expired';
                const resendDisabled = invite.resendCount >= 5 || isExpired;
                return (
                  <tr key={invite.id} className="h-9 border-b border-neutral-100 hover:bg-neutral-50">
                    <td className="px-3 text-neutral-700">{invite.email}</td>
                    <td className="px-3">
                      <span className={`inline-flex rounded-full px-2 py-0.5 text-11 font-medium ${roleBadge(invite.role)}`}>{invite.role}</span>
                    </td>
                    <td className="px-3 text-12 text-neutral-500">{formatDistanceToNow(new Date(invite.sentAt), { addSuffix: true })}</td>
                    <td className="px-3">
                      <span
                        className={`inline-flex rounded-full px-2 py-0.5 text-11 font-medium ${
                          invite.status === 'Pending' ? 'bg-amber-100 text-amber-700' : 'bg-neutral-100 text-neutral-600'
                        }`}
                      >
                        {invite.status}
                      </span>
                    </td>
                    <td className="px-3">
                      <div className="flex items-center gap-1">
                        <Button
                          size="xs"
                          variant="ghost"
                          disabled={resendDisabled}
                          onClick={() =>
                            resendInvite.mutate(
                              { email: invite.email },
                              {
                                onSuccess: () => toast.success(`Invite resent to ${invite.email}`),
                                onError: () => toast.error('Failed to resend invite'),
                              },
                            )
                          }
                        >
                          Resend
                        </Button>
                        <button
                          type="button"
                          className="flex h-7 w-7 items-center justify-center rounded text-red-600 hover:bg-red-50"
                          onClick={() =>
                            cancelInvite.mutate(invite.id, {
                              onSuccess: () => toast.success('Invite cancelled'),
                              onError: () => toast.error('Failed to cancel invite'),
                            })
                          }
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : null}

      <Modal open={inviteOpen} onOpenChange={setInviteOpen} title="Invite member" size="sm">
        <form onSubmit={onInviteSubmit} className="space-y-3">
          <Input label="Email" type="email" error={inviteForm.formState.errors.email?.message} {...inviteForm.register('email')} />

          <div>
            <p className="mb-1 text-12 font-medium text-neutral-700">Role</p>
            <label className="flex items-start gap-2 rounded border border-neutral-200 px-3 py-2">
              <input type="radio" value="Member" {...inviteForm.register('role')} checked={inviteForm.watch('role') === 'Member'} />
              <span>
                <span className="block text-13 font-medium text-neutral-800">Member</span>
                <span className="text-12 text-neutral-500">Can collaborate on tasks and projects.</span>
              </span>
            </label>
            <label className="mt-2 flex items-start gap-2 rounded border border-neutral-200 px-3 py-2">
              <input type="radio" value="Admin" {...inviteForm.register('role')} checked={inviteForm.watch('role') === 'Admin'} />
              <span>
                <span className="block text-13 font-medium text-neutral-800">Admin</span>
                <span className="text-12 text-neutral-500">Can manage members and workspace settings.</span>
              </span>
            </label>
          </div>

          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setInviteOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={inviteMember.isPending}>
              Send invite
            </Button>
          </div>
        </form>
      </Modal>

      <Modal open={Boolean(roleDialogMember)} onOpenChange={(open) => !open && setRoleDialogMember(null)} title="Change role" size="sm">
        <div className="space-y-3">
          <label className="flex items-center gap-2 rounded border border-neutral-200 px-3 py-2">
            <input type="radio" checked={nextRole === 'Admin'} onChange={() => setNextRole('Admin')} />
            <span className="text-13 text-neutral-700">Admin</span>
          </label>
          <label className="flex items-center gap-2 rounded border border-neutral-200 px-3 py-2">
            <input type="radio" checked={nextRole === 'Member'} onChange={() => setNextRole('Member')} />
            <span className="text-13 text-neutral-700">Member</span>
          </label>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setRoleDialogMember(null)}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                if (!roleDialogMember) return;
                updateMemberRole.mutate(
                  { memberId: roleDialogMember.id, payload: { role: nextRole } },
                  {
                    onSuccess: () => {
                      toast.success('Role updated');
                      setRoleDialogMember(null);
                    },
                    onError: () => toast.error('Failed to update role'),
                  },
                );
              }}
              loading={updateMemberRole.isPending}
            >
              Confirm
            </Button>
          </div>
        </div>
      </Modal>

      <Modal open={Boolean(removeDialogMember)} onOpenChange={(open) => !open && setRemoveDialogMember(null)} title="Remove member" size="sm">
        <div className="space-y-3">
          <p className="text-13 text-neutral-600">
            Remove {removeDialogMember?.displayName ?? removeDialogMember?.userName} from {workspace?.name}? Their assigned tasks will become unassigned.
          </p>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setRemoveDialogMember(null)}>
              Cancel
            </Button>
            <Button
              variant="danger"
              loading={removeMember.isPending}
              onClick={() => {
                if (!removeDialogMember) return;
                removeMember.mutate(removeDialogMember.id, {
                  onSuccess: () => {
                    toast.success('Member removed');
                    setRemoveDialogMember(null);
                  },
                  onError: () => toast.error('Failed to remove member'),
                });
              }}
            >
              Remove
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
