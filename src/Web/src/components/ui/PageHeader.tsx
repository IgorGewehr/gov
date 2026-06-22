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
    <div className="d-flex justify-content-between align-items-start flex-wrap mb-4">
      <div>
        <h1 className="mb-1">{title}</h1>
        {description && <p className="text-gray-60 mb-0">{description}</p>}
      </div>
      {actions && <div className="d-flex" style={{ gap: 'var(--spacing-scale-1x)' }}>{actions}</div>}
    </div>
  );
}
