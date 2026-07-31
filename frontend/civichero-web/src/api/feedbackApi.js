import apiClient, { unwrap } from "./apiClient";
export const feedbackApi = { submit: async (complaintId, value) => unwrap(await apiClient.post(`/complaints/${complaintId}/feedback`, value)) };
