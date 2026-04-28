import * as Dialog from '@radix-ui/react-dialog';
import { CheckSquare, FolderOpen, Loader2, MessageSquare, Search } from 'lucide-react';
import { AnimatePresence, motion } from 'framer-motion';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useSearch } from '../../hooks/api/search.hooks';
import type { SearchHitDto } from '../../types/api';
import { useUIStore } from '../../stores/uiStore';

function itemIcon(type: SearchHitDto['type']) {
  if (type === 'project') return FolderOpen;
  if (type === 'task') return CheckSquare;
  return MessageSquare;
}

export function CommandPalette() {
  const navigate = useNavigate();
  const isOpen = useUIStore((state) => state.isCommandPaletteOpen);
  const openCommandPalette = useUIStore((state) => state.openCommandPalette);
  const closeCommandPalette = useUIStore((state) => state.closeCommandPalette);
  const openTaskSlideOver = useUIStore((state) => state.openTaskSlideOver);

  const [query, setQuery] = useState('');
  const [focused, setFocused] = useState(0);
  const [recent, setRecent] = useState<string[]>(() => {
    try {
      return JSON.parse(localStorage.getItem('tf-recent-searches') ?? '[]');
    } catch {
      return [];
    }
  });

  const { data: results, isFetching } = useSearch(query);
  const allItems = useMemo(() => [...(results?.projects ?? []), ...(results?.tasks ?? []), ...(results?.comments ?? [])], [results]);

  useEffect(() => {
    const onToggle = (event: KeyboardEvent) => {
      const isCmdK = (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k';
      if (isCmdK) {
        event.preventDefault();
        if (useUIStore.getState().isCommandPaletteOpen) closeCommandPalette();
        else openCommandPalette();
      }
    };
    window.addEventListener('keydown', onToggle);
    return () => window.removeEventListener('keydown', onToggle);
  }, [closeCommandPalette, openCommandPalette]);

  useEffect(() => {
    if (!isOpen) return;
    const handler = (event: KeyboardEvent) => {
      if (event.key === 'ArrowDown') {
        event.preventDefault();
        setFocused((value) => Math.min(value + 1, Math.max(allItems.length - 1, 0)));
      }
      if (event.key === 'ArrowUp') {
        event.preventDefault();
        setFocused((value) => Math.max(value - 1, 0));
      }
      if (event.key === 'Enter' && allItems[focused]) {
        event.preventDefault();
        handleSelect(allItems[focused]);
      }
      if (event.key === 'Escape') {
        event.preventDefault();
        closeCommandPalette();
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [allItems, closeCommandPalette, focused, isOpen]);

  const handleSelect = (item: SearchHitDto) => {
    const updated = [query, ...recent.filter((entry) => entry !== query)].filter(Boolean).slice(0, 5);
    setRecent(updated);
    localStorage.setItem('tf-recent-searches', JSON.stringify(updated));

    if (item.type === 'project') navigate(`/projects/${item.id}/board`);
    if (item.type === 'task') {
      const metadata = item.metadata as { projectId?: string };
      if (metadata.projectId) navigate(`/projects/${metadata.projectId}/board`);
      openTaskSlideOver(item.id);
    }
    if (item.type === 'comment') navigate('/projects');
    closeCommandPalette();
  };

  const itemChipLabel = (item: SearchHitDto) => {
    if (item.type === 'comment') return 'Comment';
    if (item.type === 'task') {
      const metadata = item.metadata as { status?: string } | null;
      return metadata?.status ?? 'Task';
    }
    return 'Project';
  };

  return (
    <Dialog.Root open={isOpen} onOpenChange={(open) => (open ? openCommandPalette() : closeCommandPalette())}>
      <Dialog.Portal>
        <AnimatePresence>
          {isOpen ? (
            <>
              <Dialog.Overlay asChild>
                <motion.div className="fixed inset-0 z-50 bg-surface-overlay backdrop-blur-sm" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }} />
              </Dialog.Overlay>
              <Dialog.Content asChild>
                <motion.div
                  className="fixed left-1/2 top-20 z-50 w-full max-w-[540px] -translate-x-1/2 overflow-hidden rounded-lg border border-neutral-200 bg-white shadow-e500"
                  initial={{ opacity: 0, scale: 0.98 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.98 }}
                >
                  <div className="flex items-center gap-2.5 border-b border-neutral-200 p-3">
                    <Search className="h-4 w-4 text-neutral-400" />
                    <input
                      className="flex-1 text-14 outline-none placeholder:text-neutral-400"
                      placeholder="Search projects, tasks, comments..."
                      value={query}
                      onChange={(event) => {
                        setQuery(event.target.value);
                        setFocused(0);
                      }}
                      autoFocus
                    />
                    {isFetching ? <Loader2 className="h-4 w-4 animate-spin text-neutral-400" /> : null}
                    <kbd className="rounded border border-neutral-200 bg-white px-1 text-11 text-neutral-500">esc</kbd>
                  </div>

                  <div className="max-h-[360px] overflow-y-auto">
                    {query.length < 2 ? (
                      <div>
                        <p className="px-3 pb-1 pt-3 text-11 font-semibold uppercase tracking-wider text-neutral-500">Recent</p>
                        {recent.length === 0 ? <p className="px-3 py-2 text-12 text-neutral-500">No recent searches.</p> : null}
                        {recent.map((entry) => (
                          <button key={entry} type="button" className="block w-full px-3 py-2.5 text-left text-13 text-neutral-700 hover:bg-neutral-50" onClick={() => setQuery(entry)}>
                            {entry}
                          </button>
                        ))}
                      </div>
                    ) : results && results.totalResults > 0 ? (
                      <>
                        {[
                          { label: 'Projects', items: results.projects },
                          { label: 'Tasks', items: results.tasks },
                          { label: 'Comments', items: results.comments },
                        ].map((group) =>
                          group.items.length > 0 ? (
                            <div key={group.label}>
                              <p className="sticky top-0 bg-neutral-50 px-3 py-1.5 text-11 font-semibold uppercase tracking-wider text-neutral-500">{group.label}</p>
                              {group.items.map((item) => {
                                const index = allItems.findIndex((entry) => entry.id === item.id && entry.type === item.type);
                                const focusedRow = index === focused;
                                const Icon = itemIcon(item.type);
                                return (
                                  <button
                                    key={`${item.type}-${item.id}`}
                                    type="button"
                                    className={`flex w-full items-center gap-2.5 px-3 py-2.5 text-left hover:bg-primary-50 ${
                                      focusedRow ? 'border-l-2 border-primary-500 bg-primary-50' : ''
                                    }`}
                                    onMouseEnter={() => setFocused(index)}
                                    onClick={() => handleSelect(item)}
                                  >
                                    <span className="inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-primary-100 text-primary-700">
                                      <Icon className="h-4 w-4" />
                                    </span>
                                    <span className="min-w-0 flex-1">
                                      <span className="block truncate text-13 font-medium text-neutral-800">{item.title}</span>
                                      <span className="block truncate text-12 text-neutral-400">{item.snippet}</span>
                                    </span>
                                    <span className="shrink-0 text-right">
                                      <span className="rounded bg-neutral-100 px-1.5 py-0.5 text-10 text-neutral-600">{itemChipLabel(item)}</span>
                                      {focusedRow ? <span className="mt-1 block text-11 text-neutral-500">↵</span> : null}
                                    </span>
                                  </button>
                                );
                              })}
                            </div>
                          ) : null,
                        )}
                      </>
                    ) : (
                      <div className="py-8 text-center">
                        <p className="text-13 text-neutral-700">No results for "{query}"</p>
                        <p className="text-12 text-neutral-400">Try different keywords</p>
                      </div>
                    )}
                  </div>

                  <div className="flex gap-4 border-t border-neutral-200 bg-neutral-50 px-3 py-2 text-11 text-neutral-400">
                    <span>↑↓ Navigate</span>
                    <span>↵ Open</span>
                    <span>Esc Close</span>
                  </div>
                </motion.div>
              </Dialog.Content>
            </>
          ) : null}
        </AnimatePresence>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
