import { Mail } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useLocation } from 'react-router-dom';
import toast from 'react-hot-toast';
import AuthLayout from '../../layouts/AuthLayout';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useResendVerification } from '../../hooks/api/auth.hooks';

type ResendStatus = 'idle' | 'sending' | 'sent' | 'error';

interface LocationState {
  email?: string;
}

export default function VerifyEmailPendingPage() {
  const location = useLocation();
  const locationState = (location.state as LocationState | null) ?? null;
  const resendMutation = useResendVerification();
  const [email, setEmail] = useState(locationState?.email ?? '');
  const [status, setStatus] = useState<ResendStatus>('idle');
  const [cooldown, setCooldown] = useState(0);

  useEffect(() => {
    if (cooldown <= 0) return;
    const timer = window.setInterval(() => {
      setCooldown((value) => Math.max(0, value - 1));
    }, 1000);
    return () => window.clearInterval(timer);
  }, [cooldown]);

  const resendLabel = useMemo(() => {
    if (cooldown > 0) return `Resend in ${cooldown}s`;
    return 'Resend verification email';
  }, [cooldown]);

  const handleResend = async () => {
    if (!email || cooldown > 0) return;
    try {
      setStatus('sending');
      await resendMutation.mutateAsync({ email });
      setStatus('sent');
      setCooldown(60);
      toast.success('Verification email resent.');
    } catch {
      setStatus('error');
      setCooldown(60);
      toast.success('Verification email resent.');
    }
  };

  return (
    <AuthLayout title="Check your inbox">
      <div className="text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-primary-50 text-primary-600">
          <Mail className="h-6 w-6" />
        </div>
        <h2 className="text-18 font-semibold text-neutral-800">Check your inbox</h2>
        <p className="mt-2 text-13 text-neutral-500">
          We sent a verification link to <span className="font-medium text-neutral-700">{email || 'your email'}</span>.
        </p>
      </div>

      <div className="mt-5 space-y-3">
        <Input label="Email" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
        <Button
          type="button"
          variant="secondary"
          size="sm"
          className="w-full"
          disabled={!email || cooldown > 0}
          loading={status === 'sending'}
          onClick={handleResend}
        >
          {resendLabel}
        </Button>
      </div>
    </AuthLayout>
  );
}
