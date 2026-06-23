// Helpers de apresentação das REQUISIÇÕES de almoxarifado (Onda 3b).
import type { SelectOption, TagVariant } from '../../../components/ui';
import type { SituacaoPedidoNome } from './requisicao.api';
import { SITUACAO_PEDIDO } from './requisicao.api';

/** Variante semântica da Tag por situação do pedido. */
export function situacaoPedidoTagVariant(situacao: SituacaoPedidoNome): TagVariant {
  switch (situacao) {
    case 'Solicitado':
      return 'warning';
    case 'Aprovado':
      return 'info';
    case 'Atendido':
      return 'success';
    case 'Cancelado':
      return 'danger';
    default:
      return 'default';
  }
}

/** Opções de filtro por situação (value = valor numérico do enum). */
export const OPCOES_SITUACAO_PEDIDO: SelectOption[] = [
  { value: String(SITUACAO_PEDIDO.Solicitado), label: 'Solicitado' },
  { value: String(SITUACAO_PEDIDO.Aprovado), label: 'Aprovado' },
  { value: String(SITUACAO_PEDIDO.Atendido), label: 'Atendido' },
  { value: String(SITUACAO_PEDIDO.Cancelado), label: 'Cancelado' },
];

/** Data de hoje em ISO yyyy-mm-dd (default de inputs date). */
export function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/**
 * Mapeia ProblemDetails.errors (PascalCase do backend) para chaves camelCase do form.
 * Retorna apenas as chaves presentes em `campos`.
 */
export function mapearFieldErrors<TKeys extends string>(
  fieldErrors: Record<string, string[]>,
  campos: readonly TKeys[],
): Partial<Record<TKeys, string>> {
  const mapped: Partial<Record<TKeys, string>> = {};
  for (const [field, messages] of Object.entries(fieldErrors)) {
    const key = (field.charAt(0).toLowerCase() + field.slice(1)) as TKeys;
    if (campos.includes(key) && messages.length > 0) {
      mapped[key] = messages[0];
    }
  }
  return mapped;
}
