// Utilitários compartilhados pelos modais de ação (commands) do módulo RecursosHumanos.
// Mantém o mapeamento de ProblemDetails.fieldErrors → erros por campo num único lugar.
import { ApiError } from '../../api/problemDetails';

/** Limite padrão de caracteres para campos de motivo/justificativa. */
export const MOTIVO_MAX = 500;

/**
 * Mapeia os fieldErrors de um ApiError para as chaves de formulário conhecidas
 * (camelCase). Campos desconhecidos são ignorados.
 */
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
