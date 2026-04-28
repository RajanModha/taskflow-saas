import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Spinner } from '../../components/ui/Spinner';
import { useAcceptInvite, useJoinWorkspace } from '../../hooks/api/workspace.hooks';

const schema = z.object({
  code: z.string().min(1, 'Invite code is required'),
});

type FormValues = z.infer<typeof schema>;

export default function JoinWorkspacePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const acceptInvite = useAcceptInvite();
  const joinWorkspace = useJoinWorkspace();
  const [tokenError, setTokenError] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  useEffect(() => {
    if (!token) return;

    acceptInvite
      .mutateAsync({ token })
      .then(() => {
        toast.success('Joined workspace!');
        navigate('/dashboard', { replace: true });
      })
      .catch(() => {
        setTokenError(true);
      });
  }, [acceptInvite, navigate, token]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      await joinWorkspace.mutateAsync({ code: values.code });
      navigate('/dashboard', { replace: true });
    } catch {
      toast.error('Unable to join workspace. Please verify the code.');
    }
  });

  if (token && acceptInvite.isPending) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page">
        <div className="flex items-center gap-2 rounded border border-neutral-200 bg-white px-4 py-3 text-13 text-neutral-600 shadow-e100">
          <Spinner size="sm" />
          Joining workspace...
        </div>
      </div>
    );
  }

  if (token && tokenError) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface-page px-4">
        <div className="w-full max-w-md rounded-md border border-red-200 bg-white p-6 text-center shadow-e100">
          <AlertCircle className="mx-auto h-8 w-8 text-red-500" />
          <h1 className="mt-3 text-18 font-semibold text-neutral-800">Invite link invalid</h1>
          <p className="mt-1 text-13 text-neutral-600">This invite link has expired or is invalid.</p>
          <Link className="mt-4 inline-block text-13 font-medium" to="/login">
            Back to login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-page px-4">
      <div className="w-full max-w-md rounded-md border border-neutral-200 bg-white p-6 shadow-e100">
        <h1 className="text-20 font-semibold text-neutral-800">Join workspace</h1>
        <p className="mt-1 text-13 text-neutral-500">Enter your invite code to join your team workspace.</p>
        <form onSubmit={onSubmit} className="mt-5 space-y-4">
          <Input label="Enter invite code" error={errors.code?.message} {...register('code')} />
          <Button type="submit" className="w-full" size="lg" loading={joinWorkspace.isPending}>
            Join workspace
          </Button>
        </form>
      </div>
    </div>
  );
}
