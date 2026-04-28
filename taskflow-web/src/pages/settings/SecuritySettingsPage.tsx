import { zodResolver } from '@hookform/resolvers/zod';
import { format, formatDistanceToNow } from 'date-fns';
import { Eye, EyeOff, Laptop, Smartphone } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { PasswordStrengthBar } from '../../components/auth/PasswordStrengthBar';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useChangePassword, useLogoutAll, useRevokeSession, useSessions } from '../../hooks/api/auth.hooks';
import { getFieldErrors } from '../../lib/api';
import { useAuthStore } from '../../stores/authStore';

const schema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required'),
    newPassword: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Z]/, 'One uppercase letter')
      .regex(/[0-9]/, 'One number')
      .regex(/[^a-zA-Z0-9]/, 'One special character'),
    confirmPassword: z.string(),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords do not match',
  });

type FormValues = z.infer<typeof schema>;

function sessionDeviceIcon(deviceInfo: string | null) {
  const value = (deviceInfo ?? '').toLowerCase();
  return value.includes('mobile') || value.includes('android') || value.includes('iphone') ? Smartphone : Laptop;
}

export default function SecuritySettingsPage() {
  const refreshToken = useAuthStore((state) => state.refreshToken);
  const { data: sessions = [] } = useSessions(refreshToken ?? null);
  const changePassword = useChangePassword();
  const revokeSession = useRevokeSession();
  const logoutAll = useLogoutAll();

  const [showCurrent, setShowCurrent] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setError,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
  });

  const newPassword = watch('newPassword', '');

  const onSubmit = handleSubmit(async (values) => {
    if (!refreshToken) return;
    try {
      const data = await changePassword.mutateAsync({
        currentPassword: values.currentPassword,
        newPassword: values.newPassword,
        confirmPassword: values.confirmPassword,
        refreshToken,
      });
      toast.success(data.message);
      reset();
    } catch (error) {
      const status = (error as { response?: { status?: number } })?.response?.status;
      if (status === 401) {
        setError('currentPassword', { message: 'Current password is incorrect' });
        return;
      }
      if (status === 400) {
        const fieldErrors = getFieldErrors(error);
        Object.entries(fieldErrors).forEach(([field, message]) => {
          if (field === 'currentPassword' || field === 'newPassword' || field === 'confirmPassword') {
            setError(field, { message });
          }
        });
      }
    }
  });

  return (
    <div>
      <div className="max-w-[480px] rounded-md border border-neutral-200 bg-white">
        <div className="border-b border-neutral-100 px-4 py-3">
          <h2 className="text-16 font-semibold text-neutral-800">Change Password</h2>
        </div>

        <form className="space-y-4 p-4" onSubmit={onSubmit}>
          <div className="relative">
            <Input type={showCurrent ? 'text' : 'password'} label="Current password" error={errors.currentPassword?.message} className="pr-9" {...register('currentPassword')} />
            <button type="button" className="absolute right-2 top-8 text-neutral-400" onClick={() => setShowCurrent((value) => !value)}>
              {showCurrent ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>

          <div className="relative">
            <Input type={showNew ? 'text' : 'password'} label="New password" error={errors.newPassword?.message} className="pr-9" {...register('newPassword')} />
            <button type="button" className="absolute right-2 top-8 text-neutral-400" onClick={() => setShowNew((value) => !value)}>
              {showNew ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
            <PasswordStrengthBar password={newPassword} />
          </div>

          <div className="relative">
            <Input type={showConfirm ? 'text' : 'password'} label="Confirm password" error={errors.confirmPassword?.message} className="pr-9" {...register('confirmPassword')} />
            <button type="button" className="absolute right-2 top-8 text-neutral-400" onClick={() => setShowConfirm((value) => !value)}>
              {showConfirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>

          <div className="flex justify-end">
            <Button type="submit" size="sm" loading={changePassword.isPending}>
              Save changes
            </Button>
          </div>
        </form>
      </div>

      <div className="mt-6 max-w-[640px] rounded-md border border-neutral-200 bg-white">
        <div className="flex items-center justify-between border-b border-neutral-100 px-4 py-3">
          <h2 className="text-16 font-semibold text-neutral-800">Active Sessions</h2>
          <Button
            size="sm"
            variant="danger-ghost"
            loading={logoutAll.isPending}
            onClick={() =>
              logoutAll.mutate(undefined, {
                onSuccess: () => toast.success('All other sessions revoked.'),
              })
            }
          >
            Revoke all other sessions
          </Button>
        </div>
        <div>
          {sessions.map((session) => {
            const DeviceIcon = sessionDeviceIcon(session.deviceInfo);
            return (
              <div key={session.id} className="flex items-center gap-3 border-b border-neutral-100 p-3 last:border-b-0">
                <DeviceIcon className="h-4 w-4 text-neutral-500" />
                <div className="min-w-0 flex-1">
                  <p className="truncate text-13 font-medium text-neutral-700">{session.deviceInfo ?? 'Unknown device'}</p>
                  <p className="truncate text-12 text-neutral-400">{session.ipAddress ?? 'Unknown IP'}</p>
                </div>
                <div className="text-right">
                  <p className="text-12 text-neutral-500">Started {format(new Date(session.createdAt), 'MMM d h:mm a')}</p>
                  <p className="text-12 text-neutral-400">Expires in {formatDistanceToNow(new Date(session.expiresAt))}</p>
                </div>
                {session.isCurrent ? (
                  <span className="rounded bg-green-100 px-2 py-0.5 text-11 font-medium text-green-700">(Current)</span>
                ) : (
                  <Button
                    size="sm"
                    variant="danger-ghost"
                    onClick={() =>
                      revokeSession.mutate(session.id, {
                        onSuccess: () => toast.success('Session revoked'),
                      })
                    }
                  >
                    Revoke
                  </Button>
                )}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}
