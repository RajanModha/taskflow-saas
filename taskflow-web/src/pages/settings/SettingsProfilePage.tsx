import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';

export default function SettingsProfilePage() {
  return (
    <div className="max-w-[480px]">
      <h2 className="mb-1 text-16 font-semibold text-neutral-800">Profile Information</h2>
      <p className="mb-5 text-13 text-neutral-500">Update your personal details.</p>

      <div className="rounded-md border border-neutral-200 bg-white">
        <div className="space-y-4 p-4">
          <Input label="Full name" defaultValue="Alex Johnson" />
          <Input label="Email" defaultValue="alex@taskflow.dev" type="email" />
          <Input label="Job title" defaultValue="Product Engineer" />
        </div>
        <div className="flex justify-end gap-2 border-t border-neutral-100 px-4 py-3">
          <Button size="sm" variant="secondary">Cancel</Button>
          <Button size="sm" variant="primary">Save changes</Button>
        </div>
      </div>
    </div>
  );
}
