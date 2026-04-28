import { Check } from 'lucide-react';
import type { ReactNode } from 'react';

interface AuthLayoutProps {
  title: string;
  subtitle?: string;
  children: ReactNode;
}

const trustAvatars = ['AL', 'PR', 'MN', 'SJ'];

export default function AuthLayout({ title, subtitle, children }: AuthLayoutProps) {
  return (
    <div className="flex min-h-screen">
      <aside className="hidden w-2/5 flex-col justify-between bg-gradient-to-br from-primary-700 to-primary-950 p-10 text-white lg:flex">
        <div>
          <div className="text-26 font-semibold tracking-tight">TaskFlow</div>
          <p className="mt-3 max-w-sm text-15 text-primary-100">Plan smarter, execute faster, and deliver as one team.</p>

          <ul className="mt-10 space-y-4">
            {['Unified workspace for projects and tasks', 'Real-time visibility for every stakeholder', 'Secure collaboration built for modern teams'].map(
              (point) => (
                <li key={point} className="flex items-start gap-3 text-14 text-primary-50">
                  <span className="mt-0.5 inline-flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-white/20">
                    <Check className="h-3.5 w-3.5" />
                  </span>
                  <span>{point}</span>
                </li>
              ),
            )}
          </ul>
        </div>

        <div>
          <div className="flex items-center">
            {trustAvatars.map((avatar, index) => (
              <span
                key={avatar}
                className="inline-flex h-8 w-8 items-center justify-center rounded-full border-2 border-primary-900 bg-primary-300 text-11 font-semibold text-primary-950"
                style={{ marginLeft: index === 0 ? 0 : -8 }}
              >
                {avatar}
              </span>
            ))}
          </div>
          <p className="mt-3 text-13 text-primary-100">Trusted by modern teams</p>
        </div>
      </aside>

      <main className="w-full overflow-y-auto bg-white lg:w-3/5">
        <div className="flex min-h-screen items-center justify-center">
          <div className="mx-auto w-full max-w-[360px] px-6 py-12">
            <h1 className="text-24 font-semibold text-neutral-800">{title}</h1>
            {subtitle ? <p className="mt-1 text-13 text-neutral-500">{subtitle}</p> : null}
            <div className="mt-6">{children}</div>
          </div>
        </div>
      </main>
    </div>
  );
}
