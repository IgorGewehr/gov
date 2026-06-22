// Contexto de notificações (toasts). Separado do provider para evitar avisos de
// fast-refresh (arquivo só exporta o contexto e tipos).
import { createContext } from 'react';
import type { AlertVariant } from '../Alert';

export interface ToastOptions {
  variant?: AlertVariant;
  title?: string;
  /** Duração em ms; 0 mantém até o usuário fechar. Padrão 5000. */
  durationMs?: number;
}

export interface ToastApi {
  /** Exibe uma notificação. Retorna o id para descarte manual. */
  notify: (message: string, options?: ToastOptions) => string;
  success: (message: string, title?: string) => string;
  error: (message: string, title?: string) => string;
  warning: (message: string, title?: string) => string;
  info: (message: string, title?: string) => string;
  dismiss: (id: string) => void;
}

export const ToastContext = createContext<ToastApi | null>(null);
