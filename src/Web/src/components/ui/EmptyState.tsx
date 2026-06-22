// Estado vazio acessível. Comunica ausência de dados + ação opcional.
import type { ReactNode } from 'react';

export interface EmptyStateProps {
  title: ReactNode;
  description?: ReactNode;
  /** Ícone Font Awesome (decorativo, aria-hidden). */
  icon?: string;
  action?: ReactNode;
}

export function EmptyState({ title, description, icon = 'fas fa-folder-open', action }: EmptyStateProps) {
  return (
    <div className="app-center text-center" role="status">
      <div className="stack">
        <i className={`${icon} fa-2x text-gray-50`} aria-hidden="true" />
        <p className="text-up-01 text-semi-bold">{title}</p>
        {description && <p className="text-gray-60">{description}</p>}
        {action}
      </div>
    </div>
  );
}
