import { create } from 'zustand';

export const useComplaintStore = create((set) => ({
  dashboard: null,
  complaints: [],
  selectedComplaint: null,
  setDashboard: (dashboard) => set({ dashboard }),
  setComplaints: (complaints) => set({ complaints }),
  setSelectedComplaint: (selectedComplaint) => set({ selectedComplaint }),
  clearSelectedComplaint: () => set({ selectedComplaint: null }),
}));
