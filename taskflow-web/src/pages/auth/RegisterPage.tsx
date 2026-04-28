import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Mail } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { PasswordStrengthBar } from '../../components/auth/PasswordStrengthBar';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useRegister } from '../../hooks/api/auth.hooks';
import { getFieldErrors } from '../../lib/api';
import AuthLayout from '../../layouts/AuthLayout';

const registerSchema = z
  .object({
    organizationName: z.string().min(2, 'Organization name is too short').max(100),
    email: z.string().email('Enter a valid email'),
    userName: z
      .string()
      .min(3, 'Username must be at least 3 characters')
      .max(30)
      .regex(/^[a-zA-Z0-9_]+$/, 'Letters, numbers, _ only'),
    password: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .regex(/[A-Z]/, 'One uppercase letter')
      .regex(/[0-9]/, 'One number')
      .regex(/[^a-zA-Z0-9]/, 'One special character'),
    confirmPassword: z.string(),
  })
  .refine((data) => data.password === data.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Passwords do not match',
  });

type RegisterFormValues = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const navigate = useNavigate();
  const registerMutation = useRegister();
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
  });

  const password = watch('password', '');

  const onSubmit = handleSubmit(async (values) => {
    try {
      await registerMutation.mutateAsync(values);
      navigate('/verify-email-pending', { state: { email: values.email } });
    } catch (error) {
      const status = (error as { response?: { status?: number } })?.response?.status;
      if (status === 400) {
        const fieldErrors = getFieldErrors(error);
        Object.entries(fieldErrors).forEach(([field, message]) => {
          setError(field as keyof RegisterFormValues, { message });
        });
      }
    }
  });

  return (
    <AuthLayout title="Create your account" subtitle="Set up your workspace in a few steps">
      <form onSubmit={onSubmit} className="space-y-4">
        <Input label="Organization name" autoComplete="organization" error={errors.organizationName?.message} {...register('organizationName')} />

        <Input
          label="Email"
          type="email"
          autoComplete="email"
          leftIcon={<Mail className="h-3.5 w-3.5" />}
          error={errors.email?.message}
          {...register('email')}
        />

        <div>
          <label htmlFor="register-username" className="mb-1 block text-12 font-medium text-neutral-700">
            Username
          </label>
          <div className="relative">
            <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-13 text-neutral-400">@</span>
            <Input id="register-username" className="pl-7" error={errors.userName?.message} {...register('userName')} />
          </div>
        </div>

        <div>
          <label htmlFor="register-password" className="mb-1 block text-12 font-medium text-neutral-700">
            Password
          </label>
          <div className="relative">
            <Input
              id="register-password"
              type={showPassword ? 'text' : 'password'}
              autoComplete="new-password"
              className="pr-9"
              error={errors.password?.message}
              {...register('password')}
            />
            <button
              type="button"
              onClick={() => setShowPassword((value) => !value)}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-600"
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </button>
          </div>
          <PasswordStrengthBar password={password} />
        </div>

        <div>
          <label htmlFor="register-confirm-password" className="mb-1 block text-12 font-medium text-neutral-700">
            Confirm password
          </label>
          <div className="relative">
            <Input
              id="register-confirm-password"
              type={showConfirmPassword ? 'text' : 'password'}
              autoComplete="new-password"
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

        <Button type="submit" size="lg" variant="primary" className="w-full" loading={registerMutation.isPending}>
          Create account
        </Button>
      </form>

      <p className="mt-5 text-center text-13 text-neutral-500">
        Already have an account?{' '}
        <Link to="/login" className="font-medium">
          Sign in -&gt;
        </Link>
      </p>
    </AuthLayout>
  );
}
