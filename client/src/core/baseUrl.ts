const isProduction = import.meta.env.PROD;

// Prefer an explicit environment variable when building the client for production.
// Fallbacks:
// - Production: same-origin (useful if UI is served by the API container)
// - Development: localhost default (can be overridden via VITE_API_BASE_URL)
const envApiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim();

const devDefault = "http://localhost:5284";

export const baseUrl = isProduction
  ? (envApiBaseUrl || (typeof window !== "undefined" ? window.location.origin : ""))
  : (envApiBaseUrl || devDefault);