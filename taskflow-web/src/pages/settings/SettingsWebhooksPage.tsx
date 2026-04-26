import { Bell } from 'lucide-react';
import { EmptyState } from '../../components/ui/EmptyState';

export default function SettingsWebhooksPage() {
  return (
    <div>
      <h2 className="mb-1 text-16 font-semibold text-neutral-800">Webhooks</h2>
      <p className="mb-5 text-13 text-neutral-500">Configure outgoing events for integrations.</p>
      <div className="rounded-md border border-neutral-200 bg-white">
        <EmptyState
          title="No webhooks configured"
          description="Create your first webhook endpoint."
          icon={Bell}
          size="sm"
          className="py-10"
        />
      </div>
    </div>
  );
}
