import * as Tooltip from '@radix-ui/react-tooltip';
import { AnimatePresence, motion } from 'framer-motion';
import { Outlet } from 'react-router-dom';
import { useMediaQuery } from '../../hooks/useMediaQuery';
import { useUIStore } from '../../stores/uiStore';
import { TaskDetailSlideOver } from '../tasks/TaskDetailSlideOver';
import { CommandPalette } from './CommandPalette';
import Sidebar, { SidebarContent } from './Sidebar';
import TopBar from './TopBar';

export default function AppLayout() {
  const { sidebarCollapsed, mobileSidebarOpen, setMobileSidebar, taskSlideOverTaskId, closeTaskSlideOver } = useUIStore();
  const isLg = useMediaQuery('(min-width: 1024px)');
  const sidebarW = sidebarCollapsed ? 48 : 220;

  return (
    <Tooltip.Provider delayDuration={200}>
      <div className="flex h-screen overflow-hidden bg-surface-page">
        <Sidebar />

        <AnimatePresence>
          {mobileSidebarOpen ? (
            <>
              <motion.div
                className="fixed inset-0 z-30 bg-surface-overlay lg:hidden"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                onClick={() => setMobileSidebar(false)}
              />
              <motion.div
                className="fixed left-0 top-0 z-40 h-full w-sidebar lg:hidden"
                initial={{ x: -220 }}
                animate={{ x: 0 }}
                exit={{ x: -220 }}
                transition={{ type: 'spring', damping: 30, stiffness: 300 }}
              >
                <SidebarContent forceExpanded onNavigate={() => setMobileSidebar(false)} />
              </motion.div>
            </>
          ) : null}
        </AnimatePresence>

        <div
          className="main-content flex min-w-0 flex-1 flex-col overflow-hidden transition-[margin]"
          style={{ marginLeft: isLg ? sidebarW : 0 }}
        >
          <TopBar />
          <main className="min-h-0 flex-1 overflow-y-auto">
            <Outlet />
          </main>
        </div>
        <CommandPalette />
        <TaskDetailSlideOver taskId={taskSlideOverTaskId} onClose={closeTaskSlideOver} />
      </div>
    </Tooltip.Provider>
  );
}
