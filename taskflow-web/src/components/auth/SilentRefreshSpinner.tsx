import { Spinner } from '../ui/Spinner';

export default function SilentRefreshSpinner() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-surface-page">
      <div className="flex items-center gap-2 rounded border border-neutral-200 bg-white px-4 py-3 text-13 text-neutral-600 shadow-e100">
        <Spinner size="sm" />
        Restoring your session...
      </div>
    </div>
  );
}
