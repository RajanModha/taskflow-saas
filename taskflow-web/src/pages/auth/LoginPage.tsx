import { zodResolver } from '@hookform/resolvers/zod';
import { Eye, EyeOff, Mail } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { queryClient } from '../../lib/queryClient';
import api from '../../lib/api';
import AuthLayout from '../../layouts/AuthLayout';
import { useLogin } from '../../hooks/api/auth.hooks';
import { useAuthStore } from '../../stores/authStore';
import type { UserProfileResponse } from '../../types/api';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';

const loginSchema = z.object({
  email: z.string().email('Enter a valid email'),
  password: z.string().min(1, 'Password is required'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

export default function LoginPage() {
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = useState(false);
  const loginMutation = useLogin();

  const {
    register,
    handleSubmit,
    formState: { errors },
    setError,
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      const data = await loginMutation.mutateAsync(values);
      useAuthStore.getState().setAuth(data);

      await queryClient.prefetchQuery({
        queryKey: ['me'],
        queryFn: async () => {
          const response = await api.get<UserProfileResponse>('/Auth/me');
          return response.data;
        },
      });

      const user = queryClient.getQueryData<UserProfileResponse>(['me']);
      if (!user) {
        return;
      }

      useAuthStore.getState().setUser(user);
      navigate(user.organizationId ? '/dashboard' : '/workspace/create');
    } catch (error) {
      const status = (error as { response?: { status?: number } })?.response?.status;
      if (status === 401) {
        setError('root', { message: 'Invalid email or password.' });
        return;
      }
      if (status === 403) {
        toast.error('Please verify your email first.');
        navigate('/verify-email-pending', { state: { email: values.email } });
      }
    }
  });

  return (
    <AuthLayout title="Welcome back" subtitle="Sign in to continue with TaskFlow">
      <form onSubmit={onSubmit} className="space-y-4">
        <Input
          label="Email"
          type="email"
          autoComplete="email"
          leftIcon={<Mail className="h-3.5 w-3.5" />}
          error={errors.email?.message}
          {...register('email')}
        />

        <div>
          <div className="mb-1 flex items-center justify-between">
            <label className="text-12 font-medium text-neutral-700" htmlFor="login-password">
              Password
            </label>
            <Link to="/forgot-password" className="text-12">
              Forgot password?
            </Link>
          </div>

          <div className="relative">
            <Input
              id="login-password"
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
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
        </div>

        {errors.root?.message ? <p className="text-12 text-red-600">{errors.root.message}</p> : null}

        <Button type="submit" size="lg" variant="primary" className="w-full" loading={loginMutation.isPending}>
          Sign in
        </Button>
      </form>

      <p className="mt-5 text-center text-13 text-neutral-500">
        Don&apos;t have an account?{' '}
        <Link to="/register" className="font-medium">
          Sign up -&gt;
        </Link>
      </p>
    </AuthLayout>
  );
}
