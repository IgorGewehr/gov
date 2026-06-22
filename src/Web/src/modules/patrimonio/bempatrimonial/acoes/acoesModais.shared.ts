// Internos compartilhados pelos modais de ação do agregado BemPatrimonial.
// Extraído de BemPatrimonialAcoesModais.tsx — comportamento idêntico.
import { ApiError } from '../../../../api/problemDetails';

export const hoje = (): string => new Date().toISOString().slice(0, 10);

/** Mapeia ProblemDetails.errors do backend para mensagens de erro por campo. */
export function mapearFieldErrors<E extends Record<string, string>>(
  error: unknown,
  campos: Record<keyof E, true>,
): Partial<E> | null {
  if (!(error instanceof ApiError) || Object.keys(error.fieldErrors).length === 0) return null;
  const mapped: Record<string, string> = {};
  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const key = field.charAt(0).toLowerCase() + field.slice(1);
    if (key in campos) mapped[key] = messages[0];
  }
  return mapped as Partial<E>;
}

export interface AcaoModalProps {
  bemId: string;
  open: boolean;
  onClose: () => void;
}
