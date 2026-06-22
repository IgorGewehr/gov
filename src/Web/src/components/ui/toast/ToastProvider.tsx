// Provider de notificações. Renderiza uma região aria-live (polite) no canto da
// tela. Auto-descarte por timer; cada toast usa o componente Alert (cor+ícone+texto).
import { useCallback, useMemo, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { Alert } from '../Alert';
import type { AlertVariant } from '../Alert';
import { ToastContext } from './ToastContext';
import type { ToastApi, ToastOptions } from './ToastContext';

interface ToastItem {
  id: string;
  message: string;
  variant: AlertVariant;
  title?: string;
}

let counter = 0;
function nextId(): string {
  counter += 1;
  return `toast-${counter}-${Date.now()}`;
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const timers = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  const dismiss = useCallback((id: string) => {
    setToasts((current) => current.filter((t) => t.id !== id));
    const timer = timers.current.get(id);
    if (timer) {
      clearTimeout(timer);
      timers.current.delete(id);
    }
  }, []);

  const notify = useCallback(
    (message: string, options: ToastOptions = {}): string => {
      const id = nextId();
      const variant = options.variant ?? 'info';
      setToasts((current) => [...current, { id, message, variant, title: options.title }]);
      const duration = options.durationMs ?? 5000;
      if (duration > 0) {
        const timer = setTimeout(() => dismiss(id), duration);
        timers.current.set(id, timer);
      }
      return id;
    },
    [dismiss],
  );

  const api = useMemo<ToastApi>(
    () => ({
      notify,
      success: (message, title) => notify(message, { variant: 'success', title }),
      error: (message, title) => notify(message, { variant: 'danger', title, durationMs: 8000 }),
      warning: (message, title) => notify(message, { variant: 'warning', title }),
      info: (message, title) => notify(message, { variant: 'info', title }),
      dismiss,
    }),
    [notify, dismiss],
  );

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div
        className="br-toast-region"
        role="region"
        aria-live="polite"
        aria-label="Notificações"
        style={{
          position: 'fixed',
          top: 'var(--spacing-scale-2x)',
          right: 'var(--spacing-scale-2x)',
          zIndex: 'var(--z-index-layer-3, 9999)' as React.CSSProperties['zIndex'],
          maxWidth: '24rem',
          display: 'flex',
          flexDirection: 'column',
          gap: 'var(--spacing-scale-1x)',
        }}
      >
        {toasts.map((toast) => (
          <Alert key={toast.id} variant={toast.variant} title={toast.title}>
            {toast.message}{' '}
            <button
              type="button"
              className="br-button circle small"
              aria-label="Fechar notificação"
              onClick={() => dismiss(toast.id)}
            >
              <i className="fas fa-times" aria-hidden="true" />
            </button>
          </Alert>
        ))}
      </div>
    </ToastContext.Provider>
  );
}
