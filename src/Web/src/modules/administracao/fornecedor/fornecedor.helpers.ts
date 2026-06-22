// Helpers de apresentacao compartilhados pelas telas de Fornecedor.
import type { TagVariant } from '../../../components/ui';
import type { SituacaoFornecedor, TipoSancao } from './fornecedor.api';
import { TIPOS_IMPEDITIVOS } from './fornecedor.api';

/** Mapeia a situacao cadastral para a variante semantica da Tag. */
export function situacaoTagVariant(situacao: SituacaoFornecedor): TagVariant {
  switch (situacao) {
    case 'Ativo':
      return 'success';
    case 'Sancionado':
      return 'danger';
    case 'Inativo':
      return 'default';
    default:
      return 'default';
  }
}

/** Mapeia o tipo de sancao para a variante da Tag (impeditiva = danger). */
export function tipoSancaoTagVariant(tipo: TipoSancao): TagVariant {
  if (TIPOS_IMPEDITIVOS.has(tipo)) return 'danger';
  return tipo === 'Multa' ? 'warning' : 'info';
}

/** Data de hoje em ISO yyyy-mm-dd (referencia padrao para consultas/sancoes). */
export function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}
