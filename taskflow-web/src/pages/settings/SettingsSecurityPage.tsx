import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';

export default function SettingsSecurityPage() {
  return (
    <div className="max-w-[480px]">
      <h2 className="mb-1 text-16 font-semibold text-neutral-800">Security</h2>
      <p className="mb-5 text-13 text-neutral-500">Manage password and account security.</p>

      <div className="rounded-md border border-neutral-200 bg-white">
        <div className="space-y-4 p-4">
          <Input label="Current password" type="password" />
          <Input label="New password" type="password" />
          <Input label="Confirm password" type="password" />
        </div>
        <div className="flex justify-end gap-2 border-t border-neutral-100 px-4 py-3">
          <Button size="sm" variant="secondary">Cancel</Button>
          <Button size="sm" variant="primary">Save changes</Button>
        </div>
      </div>
    </div>
  );
}
