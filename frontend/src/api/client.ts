import axios from 'axios';

const TOKEN_KEY = 'pms_token';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
});

export function setToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

api.interceptors.response.use(
  (res) => res,
  (error) => {
    if (error.response?.status === 401 && !error.config?.url?.includes('/auth/login')) {
      setToken(null);
      if (window.location.pathname !== '/login') window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export function apiError(e: unknown): string {
  if (axios.isAxiosError(e)) {
    return e.response?.data?.error ?? e.response?.data?.title ?? e.message;
  }
  return 'Une erreur est survenue';
}
