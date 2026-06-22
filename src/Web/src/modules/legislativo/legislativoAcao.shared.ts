// Tipos e utilitarios compartilhados pelos modais de ACAO do modulo Legislativo.
// Mantem cada arquivo de modais < 300 linhas (CLAUDE.md §13).
import { ApiError } from '../../api/problemDetails';

/** Props base de um modal de acao vinculado a uma entidade do Legislativo. */
export interface AcaoModalBaseProps {
  open: boolean;
  onClose: () => void;
  /** Identificador da entidade-alvo (proposicao/sessao/votacao). */
  id: string;
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

/** Mensagem amigavel a partir de um erro (ApiError.userMessage ou fallback). */
export function mensagemErro(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.userMessage : fallback;
}
