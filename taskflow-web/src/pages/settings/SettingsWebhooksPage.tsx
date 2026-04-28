import { zodResolver } from '@hookform/resolvers/zod';
import { format } from 'date-fns';
import { Bell, Plus } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import toast from 'react-hot-toast';
import { z } from 'zod';
import { EmptyState } from '../../components/ui/EmptyState';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { Pagination } from '../../components/ui/Pagination';
import { useCreateWebhook, useDeleteWebhook, useTestWebhook, useUpdateWebhook, useWebhookDeliveries, useWebhooks } from '../../hooks/api/webhooks.hooks';
import { useWorkspaceMe } from '../../hooks/api/workspace.hooks';

const WEBHOOK_EVENTS = ['task.created', 'task.status_changed', 'task.assigned', 'task.deleted', 'project.created', 'project.deleted', 'member.joined'];

const schema = z.object({
  url: z.string().url('Enter valid URL').refine((v) => v.startsWith('https://'), 'URL must be https://'),
  secret: z.string().min(1, 'Secret is required'),
  events: z.array(z.string()).min(1, 'Select at least one event'),
});

type FormValues = z.infer<typeof schema>;

function statusClass(status: string) {
  if (status.toLowerCase().includes('success')) return 'bg-green-100 text-green-700';
  if (status.toLowerCase().includes('failed')) return 'bg-red-100 text-red-700';
  return 'bg-amber-100 text-amber-700';
}

export default function SettingsWebhooksPage() {
  const { data: webhooks = [] } = useWebhooks();
  const { data: workspace } = useWorkspaceMe();
  const createWebhook = useCreateWebhook();
  const updateWebhook = useUpdateWebhook();
  const deleteWebhook = useDeleteWebhook();
  const testWebhook = useTestWebhook();

  const [open, setOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [deliveriesWebhookId, setDeliveriesWebhookId] = useState<string | null>(null);
  const [deliveriesPage, setDeliveriesPage] = useState(1);

  const { data: deliveries } = useWebhookDeliveries(deliveriesWebhookId, { page: deliveriesPage, pageSize: 10 });

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { url: '', secret: '', events: [] },
  });

  const isAdmin = ['Owner', 'Admin'].includes(workspace?.currentUserRole ?? '');

  const onCreate = () => {
    setEditingId(null);
    form.reset({ url: '', secret: '', events: [] });
    setOpen(true);
  };

  const onEdit = (webhook: (typeof webhooks)[number]) => {
    setEditingId(webhook.id);
    form.reset({ url: webhook.url, secret: '', events: webhook.events });
    setOpen(true);
  };

  const onSubmit = form.handleSubmit(async (values) => {
    try {
      if (!editingId) {
        await createWebhook.mutateAsync(values);
        toast.success('Webhook created');
      } else {
        await updateWebhook.mutateAsync({ webhookId: editingId, payload: values });
        toast.success('Webhook updated');
      }
      setOpen(false);
    } catch {
      toast.error('Failed to save webhook');
    }
  });

  return (
    <div>
      <div className="page-header">
        <div>
          <h2 className="page-title">Webhooks</h2>
          <p className="text-12 text-neutral-500">Receive HTTP POST callbacks on workspace events.</p>
        </div>
        {isAdmin ? (
          <Button size="sm" leftIcon={<Plus className="h-3.5 w-3.5" />} onClick={onCreate}>
            Add webhook
          </Button>
        ) : null}
      </div>

      {webhooks.length === 0 ? (
        <EmptyState icon={Bell} title="No webhooks configured" description="Create your first webhook endpoint." action={isAdmin ? { label: 'Add webhook', onClick: onCreate } : undefined} />
      ) : (
        <div className="space-y-3">
          {webhooks.map((webhook) => (
            <div key={webhook.id} className="rounded-md border border-neutral-200 bg-white p-4">
              <div className="flex items-start gap-3">
                <p className="min-w-0 flex-1 truncate font-mono text-12 text-neutral-700">{webhook.url}</p>
                <button
                  type="button"
                  className={`relative inline-flex h-6 w-10 items-center rounded-full ${webhook.isActive ? 'bg-primary-600' : 'bg-neutral-300'}`}
                  onClick={() => updateWebhook.mutate({ webhookId: webhook.id, payload: { isActive: !webhook.isActive } })}
                >
                  <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition ${webhook.isActive ? 'translate-x-5' : 'translate-x-1'}`} />
                </button>
              </div>

              <div className="mt-2 flex flex-wrap gap-1">
                {webhook.events.map((event) => (
                  <span key={event} className="rounded-sm bg-primary-50 px-2 py-0.5 text-11 text-primary-700">
                    {event}
                  </span>
                ))}
              </div>

              <div className="mt-3 flex items-center justify-between">
                <p className="text-12 text-neutral-500">Created {format(new Date(webhook.createdAtUtc), 'MMM d, yyyy')}</p>
                <div className="flex items-center gap-1">
                  <Button size="xs" variant="ghost" onClick={() => onEdit(webhook)}>
                    Edit
                  </Button>
                  <Button
                    size="xs"
                    variant="ghost"
                    onClick={() =>
                      testWebhook.mutate(webhook.id, {
                        onSuccess: (res) => {
                          if (res.delivered) toast.success(`Webhook delivered (${res.responseStatus ?? 200})`);
                          else toast.error(`Delivery failed (${res.responseStatus ?? 'timeout'})`);
                        },
                      })
                    }
                  >
                    Test
                  </Button>
                  <Button size="xs" variant="ghost" onClick={() => setDeliveriesWebhookId(webhook.id)}>
                    Deliveries
                  </Button>
                  <Button
                    size="xs"
                    variant="danger-ghost"
                    onClick={() =>
                      deleteWebhook.mutate(webhook.id, {
                        onSuccess: () => toast.success('Webhook deleted'),
                      })
                    }
                  >
                    Delete
                  </Button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <Modal open={open} onOpenChange={setOpen} title={editingId ? 'Edit webhook' : 'Create webhook'} size="md">
        <form className="space-y-3" onSubmit={onSubmit}>
          <Input label="URL" placeholder="https://example.com/webhook" error={form.formState.errors.url?.message} {...form.register('url')} />
          <Input label="Secret" type="password" hint="Used for HMAC-SHA256 signature" error={form.formState.errors.secret?.message} {...form.register('secret')} />
          <div>
            <div className="mb-1 flex items-center justify-between">
              <p className="text-12 font-medium text-neutral-700">Events</p>
              <button
                type="button"
                className="text-12 text-primary-600"
                onClick={() => form.setValue('events', form.watch('events').length === WEBHOOK_EVENTS.length ? [] : WEBHOOK_EVENTS)}
              >
                Select all
              </button>
            </div>
            <div className="grid grid-cols-2 gap-2">
              {WEBHOOK_EVENTS.map((event) => {
                const selected = form.watch('events').includes(event);
                return (
                  <label key={event} className="flex items-center gap-2 rounded border border-neutral-200 px-2 py-1.5">
                    <input
                      type="checkbox"
                      checked={selected}
                      onChange={() =>
                        form.setValue('events', selected ? form.watch('events').filter((e) => e !== event) : [...form.watch('events'), event])
                      }
                    />
                    <span className="text-12 text-neutral-700">{event}</span>
                  </label>
                );
              })}
            </div>
            {form.formState.errors.events?.message ? <p className="mt-1 text-12 text-red-600">{form.formState.errors.events.message}</p> : null}
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="secondary" onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" loading={createWebhook.isPending || updateWebhook.isPending}>
              Save
            </Button>
          </div>
        </form>
      </Modal>

      <Modal open={Boolean(deliveriesWebhookId)} onOpenChange={(openState) => !openState && setDeliveriesWebhookId(null)} title="Webhook Deliveries" size="lg">
        <div className="overflow-hidden rounded-md border border-neutral-200">
          <table className="w-full border-collapse text-12">
            <thead>
              <tr className="border-b border-neutral-200 bg-neutral-50">
                <th className="h-8 px-3 text-left font-medium text-neutral-500">Event type</th>
                <th className="h-8 w-28 px-3 text-left font-medium text-neutral-500">Status</th>
                <th className="h-8 w-20 px-3 text-left font-medium text-neutral-500">Attempts</th>
                <th className="h-8 w-36 px-3 text-left font-medium text-neutral-500">Last attempt</th>
                <th className="h-8 w-24 px-3 text-left font-medium text-neutral-500">Response</th>
              </tr>
            </thead>
            <tbody>
              {(deliveries?.items ?? []).map((row) => (
                <tr key={row.id} className="border-b border-neutral-100">
                  <td className="px-3 py-1.5 text-neutral-700">{row.eventType}</td>
                  <td className="px-3 py-1.5">
                    <span className={`inline-flex rounded-full px-2 py-0.5 text-11 font-medium ${statusClass(row.status)}`}>{row.status}</span>
                  </td>
                  <td className="px-3 py-1.5 text-neutral-600">{row.attemptCount}</td>
                  <td className="px-3 py-1.5 text-neutral-500">{row.lastAttemptAt ? format(new Date(row.lastAttemptAt), 'MMM d, yyyy h:mm a') : '—'}</td>
                  <td className="px-3 py-1.5 text-neutral-600">{row.responseStatus ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <Pagination page={deliveries?.page ?? deliveriesPage} pageSize={deliveries?.pageSize ?? 10} totalCount={deliveries?.totalCount ?? 0} onPageChange={setDeliveriesPage} className="mt-3" />
      </Modal>
    </div>
  );
}
