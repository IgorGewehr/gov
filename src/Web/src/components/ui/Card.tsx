// Card gov.br (br-card) com elevação real (sombra + hairline; nunca cinza chapado).
// Header/footer opcionais e acento de status no topo (prop `accent`).
import type { ReactNode } from 'react';

/** Acento de status (barra superior 3px) — usado em cards de painel. */
export type CardAccent = 'primary' | 'success' | 'warning' | 'danger';

export interface CardProps {
  children: ReactNode;
  header?: ReactNode;
  footer?: ReactNode;
  className?: string;
  /** Barra de acento no topo do card (status). Status nunca só por cor. */
  accent?: CardAccent;
}

export function Card({ children, header, footer, className, accent }: CardProps) {
  const classes = [
    'br-card',
    accent ? `tg-card-accent-${accent}` : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className={classes}>
      {header && <div className="card-header">{header}</div>}
      <div className="card-content">{children}</div>
      {footer && <div className="card-footer">{footer}</div>}
    </div>
  );
}
