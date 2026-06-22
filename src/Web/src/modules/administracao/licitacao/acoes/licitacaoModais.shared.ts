// Internos compartilhados pelos modais de ação do agregado Licitacao.
// Extraído de LicitacaoActionModals.tsx — comportamento idêntico.
import type { ApiError } from '../../../../api/problemDetails';

export interface AcaoModalProps {
  open: boolean;
  onClose: () => void;
  licitacaoId: string;
}

export const GUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function primeiraMensagem(error: ApiError, campo: string): string | undefined {
  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    const key = field.charAt(0).toLowerCase() + field.slice(1);
    if (key === campo) return messages[0];
  }
  return undefined;
}
