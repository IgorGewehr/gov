// Internos compartilhados pelos modais de ação da DetailPage de Veiculo.
// Extraído de VeiculoAcaoModais.tsx — comportamento idêntico.
import { ApiError } from '../../../../api/problemDetails';

export interface AcaoModalBaseProps {
  veiculoId: string;
  open: boolean;
  onClose: () => void;
  /** Odômetro/horímetro atuais do veículo (para validar monotonia I-3/I-4). */
  odometroAtual: number;
  horimetroAtual: number;
}

export function aplicarErrosBackend(
  error: unknown,
  campos: readonly string[],
  setErros: (e: Record<string, string>) => void,
): string {
  if (error instanceof ApiError) {
    const mapped: Record<string, string> = {};
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      if (campos.includes(key) && messages.length > 0) mapped[key] = messages[0];
    }
    if (Object.keys(mapped).length > 0) setErros(mapped);
    return error.userMessage;
  }
  return 'Não foi possível concluir a operação.';
}
