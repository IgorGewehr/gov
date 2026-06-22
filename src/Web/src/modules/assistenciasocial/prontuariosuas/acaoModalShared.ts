// Tipos e utilitarios compartilhados pelos modais de acao do ProntuarioSuas.
import { ApiError } from '../../../api/problemDetails';

/** Props comuns a todos os modais de acao (abertura controlada + alvo). */
export interface ModalAcaoBaseProps {
  open: boolean;
  onClose: () => void;
  prontuarioId: string;
}

/** Mapeia ProblemDetails.errors (PascalCase) para chaves locais (camelCase) aceitas. */
export function mapearErros(
  error: unknown,
  aceitos: Record<string, number>,
): Record<string, string> {
  const mapped: Record<string, string> = {};
  if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      if (key in aceitos) mapped[key] = messages[0];
    }
  }
  return mapped;
}
