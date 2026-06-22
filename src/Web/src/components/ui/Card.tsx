// Card gov.br (br-card). Header/footer opcionais.
import type { ReactNode } from 'react';

export interface CardProps {
  children: ReactNode;
  header?: ReactNode;
  footer?: ReactNode;
  className?: string;
}

export function Card({ children, header, footer, className }: CardProps) {
  return (
    <div className={`br-card${className ? ` ${className}` : ''}`}>
      {header && <div className="card-header">{header}</div>}
      <div className="card-content">{children}</div>
      {footer && <div className="card-footer">{footer}</div>}
    </div>
  );
}
