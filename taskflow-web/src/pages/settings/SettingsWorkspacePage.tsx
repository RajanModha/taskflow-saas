import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { JoinCodeCard } from '../../components/settings/JoinCodeCard';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useRegenerateJoinCode, useUpdateWorkspace, useWorkspaceMe } from '../../hooks/api/workspace.hooks';
import type { MyWorkspaceResponse } from '../../types/api';

const schema = z.object({
  name: z.string().min(2, 'Too short').max(100, 'Too long'),
});

type FormValues = z.infer<typeof schema>;

export default function SettingsWorkspacePage() {
  const queryClient = useQueryClient();
  const { data: workspace } = useWorkspaceMe();
  const updateWorkspace = useUpdateWorkspace();
  const regenerateJoinCode = useRegenerateJoinCode();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '' },
  });

  useEffect(() => {
    if (!workspace) return;
    reset({ name: workspace.name });
  }, [reset, workspace]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      const response = await updateWorkspace.mutateAsync({ name: values.name });
      toast.success(response.message);
      queryClient.setQueryData<MyWorkspaceResponse | undefined>(['workspace', 'me'], (current) =>
        current ? { ...current, name: values.name } : current,
      );
      reset({ name: values.name });
    } catch {
      toast.error('Failed to update workspace');
    }
  });

  const isOwner = workspace?.currentUserRole === 'Owner';

  const onRegenerate = () => {
    const yes = window.confirm('Old code will stop working immediately.');
    if (!yes) return;
    regenerateJoinCode.mutate(undefined, {
      onSuccess: (data) => {
        queryClient.setQueryData<MyWorkspaceResponse | undefined>(['workspace', 'me'], (current) =>
          current ? { ...current, joinCode: data.joinCode } : current,
        );
        toast.success('Join code regenerated');
      },
    });
  };

  return (
    <div className="max-w-[480px]">
      <div className="rounded-md border border-neutral-200 bg-white">
        <form onSubmit={onSubmit}>
          <div className="space-y-4 p-4">
            <Input label="Workspace name" error={errors.name?.message} {...register('name')} />
          </div>
          <div className="flex justify-end gap-2 border-t border-neutral-100 px-4 py-3">
            <Button type="button" variant="secondary" disabled={!isDirty} onClick={() => reset({ name: workspace?.name ?? '' })}>
              Cancel
            </Button>
            <Button type="submit" loading={updateWorkspace.isPending}>
              Save
            </Button>
          </div>
        </form>
      </div>

      <JoinCodeCard
        joinCode={workspace?.joinCode}
        isOwner={Boolean(isOwner)}
        onRegenerate={onRegenerate}
        regenerating={regenerateJoinCode.isPending}
        className="mt-4"
      />
    </div>
  );
}
