// Botão tipado sobre as classes gov.br (br-button). Estados: variantes, bloco,
// loading (mostra Spinner + desabilita) e disabled. Máx. 1 primário por contexto (DS §4).
import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Spinner } from './Spinner';

// `ghost` é alias de `tertiary` (sem borda) — nome mais claro para a fase Aplicar.
export type ButtonVariant = 'primary' | 'secondary' | 'tertiary' | 'ghost' | 'danger';

/** Tamanho do botão: `sm` (32px, fonte sm) ou `md` (40px, padrão). */
export type ButtonSize = 'sm' | 'md';

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  /** Tamanho do controle (default `md`). `sm` casa com tabelas e toolbars densas. */
  size?: ButtonSize;
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
  ghost: '', // mesma classe gov.br do tertiary (sem borda)
  danger: 'danger',
};

export function Button({
  variant = 'secondary',
  size = 'md',
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
    size === 'sm' ? 'small' : '',
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
