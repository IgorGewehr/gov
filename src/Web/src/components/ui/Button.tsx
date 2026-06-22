// Botão tipado sobre as classes gov.br (br-button). Estados: variantes, bloco,
// loading (mostra Spinner + desabilita) e disabled. Máx. 1 primário por contexto (DS §4).
import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Spinner } from './Spinner';

export type ButtonVariant = 'primary' | 'secondary' | 'tertiary' | 'danger';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  /** Ocupa 100% da largura. */
  block?: boolean;
  /** Exibe spinner e desabilita o botão. */
  loading?: boolean;
  /** Apenas ícone: exige aria-label e aplica a classe circle. */
  iconOnly?: boolean;
  children?: ReactNode;
}

const VARIANT_CLASS: Record<ButtonVariant, string> = {
  primary: 'primary',
  secondary: 'secondary',
  tertiary: '',
  danger: 'danger',
};

export function Button({
  variant = 'secondary',
  block = false,
  loading = false,
  iconOnly = false,
  disabled,
  className,
  children,
  type = 'button',
  ...rest
}: ButtonProps) {
  const classes = [
    'br-button',
    VARIANT_CLASS[variant],
    block ? 'block' : '',
    iconOnly ? 'circle' : '',
    loading ? 'loading' : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button
      type={type}
      className={classes}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      {...rest}
    >
      {loading && <Spinner label="Processando…" medium={false} />}
      {children}
    </button>
  );
}
