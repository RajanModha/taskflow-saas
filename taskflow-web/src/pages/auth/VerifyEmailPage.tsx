import { motion } from 'framer-motion';
import { CheckCircle2, Mail, XCircle } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import AuthLayout from '../../layouts/AuthLayout';
import api from '../../lib/api';
import { useAuthStore } from '../../stores/authStore';
import type { AuthResponse } from '../../types/api';
import { Button } from '../../components/ui/Button';
import { Spinner } from '../../components/ui/Spinner';

type VerifyState = 'loading' | 'success' | 'error' | 'invalid';

export default function VerifyEmailPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const [status, setStatus] = useState<VerifyState>('loading');

  useEffect(() => {
    if (!token) {
      setStatus('invalid');
      return;
    }

    api
      .post<AuthResponse>('/Auth/verify-email', { token })
      .then((response) => {
        useAuthStore.getState().setAuth(response.data);
        setStatus('success');
        window.setTimeout(() => navigate('/dashboard'), 2000);
      })
      .catch(() => setStatus('error'));
  }, [navigate, token]);

  if (status === 'loading') {
    return (
      <AuthLayout title="Verifying email">
        <div className="flex flex-col items-center justify-center gap-3 py-10 text-center">
          <Spinner size="md" />
          <p className="text-13 text-neutral-600">Verifying your email...</p>
        </div>
      </AuthLayout>
    );
  }

  if (status === 'success') {
    return (
      <AuthLayout title="Email verified">
        <div className="flex flex-col items-center justify-center gap-3 py-10 text-center">
          <motion.div initial={{ scale: 0 }} animate={{ scale: 1 }} transition={{ duration: 0.25 }}>
            <CheckCircle2 className="h-12 w-12 text-green-600" />
          </motion.div>
          <p className="text-14 text-neutral-700">Email verified! Redirecting...</p>
        </div>
      </AuthLayout>
    );
  }

  const invalidState = status === 'invalid' || status === 'error';
  if (invalidState) {
    return (
      <AuthLayout title="Verification failed">
        <div className="flex flex-col items-center justify-center gap-3 py-6 text-center">
          <XCircle className="h-12 w-12 text-red-500" />
          <p className="text-14 text-neutral-700">This link is invalid or has expired.</p>
          <Button type="button" variant="secondary" leftIcon={<Mail className="h-4 w-4" />} onClick={() => navigate('/verify-email-pending')}>
            Request new link
          </Button>
        </div>
      </AuthLayout>
    );
  }

  return null;
}
