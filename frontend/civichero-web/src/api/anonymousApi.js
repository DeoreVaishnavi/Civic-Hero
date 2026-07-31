import apiClient, { unwrap } from "./apiClient";

const toFormData = (values) => {
  const data = new FormData();
  Object.entries(values).forEach(([key, value]) => {
    if (key === "images") value.forEach((file) => data.append("images", file));
    else if (value !== undefined && value !== null) data.append(key, String(value));
  });
  return data;
};

export const anonymousApi = {
  create: async (values) => unwrap(await apiClient.post("/anonymous-complaints", toFormData(values), { headers: { "Content-Type": "multipart/form-data" } })),
  track: async (referenceNumber, trackingToken) => unwrap(await apiClient.post("/anonymous-complaints/track", { referenceNumber, trackingToken })),
};
