import { formatDistanceToNow } from 'date-fns';
import { AnimatePresence, motion } from 'framer-motion';
import { Bell, Calendar, MessageSquare, UserPlus, Users, X } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useMarkAllRead, useNotifications, useUnreadCount, useDeleteNotification } from '../hooks/api/notifications.hooks';
import { Pagination } from '../components/ui/Pagination';
import { Button } from '../components/ui/Button';

function notificationIcon(type: string) {
  if (type === 'task.assigned') return { icon: UserPlus, className: 'bg-indigo-100 text-indigo-700' };
  if (type === 'task.commented') return { icon: MessageSquare, className: 'bg-blue-100 text-blue-700' };
  if (type === 'task.due_date_changed') return { icon: Calendar, className: 'bg-amber-100 text-amber-700' };
  if (type === 'member.joined') return { icon: Users, className: 'bg-green-100 text-green-700' };
  return { icon: Bell, className: 'bg-neutral-100 text-neutral-700' };
}

export default function NotificationsPage() {
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [page, setPage] = useState(1);
  const { data, isLoading } = useNotifications({ page, pageSize: 20, unreadOnly });
  const { data: unreadData } = useUnreadCount();
  const markAllRead = useMarkAllRead();
  const deleteNotif = useDeleteNotification();

  const unread = unreadData?.count ?? 0;
  const items = data?.items ?? [];
  const filteredItems = useMemo(() => (unreadOnly ? items.filter((item) => !item.isRead) : items), [items, unreadOnly]);

  return (
    <div className="page-wrapper">
      <div className="page-header">
        <h1 className="page-title">Notifications</h1>
        <Button size="sm" variant="secondary" disabled={unread === 0} onClick={() => markAllRead.mutate()}>
          Mark all as read
        </Button>
      </div>

      <div className="mb-3 flex items-center gap-2">
        <button
          type="button"
          className={`rounded px-3 py-1.5 text-13 ${!unreadOnly ? 'bg-primary-50 font-medium text-primary-700' : 'text-neutral-600 hover:bg-neutral-50'}`}
          onClick={() => {
            setUnreadOnly(false);
            setPage(1);
          }}
        >
          All
        </button>
        <button
          type="button"
          className={`rounded px-3 py-1.5 text-13 ${unreadOnly ? 'bg-primary-50 font-medium text-primary-700' : 'text-neutral-600 hover:bg-neutral-50'}`}
          onClick={() => {
            setUnreadOnly(true);
            setPage(1);
          }}
        >
          Unread ({unread})
        </button>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 6 }).map((_, index) => (
            <div key={index} className="h-[86px] animate-pulse rounded-md border border-neutral-200 bg-white" />
          ))}
        </div>
      ) : filteredItems.length === 0 ? (
        <div className="rounded-md border border-neutral-200 bg-white py-10 text-center">
          <Bell className="mx-auto mb-2 h-10 w-10 text-neutral-300" />
          <p className="text-14 font-medium text-neutral-600">{unreadOnly ? "You're all caught up!" : 'No notifications yet.'}</p>
        </div>
      ) : (
        <AnimatePresence>
          <div>
            {filteredItems.map((item) => {
              const iconMeta = notificationIcon(item.type);
              const Icon = iconMeta.icon;
              return (
                <motion.div
                  key={item.id}
                  layout
                  initial={{ opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, height: 0, marginBottom: 0 }}
                  className={`group mb-2 flex gap-3 rounded-md border p-4 ${
                    item.isRead ? 'border-neutral-200 bg-white' : 'border-primary-200 bg-primary-50/30'
                  }`}
                >
                  <span className={`inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-full ${iconMeta.className}`}>
                    <Icon className="h-5 w-5" />
                  </span>
                  <div className="min-w-0 flex-1">
                    <p className="text-13 font-medium text-neutral-800">{item.title}</p>
                    <p className="mt-0.5 text-13 text-neutral-600">{item.body}</p>
                    <p className="mt-1 text-12 text-neutral-400">{formatDistanceToNow(new Date(item.createdAt), { addSuffix: true })}</p>
                  </div>
                  <button
                    type="button"
                    className="self-start rounded p-1 text-neutral-400 opacity-0 transition-opacity group-hover:opacity-100 hover:bg-neutral-100 hover:text-neutral-600"
                    onClick={() => deleteNotif.mutate(item.id)}
                  >
                    <X className="h-4 w-4" />
                  </button>
                </motion.div>
              );
            })}
          </div>
        </AnimatePresence>
      )}

      {data ? <Pagination page={data.page} pageSize={data.pageSize} totalCount={data.totalCount} onPageChange={setPage} className="mt-3" /> : null}
    </div>
  );
}
