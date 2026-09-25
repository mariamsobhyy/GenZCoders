// Set VITE_API_URL in .env (see .env.example) for each environment; defaults to the local backend.
export const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5109';

export const API_TIMEOUT_MS = 10000;
