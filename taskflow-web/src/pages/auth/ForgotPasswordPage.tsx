import { zodResolver } from '@hookform/resolvers/zod';
import { AnimatePresence, motion } from 'framer-motion';
import { Mail } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import AuthLayout from '../../layouts/AuthLayout';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useForgotPassword } from '../../hooks/api/auth.hooks';
import { z } from 'zod';

const schema = z.object({
  email: z.string().email('Enter a valid email'),
});

type FormState = 'form' | 'sent';
type ForgotPasswordValues = z.infer<typeof schema>;

export default function ForgotPasswordPage() {
  const [view, setView] = useState<FormState>('form');
  const forgotPasswordMutation = useForgotPassword();

  const {
    register,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<ForgotPasswordValues>({
    resolver: zodResolver(schema),
  });

  const email = watch('email', '');

  const sendLink = handleSubmit(async (values) => {
    try {
      await forgotPasswordMutation.mutateAsync(values);
    } finally {
      setView('sent');
    }
  });

  const resend = async () => {
    if (!email) return;
    await forgotPasswordMutation.mutateAsync({ email });
  };

  return (
    <AuthLayout title="Forgot password" subtitle="We will send you a secure reset link">
      <AnimatePresence mode="wait">
        {view === 'form' ? (
          <motion.form key="form" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} onSubmit={sendLink} className="space-y-4">
            <Input
              label="Email"
              type="email"
              autoComplete="email"
              leftIcon={<Mail className="h-3.5 w-3.5" />}
              error={errors.email?.message}
              {...register('email')}
            />
            <Button type="submit" size="lg" className="w-full" loading={forgotPasswordMutation.isPending}>
              Send reset link
            </Button>
          </motion.form>
        ) : (
          <motion.div key="sent" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} className="text-center">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary-50 text-primary-600">
              <Mail className="h-6 w-6" />
            </div>
            <h2 className="text-18 font-semibold text-neutral-800">Check your inbox</h2>
            <p className="mt-2 text-13 text-neutral-500">If that email is registered, you&apos;ll receive a link shortly.</p>
            <div className="mt-5 flex flex-col gap-2">
              <Button type="button" variant="secondary" onClick={resend} loading={forgotPasswordMutation.isPending}>
                Didn&apos;t get it? Resend
              </Button>
              <Link to="/login" className="text-13 font-medium">
                Back to login
              </Link>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </AuthLayout>
  );
}
