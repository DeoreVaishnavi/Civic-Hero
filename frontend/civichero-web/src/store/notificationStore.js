import { create } from 'zustand';

export const useNotificationStore = create((set) => ({
  items: [],
  unreadCount: 0,
  connectionState: 'disconnected',
  setNotifications: (payload) => set({ items: payload.items ?? [], unreadCount: payload.unreadCount ?? 0 }),
  setUnreadCount: (unreadCount) => set({ unreadCount }),
  setConnectionState: (connectionState) => set({ connectionState }),
  receive: (notification) => set((state) => ({
    items: [notification, ...state.items.filter((item) => item.id !== notification.id)],
    unreadCount: notification.isRead ? state.unreadCount : state.unreadCount + 1,
  })),
  markRead: (id) => set((state) => ({
    items: state.items.map((item) => item.id === id ? { ...item, isRead: true } : item),
    unreadCount: Math.max(0, state.unreadCount - (state.items.some((item) => item.id === id && !item.isRead) ? 1 : 0)),
  })),
  markAllRead: () => set((state) => ({ items: state.items.map((item) => ({ ...item, isRead: true })), unreadCount: 0 })),
  remove: (id) => set((state) => {
    const item = state.items.find((entry) => entry.id === id);
    return { items: state.items.filter((entry) => entry.id !== id), unreadCount: Math.max(0, state.unreadCount - (item && !item.isRead ? 1 : 0)) };
  }),
  reset: () => set({ items: [], unreadCount: 0, connectionState: 'disconnected' }),
}));
