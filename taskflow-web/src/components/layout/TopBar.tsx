import * as DropdownMenu from '@radix-ui/react-dropdown-menu';
import { Bell, ChevronRight, Menu, Search } from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';
import { useUIStore } from '../../stores/uiStore';
import { Avatar } from '../ui/Avatar';

const DEMO_UNREAD = 2;

const routeTitle: Record<string, string> = {
  '/dashboard': 'Dashboard',
  '/projects': 'Projects',
  '/team': 'Team',
  '/notifications': 'Notifications',
  '/settings': 'Settings',
};

function breadcrumbLabel(pathname: string): string {
  return routeTitle[pathname] ?? 'TaskFlow';
}

export default function TopBar() {
  const { setMobileSidebar } = useUIStore();
  const { pathname } = useLocation();
  const current = breadcrumbLabel(pathname);

  return (
    <header className="sticky top-0 z-10 flex h-topbar shrink-0 items-center gap-3 border-b border-neutral-200 bg-white px-4">
      <button
        type="button"
        className="flex h-8 w-8 items-center justify-center rounded text-neutral-600 hover:bg-neutral-100 lg:hidden"
        aria-label="Open menu"
        onClick={() => setMobileSidebar(true)}
      >
        <Menu className="h-5 w-5" />
      </button>

      <nav className="hidden min-w-0 items-center gap-1 text-13 lg:flex" aria-label="Breadcrumb">
        <Link to="/dashboard" className="shrink-0 text-neutral-500 hover:text-neutral-700">
          TaskFlow
        </Link>
        <ChevronRight className="h-3.5 w-3.5 shrink-0 text-neutral-400" aria-hidden />
        <span className="truncate font-medium text-neutral-800">{current}</span>
      </nav>

      <div className="mx-4 hidden min-w-0 flex-1 justify-center sm:flex">
        <button
          type="button"
          className="flex h-[30px] w-full max-w-[320px] cursor-pointer items-center gap-2 rounded border border-neutral-200 bg-neutral-100 px-3 text-13 text-neutral-400 transition-colors hover:bg-neutral-150"
        >
          <Search className="h-4 w-4 shrink-0" />
          <span className="min-w-0 flex-1 truncate text-left">Search...</span>
          <kbd className="shrink-0 rounded border border-neutral-200 bg-white px-1 text-11 text-neutral-500">⌘K</kbd>
        </button>
      </div>

      <div className="min-w-0 flex-1 sm:hidden" aria-hidden />

      <div className="flex shrink-0 items-center gap-1">
        <button
          type="button"
          className="relative flex h-8 w-8 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100"
          aria-label="Notifications"
        >
          <Bell className="h-4 w-4" />
          {DEMO_UNREAD > 0 ? (
            <span className="absolute right-1 top-1 h-2 w-2 rounded-full bg-red-500 ring-2 ring-white" aria-hidden />
          ) : null}
        </button>

        <DropdownMenu.Root>
          <DropdownMenu.Trigger asChild>
            <button
              type="button"
              className="flex h-8 w-8 items-center justify-center rounded outline-none focus-visible:ring-2 focus-visible:ring-primary-500"
              aria-label="User menu"
            >
              <Avatar name="Alex Johnson" size="sm" />
            </button>
          </DropdownMenu.Trigger>
          <DropdownMenu.Portal>
            <DropdownMenu.Content
              className="z-50 min-w-[200px] rounded-md border border-neutral-200 bg-white py-1 shadow-e200"
              sideOffset={6}
              align="end"
            >
              <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50">
                View profile
              </DropdownMenu.Item>
              <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-neutral-700 outline-none data-[highlighted]:bg-neutral-50">
                Settings
              </DropdownMenu.Item>
              <DropdownMenu.Separator className="my-1 h-px bg-neutral-150" />
              <DropdownMenu.Item className="cursor-pointer px-3 py-2 text-13 text-red-600 outline-none data-[highlighted]:bg-red-50">
                Sign out
              </DropdownMenu.Item>
            </DropdownMenu.Content>
          </DropdownMenu.Portal>
        </DropdownMenu.Root>
      </div>
    </header>
  );
}
