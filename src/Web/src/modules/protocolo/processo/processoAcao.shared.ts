// Tipos e utilitarios compartilhados pelos modais de ACAO do agregado Processo.
// Extraido de ProcessoAcaoModals para manter cada arquivo < 300 linhas (CLAUDE.md §13).
import { ApiError } from '../../../api/problemDetails';

export const MOTIVO_MAX = 500;

export interface AcaoModalBaseProps {
  open: boolean;
  onClose: () => void;
  processoId: string;
  /** NUP usado para invalidar o detalhe apos a acao. */
  nup: string;
}

/** Mapeia ProblemDetails.fieldErrors para os campos conhecidos do formulario. */
export function tratarErroCampos(
  error: unknown,
  conhecidos: Record<string, number>,
): Record<string, string> {
  const mapped: Record<string, string> = {};
  if (error instanceof ApiError) {
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      if (key in conhecidos) mapped[key] = messages[0];
    }
  }
  return mapped;
}
