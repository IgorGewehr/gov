// Helpers de apresentação compartilhados pelas telas de Inventario (Lei 4.320 art. 96).
import type { SelectOption, TagVariant } from '../../../components/ui';
import type {
  RecomendacaoDivergenciaNome,
  SituacaoEncontradaNome,
  SituacaoInventarioNome,
  TipoDivergenciaNome,
  TipoInventarioNome,
} from './inventario.api';
import { SITUACAO_ENCONTRADA, SITUACAO_INVENTARIO, TIPO_INVENTARIO } from './inventario.api';

/** Rótulo PT-BR do tipo do inventário. */
export function tipoInventarioLabel(tipo: TipoInventarioNome): string {
  switch (tipo) {
    case 'Anual':
      return 'Anual';
    case 'PorSetor':
      return 'Por setor';
    case 'Eventual':
      return 'Eventual';
    case 'Transferencia':
      return 'Transferência';
    default:
      return tipo;
  }
}

/** Rótulo PT-BR da situação do inventário. */
export function situacaoInventarioLabel(situacao: SituacaoInventarioNome): string {
  switch (situacao) {
    case 'EmAbertura':
      return 'Em abertura';
    case 'EmContagem':
      return 'Em contagem';
    case 'EmConciliacao':
      return 'Em conciliação';
    case 'Encerrado':
      return 'Encerrado';
    case 'Cancelado':
      return 'Cancelado';
    default:
      return situacao;
  }
}

/** Variante de Tag por situação do inventário. */
export function situacaoInventarioTagVariant(situacao: SituacaoInventarioNome): TagVariant {
  switch (situacao) {
    case 'EmAbertura':
      return 'info';
    case 'EmContagem':
      return 'warning';
    case 'EmConciliacao':
      return 'warning';
    case 'Encerrado':
      return 'success';
    case 'Cancelado':
      return 'danger';
    default:
      return 'default';
  }
}

/** Rótulo PT-BR da situação física encontrada na contagem. */
export function situacaoEncontradaLabel(situacao: SituacaoEncontradaNome): string {
  switch (situacao) {
    case 'Localizado':
      return 'Localizado';
    case 'NaoLocalizado':
      return 'Não localizado';
    case 'LocalizadoOutroSetor':
      return 'Localizado em outro setor';
    case 'Inservivel':
      return 'Inservível';
    default:
      return situacao;
  }
}

/** Variante de Tag por situação encontrada. */
export function situacaoEncontradaTagVariant(situacao: SituacaoEncontradaNome): TagVariant {
  switch (situacao) {
    case 'Localizado':
      return 'success';
    case 'NaoLocalizado':
      return 'danger';
    case 'LocalizadoOutroSetor':
      return 'warning';
    case 'Inservivel':
      return 'warning';
    default:
      return 'default';
  }
}

/** Rótulo PT-BR do tipo de divergência apurada. */
export function tipoDivergenciaLabel(tipo: TipoDivergenciaNome): string {
  switch (tipo) {
    case 'Falta':
      return 'Falta';
    case 'Sobra':
      return 'Sobra';
    case 'DivergenciaLocalizacao':
      return 'Divergência de localização';
    case 'DivergenciaEstado':
      return 'Divergência de estado';
    case 'DivergenciaValor':
      return 'Divergência de valor';
    default:
      return tipo;
  }
}

/** Variante de Tag por tipo de divergência (Falta = crítica). */
export function tipoDivergenciaTagVariant(tipo: TipoDivergenciaNome): TagVariant {
  switch (tipo) {
    case 'Falta':
      return 'danger';
    case 'Sobra':
      return 'info';
    case 'DivergenciaLocalizacao':
      return 'warning';
    case 'DivergenciaEstado':
      return 'warning';
    case 'DivergenciaValor':
      return 'warning';
    default:
      return 'default';
  }
}

/** Rótulo PT-BR da recomendação de efetivação. */
export function recomendacaoLabel(recomendacao: RecomendacaoDivergenciaNome): string {
  switch (recomendacao) {
    case 'Baixa':
      return 'Baixa';
    case 'Transferencia':
      return 'Transferência';
    case 'Incorporacao':
      return 'Incorporação';
    case 'Reavaliacao':
      return 'Reavaliação';
    default:
      return recomendacao;
  }
}

/** Opções de filtro/Select por tipo de inventário (value = valor numérico do enum). */
export const OPCOES_TIPO_INVENTARIO: SelectOption[] = [
  { value: String(TIPO_INVENTARIO.Anual), label: 'Anual' },
  { value: String(TIPO_INVENTARIO.PorSetor), label: 'Por setor' },
  { value: String(TIPO_INVENTARIO.Eventual), label: 'Eventual' },
  { value: String(TIPO_INVENTARIO.Transferencia), label: 'Transferência' },
];

/** Opções de filtro por situação do inventário (value = valor numérico do enum). */
export const OPCOES_SITUACAO_INVENTARIO: SelectOption[] = [
  { value: String(SITUACAO_INVENTARIO.EmAbertura), label: 'Em abertura' },
  { value: String(SITUACAO_INVENTARIO.EmContagem), label: 'Em contagem' },
  { value: String(SITUACAO_INVENTARIO.EmConciliacao), label: 'Em conciliação' },
  { value: String(SITUACAO_INVENTARIO.Encerrado), label: 'Encerrado' },
  { value: String(SITUACAO_INVENTARIO.Cancelado), label: 'Cancelado' },
];

/** Opções de Select da situação física encontrada (value = valor numérico do enum). */
export const OPCOES_SITUACAO_ENCONTRADA: SelectOption[] = [
  { value: String(SITUACAO_ENCONTRADA.Localizado), label: 'Localizado' },
  { value: String(SITUACAO_ENCONTRADA.NaoLocalizado), label: 'Não localizado' },
  { value: String(SITUACAO_ENCONTRADA.LocalizadoOutroSetor), label: 'Localizado em outro setor' },
  { value: String(SITUACAO_ENCONTRADA.Inservivel), label: 'Inservível' },
];

/** Data de hoje em formato ISO yyyy-mm-dd (default de inputs date). */
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
