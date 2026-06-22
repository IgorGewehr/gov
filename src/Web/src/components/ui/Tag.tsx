// Tag/badge gov.br (br-tag) com variante semântica de status.
import type { ReactNode } from 'react';

export type TagVariant = 'default' | 'success' | 'warning' | 'danger' | 'info';

export interface TagProps {
  children: ReactNode;
  variant?: TagVariant;
}

const VARIANT_CLASS: Record<TagVariant, string> = {
  default: '',
  success: 'success',
  warning: 'warning',
  danger: 'danger',
  info: 'info',
};

export function Tag({ children, variant = 'default' }: TagProps) {
  return (
    <span className={`br-tag${VARIANT_CLASS[variant] ? ` ${VARIANT_CLASS[variant]}` : ''}`}>
      <span className="text">{children}</span>
    </span>
  );
}
