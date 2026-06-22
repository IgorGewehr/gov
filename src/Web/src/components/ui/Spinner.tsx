// Spinner gov.br (br-loading). Acessível: role="status" + texto para leitor de tela.
export interface SpinnerProps {
  /** Rótulo lido por tecnologia assistiva. */
  label?: string;
  /** Loading em escala média (padrão) ou pequena. */
  medium?: boolean;
}

export function Spinner({ label = 'Carregando…', medium = true }: SpinnerProps) {
  return (
    <span role="status" aria-live="polite">
      <span className={`br-loading${medium ? ' medium' : ''}`} aria-hidden="true" />
      <span className="sr-only">{label}</span>
    </span>
  );
}
