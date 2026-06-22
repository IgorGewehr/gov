// Http client tipado sobre fetch. Base "/api" (proxy Vite -> ApiHost .NET).
// Responsabilidades: header Authorization (JWT), serialização JSON, normalização
// de erro em ApiError (ProblemDetails) e tratamento de 401 (sessão expirada).

import { ApiError, type ProblemDetails } from './problemDetails';
import { clearAccessToken, getAccessToken } from './authToken';

const BASE_URL = '/api';

export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

export interface RequestOptions {
  /** Query string. Valores nullish são ignorados. */
  query?: Record<string, string | number | boolean | null | undefined>;
  /** Corpo; objetos são serializados como JSON. */
  body?: unknown;
  signal?: AbortSignal;
  /** Cabeçalhos extra (não sobrescrevem Authorization/Accept por padrão). */
  headers?: Record<string, string>;
}

/**
 * Handler global de 401. O AuthProvider registra a navegação para /login aqui,
 * evitando acoplar o http client ao react-router.
 */
let onUnauthorized: (() => void) | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  onUnauthorized = handler;
}

function buildUrl(path: string, query?: RequestOptions['query']): string {
  const url = `${BASE_URL}${path.startsWith('/') ? path : `/${path}`}`;
  if (!query) return url;
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== null && value !== undefined) params.append(key, String(value));
  }
  const qs = params.toString();
  return qs ? `${url}?${qs}` : url;
}

async function parseProblem(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) return null;
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

async function request<T>(method: HttpMethod, path: string, options: RequestOptions = {}): Promise<T> {
  const token = getAccessToken();
  const isJsonBody = options.body !== undefined && !(options.body instanceof FormData);

  const headers: Record<string, string> = {
    Accept: 'application/json',
    ...(isJsonBody ? { 'Content-Type': 'application/json' } : {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
    ...options.headers,
  };

  let response: Response;
  try {
    response = await fetch(buildUrl(path, options.query), {
      method,
      headers,
      signal: options.signal,
      body:
        options.body === undefined
          ? undefined
          : options.body instanceof FormData
            ? options.body
            : JSON.stringify(options.body),
    });
  } catch (cause) {
    // Falha de rede / CORS / abort.
    if (cause instanceof DOMException && cause.name === 'AbortError') throw cause;
    throw new ApiError('Não foi possível conectar ao servidor. Verifique sua conexão.', 0);
  }

  if (response.status === 401) {
    // 401 = sessão expirada/ inválida → limpa o token e leva ao login (auth real).
    clearAccessToken();
    onUnauthorized?.();
    throw new ApiError('Sua sessão expirou. Faça login novamente.', 401, await parseProblem(response));
  }

  if (!response.ok) {
    const problem = await parseProblem(response);
    const message = problem?.detail ?? problem?.title ?? `Erro ao processar a solicitação (HTTP ${response.status}).`;
    throw new ApiError(message, response.status, problem);
  }

  if (response.status === 204) return undefined as T;

  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) return undefined as T;
  return (await response.json()) as T;
}

export const http = {
  get: <T>(path: string, options?: RequestOptions): Promise<T> => request<T>('GET', path, options),
  post: <T>(path: string, body?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('POST', path, { ...options, body }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('PUT', path, { ...options, body }),
  patch: <T>(path: string, body?: unknown, options?: RequestOptions): Promise<T> =>
    request<T>('PATCH', path, { ...options, body }),
  delete: <T>(path: string, options?: RequestOptions): Promise<T> => request<T>('DELETE', path, options),
};
