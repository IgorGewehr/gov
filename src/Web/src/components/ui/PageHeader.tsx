// Cabeçalho de página: título (h1 — um por página/main), descrição e área de ações.
import type { ReactNode } from 'react';

export interface PageHeaderProps {
  title: ReactNode;
  description?: ReactNode;
  /** Ações à direita (ex.: botão primário "Novo"). */
  actions?: ReactNode;
}

export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="tg-page-header">
      <div className="tg-page-header-titulos">
        <h1 className="tg-page-title mb-1">{title}</h1>
        {description && <p className="tg-page-subtitulo mb-0">{description}</p>}
      </div>
      {actions && <div className="tg-page-header-acoes">{actions}</div>}
    </div>
  );
}
