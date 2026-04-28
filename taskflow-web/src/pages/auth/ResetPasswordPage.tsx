import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, XCircle } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { z } from 'zod';
import AuthLayout from '../../layouts/AuthLayout';
import { PasswordStrengthBar } from '../../components/auth/PasswordStrengthBar';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useResetPassword } from '../../hooks/api/auth.hooks';
import { getFieldErrors } from '../../lib/api';

const resetSchema = z
  .object({
    newPassword: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Z]/, 'One uppercase letter')
      .regex(/[0-9]/, 'One number')
      .regex(/[^a-zA-Z0-9]/, 'One special character'),
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords do not match',
  });

type ResetValues = z.infer<typeof resetSchema>;
type ResetStatus = 'form' | 'success' | 'invalid';

export default function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const resetMutation = useResetPassword();
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [status, setStatus] = useState<ResetStatus>(token ? 'form' : 'invalid');
  const [bannerError, setBannerError] = useState('');

  const {
    register,
    handleSubmit,
    setError,
    watch,
    formState: { errors },
  } = useForm<ResetValues>({
    resolver: zodResolver(resetSchema),
  });

  const newPassword = watch('newPassword', '');
  const isInvalidToken = useMemo(() => !token || status === 'invalid', [status, token]);

  const onSubmit = handleSubmit(async (values) => {
    if (!token) return;

    try {
      setBannerError('');
      await resetMutation.mutateAsync({
        token,
        newPassword: values.newPassword,
        confirmPassword: values.confirmPassword,
      });
      setStatus('success');
      window.setTimeout(() => navigate('/login'), 2000);
    } catch (error) {
      const statusCode = (error as { response?: { status?: number; data?: { detail?: string } } })?.response?.status;
      const detail = (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail;

      if (statusCode === 400 && detail) {
        setStatus('invalid');
        setBannerError('This reset link is invalid or has expired.');
        return;
      }

      const fieldErrors = getFieldErrors(error);
      if (Object.keys(fieldErrors).length > 0) {
        Object.entries(fieldErrors).forEach(([field, message]) => {
          setError(field as keyof ResetValues, { message });
        });
      }
    }
  });

  if (isInvalidToken) {
    return (
      <AuthLayout title="Invalid reset link">
        <div className="flex flex-col items-center justify-center gap-3 py-6 text-center">
          <XCircle className="h-12 w-12 text-red-500" />
          <p className="text-14 text-neutral-700">This reset link is invalid or has expired.</p>
          <Button type="button" variant="secondary" onClick={() => navigate('/forgot-password')}>
            Request a new reset link
          </Button>
        </div>
      </AuthLayout>
    );
  }

  if (status === 'success') {
    return (
      <AuthLayout title="Password updated">
        <p className="py-6 text-center text-14 text-green-700">Password updated! Redirecting to login...</p>
      </AuthLayout>
    );
  }

  return (
    <AuthLayout title="Reset password" subtitle="Choose a new secure password">
      <form onSubmit={onSubmit} className="space-y-4">
        {bannerError ? <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-12 text-red-700">{bannerError}</p> : null}

        <div>
          <label htmlFor="reset-password" className="mb-1 block text-12 font-medium text-neutral-700">
            New password
          </label>
          <div className="relative">
            <Input
              id="reset-password"
              type={showNewPassword ? 'text' : 'password'}
              className="pr-9"
              error={errors.newPassword?.message}
              {...register('newPassword')}
            />
            <button
              type="button"
              onClick={() => setShowNewPassword((value) => !value)}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-600"
              aria-label={showNewPassword ? 'Hide password' : 'Show password'}
            >
              {showNewPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
          <PasswordStrengthBar password={newPassword} />
        </div>

        <div>
          <label htmlFor="reset-confirm-password" className="mb-1 block text-12 font-medium text-neutral-700">
            Confirm password
          </label>
          <div className="relative">
            <Input
              id="reset-confirm-password"
              type={showConfirmPassword ? 'text' : 'password'}
              className="pr-9"
              error={errors.confirmPassword?.message}
              {...register('confirmPassword')}
            />
            <button
              type="button"
              onClick={() => setShowConfirmPassword((value) => !value)}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-600"
              aria-label={showConfirmPassword ? 'Hide password' : 'Show password'}
            >
              {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
        </div>

        <Button type="submit" size="lg" className="w-full" loading={resetMutation.isPending}>
          Update password
        </Button>
      </form>
    </AuthLayout>
  );
}
