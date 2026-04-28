import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { Avatar } from '../../components/ui/Avatar';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useMe, useUpdateProfile } from '../../hooks/api/auth.hooks';
import { getFieldErrors } from '../../lib/api';

const schema = z.object({
  displayName: z.string().min(2, 'Display name must be at least 2 characters').max(50).optional().or(z.literal('')),
  userName: z
    .string()
    .min(3, 'Username must be at least 3 characters')
    .max(30, 'Username must be at most 30 characters')
    .regex(/^[a-zA-Z0-9_]+$/, 'Only letters, numbers, and underscore are allowed'),
});

type FormValues = z.infer<typeof schema>;

function roleLabel(role: string | null, roles: string[] | null) {
  return role ?? roles?.[0] ?? 'Member';
}

export default function ProfileSettingsPage() {
  const queryClient = useQueryClient();
  const { data: user } = useMe();
  const updateProfile = useUpdateProfile();

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      displayName: '',
      userName: '',
    },
  });

  useEffect(() => {
    if (!user) return;
    reset({
      displayName: user.displayName ?? '',
      userName: user.userName,
    });
  }, [reset, user]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      const data = await updateProfile.mutateAsync({
        displayName: values.displayName || undefined,
        userName: values.userName,
      });
      queryClient.setQueryData(['me'], data);
      toast.success('Profile updated');
      reset({
        displayName: data.displayName ?? '',
        userName: data.userName,
      });
    } catch (error) {
      const status = (error as { response?: { status?: number } })?.response?.status;
      if (status === 409) {
        setError('userName', { message: 'Username already taken' });
        return;
      }
      if (status === 400) {
        const fieldErrors = getFieldErrors(error);
        Object.entries(fieldErrors).forEach(([field, message]) => {
          if (field === 'displayName' || field === 'userName') {
            setError(field, { message });
          }
        });
      }
    }
  });

  return (
    <div className="max-w-[480px]">
      <h2 className="mb-4 text-16 font-semibold text-neutral-800">Profile Information</h2>

      <div className="rounded-md border border-neutral-200 bg-white">
        <div className="space-y-4 p-4">
          <div className="flex items-center gap-3">
            <Avatar name={user?.displayName ?? user?.userName ?? 'User'} size="xl" />
            <div className="min-w-0">
              <p className="truncate text-13 font-medium text-neutral-800">{user?.displayName ?? user?.userName}</p>
              <p className="truncate text-12 text-neutral-500">{user?.email}</p>
            </div>
          </div>

          <form id="profile-form" onSubmit={onSubmit} className="space-y-4">
            <Input label="Display name" error={errors.displayName?.message} {...register('displayName')} />
            <Input label="Username" error={errors.userName?.message} {...register('userName')} />
            <Input
              label="Email"
              value={user?.email ?? ''}
              readOnly
              hint="Contact support to change"
            />
            <div>
              <label className="mb-1 block text-12 font-medium text-neutral-700">Organization</label>
              <span className="inline-flex rounded-full bg-neutral-100 px-2 py-0.5 text-12 text-neutral-700">
                {user?.organizationName ?? '—'}
              </span>
            </div>
            <div>
              <label className="mb-1 block text-12 font-medium text-neutral-700">Role</label>
              <span className="inline-flex rounded-full bg-primary-50 px-2 py-0.5 text-12 font-medium text-primary-700">
                {roleLabel(user?.role ?? null, user?.roles ?? null)}
              </span>
            </div>
          </form>
        </div>
        <div className="flex justify-end gap-2 border-t border-neutral-100 px-4 py-3">
          <Button
            size="sm"
            variant="secondary"
            onClick={() =>
              reset({
                displayName: user?.displayName ?? '',
                userName: user?.userName ?? '',
              })
            }
            disabled={!isDirty}
          >
            Cancel
          </Button>
          <Button size="sm" variant="primary" type="submit" form="profile-form" loading={updateProfile.isPending}>
            Save changes
          </Button>
        </div>
      </div>
    </div>
  );
}
