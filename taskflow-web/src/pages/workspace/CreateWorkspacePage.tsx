import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useCreateWorkspace } from '../../hooks/api/workspace.hooks';

const schema = z.object({
  name: z.string().min(2, 'Too short').max(100),
});

type FormValues = z.infer<typeof schema>;

export default function CreateWorkspacePage() {
  const navigate = useNavigate();
  const createWorkspace = useCreateWorkspace();
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createWorkspace.mutateAsync(values);
      navigate('/dashboard');
    } catch {
      toast.error('Unable to create workspace. Please try again.');
    }
  });

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-page px-4">
      <div className="w-full max-w-md rounded-md border border-neutral-200 bg-white p-6 shadow-e100">
        <h1 className="text-20 font-semibold text-neutral-800">Create your workspace</h1>
        <p className="mt-1 text-13 text-neutral-500">Set up a workspace to start collaborating with your team.</p>
        <form onSubmit={onSubmit} className="mt-5 space-y-4">
          <Input label="Workspace Name" error={errors.name?.message} {...register('name')} />
          <Button type="submit" className="w-full" size="lg" loading={createWorkspace.isPending}>
            Create workspace
          </Button>
        </form>
      </div>
    </div>
  );
}
