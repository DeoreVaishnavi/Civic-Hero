import axios from "axios";
import i18n from "../i18n/i18n";

const languageHeaders = { en: "en-US", mr: "mr-IN", hi: "hi-IN" };
export const tokenStore = { get: () => sessionStorage.getItem("civicHeroAccessToken"), set: (token) => token ? sessionStorage.setItem("civicHeroAccessToken", token) : sessionStorage.removeItem("civicHeroAccessToken"), clear: () => sessionStorage.removeItem("civicHeroAccessToken") };
const apiClient = axios.create({ baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:5000/api/v1", timeout: 20_000, withCredentials: true });
apiClient.interceptors.request.use((config) => { const token = tokenStore.get(); if (token) config.headers.Authorization = `Bearer ${token}`; const language = i18n.resolvedLanguage?.split("-")[0] || "en"; config.headers["Accept-Language"] = languageHeaders[language] || languageHeaders.en; return config; });
apiClient.interceptors.response.use((response) => response, async (error) => { const original = error.config; if (error.response?.status === 401 && !original?._retried && !original?.url?.includes("refresh-token")) { original._retried = true; try { const { data } = await apiClient.post("/auth/refresh-token"); const token = data?.data?.accessToken; if (token) { tokenStore.set(token); original.headers.Authorization = `Bearer ${token}`; return apiClient(original); } } catch { tokenStore.clear(); window.dispatchEvent(new Event("civichero:session-expired")); } } return Promise.reject(error); });
export const unwrap = (response) => response?.data?.data ?? response?.data;
export default apiClient;
