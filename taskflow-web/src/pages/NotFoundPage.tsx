import { Link } from 'react-router-dom';
import { Button } from '../components/ui/Button';

export function NotFoundContent() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-surface-page">
      <div className="text-center">
        <p className="select-none text-[96px] font-bold leading-none text-neutral-100">404</p>
        <h1 className="-mt-4 mb-2 text-24 font-semibold text-neutral-800">Page not found</h1>
        <p className="mb-6 text-13 text-neutral-500">
          The page you're looking for doesn't exist or has been moved.
        </p>
        <Link to="/dashboard">
          <Button variant="primary" size="md">
            Back to dashboard
          </Button>
        </Link>
      </div>
    </div>
  );
}

export function NotFoundPage() {
  return <NotFoundContent />;
}
