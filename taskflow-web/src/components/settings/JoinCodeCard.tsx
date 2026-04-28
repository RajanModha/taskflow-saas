import { Copy, RotateCw } from 'lucide-react';
import toast from 'react-hot-toast';
import { Button } from '../ui/Button';

interface JoinCodeCardProps {
  joinCode: string | null | undefined;
  isOwner: boolean;
  onRegenerate?: () => void;
  regenerating?: boolean;
  className?: string;
}

export function JoinCodeCard({ joinCode, isOwner, onRegenerate, regenerating = false, className }: JoinCodeCardProps) {
  return (
    <div className={`rounded-md border border-primary-100 bg-primary-50 p-4 ${className ?? ''}`}>
      <div className="flex flex-wrap items-center gap-3">
        <div>
          <p className="text-12 text-primary-700">Join code</p>
          <p className="font-mono text-16 font-bold text-primary-700">{joinCode ?? '—'}</p>
        </div>
        <div className="ml-auto flex items-center gap-2">
          <Button
            size="sm"
            variant="secondary"
            leftIcon={<Copy className="h-3.5 w-3.5" />}
            onClick={async () => {
              if (!joinCode) return;
              await navigator.clipboard.writeText(joinCode);
              toast.success('Copied!');
            }}
          >
            Copy
          </Button>
          {isOwner ? (
            <Button
              size="sm"
              variant="ghost"
              leftIcon={<RotateCw className="h-3.5 w-3.5" />}
              onClick={onRegenerate}
              loading={regenerating}
            >
              Regenerate
            </Button>
          ) : null}
        </div>
      </div>
    </div>
  );
}
