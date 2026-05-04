import { AlertCircle } from 'lucide-react';
import { isRouteErrorResponse, Link, useRouteError } from 'react-router-dom';
import { NotFoundContent } from '../pages/NotFoundPage';
import { Button } from './ui/Button';

export function RouteErrorBoundary() {
  const error = useRouteError();

  if (isRouteErrorResponse(error) && error.status === 404) {
    return <NotFoundContent />;
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface-page">
      <div className="max-w-md px-6 text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-red-100">
          <AlertCircle className="h-6 w-6 text-red-600" />
        </div>
        <h1 className="mb-2 text-20 font-semibold text-neutral-800">Something went wrong</h1>
        <p className="mb-6 text-13 text-neutral-500">
          An unexpected error occurred. Please refresh the page or go back.
        </p>
        <div className="flex items-center justify-center gap-3">
          <Button variant="secondary" size="md" onClick={() => window.location.reload()}>
            Refresh page
          </Button>
          <Link to="/dashboard">
            <Button variant="primary" size="md">
              Go to dashboard
            </Button>
          </Link>
        </div>
        {import.meta.env.DEV && error instanceof Error ? (
          <pre className="mt-6 max-h-48 overflow-auto rounded bg-red-50 p-4 text-left text-11 text-red-600">
            {error.message}
            {'\n'}
            {error.stack}
          </pre>
        ) : null}
      </div>
    </div>
  );
}
