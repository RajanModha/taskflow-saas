import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface UIState {
  sidebarCollapsed: boolean;
  mobileSidebarOpen: boolean;
  isCommandPaletteOpen: boolean;
  taskSlideOverTaskId: string | null;
  toggleSidebar: () => void;
  setMobileSidebar: (open: boolean) => void;
  openCommandPalette: () => void;
  closeCommandPalette: () => void;
  openTaskSlideOver: (taskId: string) => void;
  closeTaskSlideOver: () => void;
}

export const useUIStore = create<UIState>()(
  persist(
    (set) => ({
      sidebarCollapsed: false,
      mobileSidebarOpen: false,
      isCommandPaletteOpen: false,
      taskSlideOverTaskId: null,
      toggleSidebar: () => set((s) => ({ sidebarCollapsed: !s.sidebarCollapsed })),
      setMobileSidebar: (open) => set({ mobileSidebarOpen: open }),
      openCommandPalette: () => set({ isCommandPaletteOpen: true }),
      closeCommandPalette: () => set({ isCommandPaletteOpen: false }),
      openTaskSlideOver: (taskId) => set({ taskSlideOverTaskId: taskId }),
      closeTaskSlideOver: () => set({ taskSlideOverTaskId: null }),
    }),
    {
      name: 'taskflow-ui',
      partialize: (state) => ({ sidebarCollapsed: state.sidebarCollapsed }),
    },
  ),
);
