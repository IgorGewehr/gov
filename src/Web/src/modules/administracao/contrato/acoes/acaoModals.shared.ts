// Internos compartilhados pelos modais de ação do agregado Contrato.
// Extraído de ContratoAcaoModals.tsx — comportamento idêntico.
import { ApiError } from '../../../../api/problemDetails';

export interface AcaoModalProps {
  contratoId: string;
  open: boolean;
  onClose: () => void;
}

export function mapearFieldErrors(error: unknown, campos: Record<string, true>): Record<string, string> {
  const mapped: Record<string, string> = {};
  if (error instanceof ApiError) {
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      if (key in campos) mapped[key] = messages[0];
    }
  }
  return mapped;
}
