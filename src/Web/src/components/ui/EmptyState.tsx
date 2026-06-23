// Estado vazio acessível. Comunica ausência de dados + ação opcional.
// `inline` (padrão) é compacto — não domina a tela dentro de um card/tabela.
// `page` centraliza em altura cheia, para uma tela 100% vazia.
import type { ReactNode } from 'react';

export interface EmptyStateProps {
  title: ReactNode;
  description?: ReactNode;
  /** Ícone Font Awesome (decorativo, aria-hidden). */
  icon?: string;
  action?: ReactNode;
  /** `inline` (compacto, default) ou `page` (centralizado, altura cheia). */
  size?: 'inline' | 'page';
}

export function EmptyState({
  title,
  description,
  icon = 'fas fa-folder-open',
  action,
  size = 'inline',
}: EmptyStateProps) {
  return (
    <div className={`tg-empty${size === 'page' ? ' tg-empty-page' : ''}`} role="status">
      <span className="tg-empty-icone" aria-hidden="true">
        <i className={`${icon} fa-lg`} />
      </span>
      <p className="tg-empty-titulo">{title}</p>
      {description && <p className="tg-empty-descricao">{description}</p>}
      {action}
    </div>
  );
}
