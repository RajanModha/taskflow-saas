import { EmptyState } from '../../components/ui/EmptyState';

export default function SettingsTemplatesPage() {
  return (
    <div>
      <h2 className="mb-1 text-16 font-semibold text-neutral-800">Templates</h2>
      <p className="mb-5 text-13 text-neutral-500">Manage reusable project and task templates.</p>
      <div className="rounded-md border border-neutral-200 bg-white">
        <EmptyState.NoProjects size="sm" className="py-10" action={{ label: 'Create template', onClick: () => undefined }} />
      </div>
    </div>
  );
}
