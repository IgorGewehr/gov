// Mensagem de feedback gov.br (br-message). Cor + ícone + texto (DS §6 — nunca só cor).
// role=alert para danger/warning (assertivo) e status para success/info (polido).
import type { ReactNode } from 'react';

export type AlertVariant = 'success' | 'warning' | 'danger' | 'info';

export interface AlertProps {
  variant: AlertVariant;
  title?: ReactNode;
  children: ReactNode;
}

const ICON: Record<AlertVariant, string> = {
  success: 'fas fa-check-circle',
  warning: 'fas fa-exclamation-triangle',
  danger: 'fas fa-times-circle',
  info: 'fas fa-info-circle',
};

export function Alert({ variant, title, children }: AlertProps) {
  const assertive = variant === 'danger' || variant === 'warning';
  return (
    <div className={`br-message ${variant}`} role={assertive ? 'alert' : 'status'}>
      <div className="icon">
        <i className={ICON[variant]} aria-hidden="true" />
      </div>
      <div className="content">
        {title && <span className="message-title">{title} </span>}
        <span className="message-body">{children}</span>
      </div>
    </div>
  );
}
