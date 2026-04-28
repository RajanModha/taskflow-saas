import * as Tooltip from '@radix-ui/react-tooltip';
import {
  Bell,
  ChevronLeft,
  FolderOpen,
  LayoutDashboard,
  Search,
  Settings,
  Users,
} from 'lucide-react';
import { NavLink } from 'react-router-dom';
import { useMe } from '../../hooks/api/auth.hooks';
import { useUnreadCount } from '../../hooks/api/notifications.hooks';
import { cn } from '../../lib/utils';
import { useAuthStore } from '../../stores/authStore';
import { useUIStore } from '../../stores/uiStore';
import { Avatar } from '../ui/Avatar';

function NavItem({
  to,
  label,
  icon: Icon,
  collapsed,
  onNavigate,
}: {
  to: string;
  label: string;
  icon: typeof LayoutDashboard;
  collapsed: boolean;
  onNavigate?: () => void;
}) {
  const content = ({ isActive }: { isActive: boolean }) =>
    collapsed ? (
      <span
        className={cn(
          'mx-auto flex h-8 w-8 items-center justify-center rounded',
          isActive ? 'bg-primary-50 text-primary-700' : 'text-neutral-600 hover:bg-neutral-50 hover:text-neutral-800',
        )}
      >
        <Icon className="h-4 w-4 shrink-0" />
      </span>
    ) : (
      <span
        className={cn(
          'mx-2 flex h-8 items-center gap-2.5 rounded px-3 text-13',
          isActive
            ? 'bg-primary-50 font-medium text-primary-700'
            : 'text-neutral-600 hover:bg-neutral-50 hover:text-neutral-800',
        )}
      >
        <Icon className="h-4 w-4 shrink-0" />
        {label}
      </span>
    );

  const link = (
    <NavLink
      to={to}
      end={to === '/dashboard'}
      onClick={onNavigate}
      className={() => cn(collapsed && 'flex justify-center')}
    >
      {({ isActive }) => content({ isActive })}
    </NavLink>
  );

  if (!collapsed) {
    return <div className="mb-0.5">{link}</div>;
  }

  return (
    <div className="mb-0.5 flex justify-center">
      <Tooltip.Root>
        <Tooltip.Trigger asChild>{link}</Tooltip.Trigger>
        <Tooltip.Portal>
          <Tooltip.Content
            side="right"
            sideOffset={8}
            className="z-50 rounded bg-neutral-900 px-2 py-1 text-11 text-neutral-0 shadow-e200"
          >
            {label}
            <Tooltip.Arrow className="fill-neutral-900" />
          </Tooltip.Content>
        </Tooltip.Portal>
      </Tooltip.Root>
    </div>
  );
}

export interface SidebarContentProps {
  /** Mobile drawer: always expanded rail; closes drawer on nav */
  forceExpanded?: boolean;
  onNavigate?: () => void;
}

export function SidebarContent({ forceExpanded = false, onNavigate }: SidebarContentProps) {
  const { sidebarCollapsed } = useUIStore();
  const storedUser = useAuthStore((state) => state.user);
  const { data: me } = useMe();
  const { data: unread } = useUnreadCount();
  const collapsed = forceExpanded ? false : sidebarCollapsed;
  const showSearchNav = collapsed && !forceExpanded;
  const unreadCount = unread?.count ?? 0;
  const displayName = me?.displayName ?? me?.userName ?? storedUser?.displayName ?? storedUser?.userName ?? 'User';

  return (
    <div className="flex h-full flex-col border-r border-neutral-200 bg-white">
      <div
        className={cn(
          'flex h-topbar shrink-0 items-center border-b border-neutral-150 px-3',
          collapsed ? 'justify-center' : 'gap-2',
        )}
      >
        <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded bg-primary-600 text-11 font-bold text-white">
          TF
        </div>
        {!collapsed ? <span className="truncate text-14 font-bold text-primary-700">TaskFlow</span> : null}
      </div>

      <nav className="flex-1 overflow-y-auto py-2">
        <NavItem to="/dashboard" label="Dashboard" icon={LayoutDashboard} collapsed={collapsed} onNavigate={onNavigate} />
        <NavItem to="/projects" label="Projects" icon={FolderOpen} collapsed={collapsed} onNavigate={onNavigate} />

        <div className="mx-3 my-2 border-t border-neutral-150" />

        <NavItem to="/team" label="Team" icon={Users} collapsed={collapsed} onNavigate={onNavigate} />
        <div className="mb-0.5">
          {collapsed ? (
            <Tooltip.Root>
              <Tooltip.Trigger asChild>
                <NavLink
                  to="/notifications"
                  onClick={onNavigate}
                  className={({ isActive }) =>
                    cn(
                      'relative mx-auto flex h-8 w-8 items-center justify-center rounded',
                      isActive ? 'bg-primary-50 text-primary-700' : 'text-neutral-600 hover:bg-neutral-50 hover:text-neutral-800',
                    )
                  }
                >
                  <Bell className="h-4 w-4" />
                  {unreadCount > 0 ? (
                    <span className="absolute right-1 top-1 h-2 w-2 rounded-full bg-red-500 ring-2 ring-white" />
                  ) : null}
                </NavLink>
              </Tooltip.Trigger>
              <Tooltip.Portal>
                <Tooltip.Content
                  side="right"
                  sideOffset={8}
                  className="z-50 rounded bg-neutral-900 px-2 py-1 text-11 text-neutral-0 shadow-e200"
                >
                  Notifications
                  <Tooltip.Arrow className="fill-neutral-900" />
                </Tooltip.Content>
              </Tooltip.Portal>
            </Tooltip.Root>
          ) : (
            <NavLink
              to="/notifications"
              onClick={onNavigate}
              className={({ isActive }) =>
                cn(
                  'relative mx-2 flex h-8 items-center gap-2.5 rounded px-3 text-13',
                  isActive
                    ? 'bg-primary-50 font-medium text-primary-700'
                    : 'text-neutral-600 hover:bg-neutral-50 hover:text-neutral-800',
                )
              }
            >
              <Bell className="h-4 w-4 shrink-0" />
              Notifications
              {unreadCount > 0 ? (
                <span className="ml-auto flex h-4 min-w-4 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-semibold text-white">
                  {unreadCount > 9 ? '9+' : unreadCount}
                </span>
              ) : null}
            </NavLink>
          )}
        </div>

        {showSearchNav ? (
          <>
            <div className="mx-3 my-2 border-t border-neutral-150" />
            <div className="mb-0.5 flex justify-center">
              <Tooltip.Root>
                <Tooltip.Trigger asChild>
                  <button
                    type="button"
                    className="mx-auto flex h-8 w-8 items-center justify-center rounded text-neutral-600 hover:bg-neutral-50 hover:text-neutral-800"
                    aria-label="Search"
                  >
                    <Search className="h-4 w-4" />
                  </button>
                </Tooltip.Trigger>
                <Tooltip.Portal>
                  <Tooltip.Content
                    side="right"
                    sideOffset={8}
                    className="z-50 rounded bg-neutral-900 px-2 py-1 text-11 text-neutral-0 shadow-e200"
                  >
                    Search
                    <Tooltip.Arrow className="fill-neutral-900" />
                  </Tooltip.Content>
                </Tooltip.Portal>
              </Tooltip.Root>
            </div>
          </>
        ) : null}
      </nav>

      <div className="shrink-0 border-t border-neutral-150 p-2">
        {collapsed ? (
          <div className="flex flex-col items-center gap-2">
            <Tooltip.Root>
              <Tooltip.Trigger asChild>
                <div className="flex justify-center">
                  <Avatar name={displayName} size="sm" src={me?.avatarUrl ?? undefined} />
                </div>
              </Tooltip.Trigger>
              <Tooltip.Portal>
                <Tooltip.Content
                  side="right"
                  sideOffset={8}
                  className="z-50 rounded bg-neutral-900 px-2 py-1 text-11 text-neutral-0 shadow-e200"
                >
                  {displayName}
                  <Tooltip.Arrow className="fill-neutral-900" />
                </Tooltip.Content>
              </Tooltip.Portal>
            </Tooltip.Root>
            <NavLink
              to="/settings"
              onClick={onNavigate}
              className="flex h-8 w-8 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100 hover:text-neutral-700"
              aria-label="Settings"
            >
              <Settings className="h-4 w-4" />
            </NavLink>
          </div>
        ) : (
          <div className="flex items-center gap-2 px-1">
            <Avatar name={displayName} size="sm" src={me?.avatarUrl ?? undefined} />
            <span className="min-w-0 flex-1 truncate text-13 font-medium text-neutral-800">{displayName}</span>
            <NavLink
              to="/settings"
              onClick={onNavigate}
              className="flex h-8 w-8 shrink-0 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100 hover:text-neutral-700"
              aria-label="Settings"
            >
              <Settings className="h-4 w-4" />
            </NavLink>
          </div>
        )}
      </div>
    </div>
  );
}

export default function Sidebar() {
  const { sidebarCollapsed, toggleSidebar } = useUIStore();

  return (
    <aside
      className={cn(
        'sidebar-nav fixed left-0 top-0 z-20 hidden h-full flex-col lg:flex',
        sidebarCollapsed ? 'w-sidebar-collapsed' : 'w-sidebar',
      )}
    >
      <div className="relative flex min-h-0 flex-1 flex-col">
        <SidebarContent />
        <button
          type="button"
          onClick={toggleSidebar}
          className="absolute -right-3 top-[72px] z-30 flex h-6 w-6 items-center justify-center rounded-full border border-neutral-200 bg-white shadow-e100"
          aria-label={sidebarCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          <ChevronLeft className={cn('h-3.5 w-3.5 text-neutral-600', sidebarCollapsed && 'rotate-180')} />
        </button>
      </div>
    </aside>
  );
}
