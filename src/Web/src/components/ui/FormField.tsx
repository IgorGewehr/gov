// Wrapper de campo de formulário: associa label, mensagem de erro e texto de ajuda
// ao controle via aria-describedby/aria-invalid (DS §5 / eMAG). Gera ids estáveis.
import { useId } from 'react';
import type { ReactNode } from 'react';

export interface FormFieldRenderProps {
  /** id a aplicar no controle (associa ao <label htmlFor>). */
  id: string;
  /** Ids a passar em aria-describedby (ajuda + erro). undefined quando vazio. */
  describedBy: string | undefined;
  /** true quando há erro; aplicar em aria-invalid. */
  invalid: boolean;
}

export interface FormFieldProps {
  label: ReactNode;
  /** Mensagem de erro; quando presente marca o campo como inválido. */
  error?: string | null;
  /** Texto de ajuda exibido abaixo do controle. */
  help?: ReactNode;
  /** Marca o campo como obrigatório (asterisco + atributo no controle a cargo do consumidor). */
  required?: boolean;
  /** Render prop que recebe os ids/aria a aplicar no controle. */
  children: (props: FormFieldRenderProps) => ReactNode;
}

export function FormField({ label, error, help, required = false, children }: FormFieldProps) {
  const baseId = useId();
  const fieldId = `${baseId}-field`;
  const helpId = `${baseId}-help`;
  const errorId = `${baseId}-error`;

  const describedBy = [help ? helpId : null, error ? errorId : null].filter(Boolean).join(' ') || undefined;

  return (
    <div className={`br-input${error ? ' danger' : ''}`}>
      <label htmlFor={fieldId}>
        {label}
        {required && (
          <span className="text-red-vivid-50" aria-hidden="true">
            {' '}
            *
          </span>
        )}
      </label>

      {children({ id: fieldId, describedBy, invalid: Boolean(error) })}

      {help && (
        <span className="feedback" id={helpId}>
          {help}
        </span>
      )}

      {error && (
        <span className="feedback danger" id={errorId} role="alert">
          <i className="fas fa-times-circle" aria-hidden="true" /> {error}
        </span>
      )}
    </div>
  );
}
