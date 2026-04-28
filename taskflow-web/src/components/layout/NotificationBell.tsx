import * as Popover from '@radix-ui/react-popover';
import { Calendar, Bell, MessageSquare, UserPlus, Users } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import { useNavigate } from 'react-router-dom';
import { useMarkAllRead, useMarkRead, useNotifications, useUnreadCount } from '../../hooks/api/notifications.hooks';
import { useUIStore } from '../../stores/uiStore';

function notificationIcon(type: string) {
  if (type === 'task.assigned') return { icon: UserPlus, className: 'bg-indigo-100 text-indigo-700' };
  if (type === 'task.commented') return { icon: MessageSquare, className: 'bg-blue-100 text-blue-700' };
  if (type === 'task.due_date_changed') return { icon: Calendar, className: 'bg-amber-100 text-amber-700' };
  if (type === 'member.joined') return { icon: Users, className: 'bg-green-100 text-green-700' };
  return { icon: Bell, className: 'bg-neutral-100 text-neutral-700' };
}

export function NotificationBell() {
  const navigate = useNavigate();
  const { data: countData } = useUnreadCount();
  const unread = countData?.count ?? 0;
  const { data: recent } = useNotifications({ page: 1, pageSize: 5 });
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();
  const openTaskSlideOver = useUIStore((state) => state.openTaskSlideOver);

  return (
    <Popover.Root>
      <Popover.Trigger asChild>
        <button
          type="button"
          className="relative flex h-8 w-8 items-center justify-center rounded text-neutral-500 hover:bg-neutral-100"
          aria-label="Notifications"
        >
          <Bell className="h-4 w-4" />
          {unread > 0 ? <span className="absolute right-1 top-1 h-2 w-2 rounded-full bg-red-500" /> : null}
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content className="z-50 w-80 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-e400" sideOffset={8} align="end">
          <div className="flex items-center justify-between border-b border-neutral-100 px-3 py-2">
            <p className="text-13 font-semibold text-neutral-800">Notifications</p>
            <button type="button" className="text-12 text-primary-600" onClick={() => markAllRead.mutate()}>
              Mark all read
            </button>
          </div>

          <div className="max-h-72 overflow-y-auto">
            {(recent?.items ?? []).map((item) => {
              const iconMeta = notificationIcon(item.type);
              const Icon = iconMeta.icon;
              return (
                <button
                  key={item.id}
                  type="button"
                  className={`flex w-full cursor-pointer gap-2.5 border-b border-neutral-100 px-3 py-2.5 text-left hover:bg-neutral-50 ${
                    item.isRead ? '' : 'bg-primary-50'
                  }`}
                  onClick={() => {
                    if (!item.isRead) {
                      markRead.mutate(item.id);
                    }
                    if (item.entityType === 'Task' && item.entityId) {
                      openTaskSlideOver(item.entityId);
                      return;
                    }
                    if (item.entityType === 'Project' && item.entityId) {
                      navigate(`/projects/${item.entityId}`);
                    }
                  }}
                >
                  <span className={`inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-full ${iconMeta.className}`}>
                    <Icon className="h-4 w-4" />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block truncate text-13 font-medium text-neutral-700">{item.title}</span>
                    <span className="block truncate text-12 text-neutral-500">{item.body}</span>
                  </span>
                  <span className="shrink-0 text-right">
                    <span className="block text-11 text-neutral-400">{formatDistanceToNow(new Date(item.createdAt), { addSuffix: true })}</span>
                    {!item.isRead ? <span className="ml-auto mt-1 block h-2 w-2 rounded-full bg-primary-500" /> : null}
                  </span>
                </button>
              );
            })}
          </div>

          <button
            type="button"
            className="w-full border-t border-neutral-100 px-3 py-2 text-center text-12 text-primary-600 hover:bg-neutral-50"
            onClick={() => navigate('/notifications')}
          >
            View all →
          </button>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
