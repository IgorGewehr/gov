// Hook de acesso às notificações. Lança se usado fora do ToastProvider.
import { useContext } from 'react';
import { ToastContext } from './ToastContext';
import type { ToastApi } from './ToastContext';

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext);
  if (!ctx) {
    throw new Error('useToast deve ser usado dentro de <ToastProvider>.');
  }
  return ctx;
}
