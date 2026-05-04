import { NavLink, Outlet } from 'react-router-dom';
import { useMe } from '../hooks/api/auth.hooks';
import { cn } from '../lib/utils';

function NavItem({ to, label }: { to: string; label: string }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        cn(
          'flex h-8 items-center rounded px-3 text-13',
          isActive ? 'bg-primary-50 font-medium text-primary-700' : 'text-neutral-600 hover:bg-neutral-50',
        )
      }
      end
    >
      {label}
    </NavLink>
  );
}

export default function SettingsLayout() {
  const { data: me } = useMe();
  const isAdmin = ['Owner', 'Admin'].includes(me?.role ?? '');

  return (
    <div className="flex h-full">
      <nav className="w-[180px] flex-shrink-0 border-r border-neutral-200 bg-white p-4">
        <p className="mb-4 text-16 font-semibold text-neutral-800">Settings</p>

        <p className="mb-1 text-11 font-semibold uppercase tracking-wider text-neutral-500">Account</p>
        <NavItem to="/settings/profile" label="Profile" />
        <NavItem to="/settings/security" label="Security" />

        <p className="mb-1 mt-4 text-11 font-semibold uppercase tracking-wider text-neutral-500">Workspace</p>
        <NavItem to="/settings/workspace" label="General" />
        <NavItem to="/settings/tags" label="Tags" />
        <NavItem to="/settings/webhooks" label="Webhooks" />
        <NavItem to="/settings/templates" label="Templates" />
        {isAdmin ? <NavItem to="/trash" label="Trash" /> : null}
      </nav>

      <div className="flex-1 overflow-y-auto p-6">
        <Outlet />
      </div>
    </div>
  );
}
