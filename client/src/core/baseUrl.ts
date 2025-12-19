const isProduction = import.meta.env.PROD;


const envApiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim();

const devDefault = "http://localhost:5284";

export const baseUrl = isProduction
  ? (envApiBaseUrl || (typeof window !== "undefined" ? window.location.origin : ""))
  : (envApiBaseUrl || devDefault);
