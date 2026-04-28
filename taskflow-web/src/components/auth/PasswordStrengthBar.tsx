interface PasswordStrengthBarProps {
  password: string;
}

const checks = [
  (value: string) => value.length >= 8,
  (value: string) => /[A-Z]/.test(value),
  (value: string) => /[0-9]/.test(value),
  (value: string) => /[^a-zA-Z0-9]/.test(value),
];

function colorClass(score: number) {
  if (score >= 4) return 'bg-green-500';
  if (score === 3) return 'bg-lime-400';
  if (score === 2) return 'bg-amber-400';
  if (score === 1) return 'bg-red-400';
  return 'bg-neutral-200';
}

export function PasswordStrengthBar({ password }: PasswordStrengthBarProps) {
  const score = checks.reduce((total, check) => total + Number(check(password)), 0);

  return (
    <div className="mt-2">
      <div className="grid grid-cols-4 gap-1.5">
        {Array.from({ length: 4 }).map((_, index) => (
          <span
            key={index}
            className={`h-1.5 rounded-full transition-colors ${index < score ? colorClass(score) : 'bg-neutral-200'}`}
          />
        ))}
      </div>
    </div>
  );
}
