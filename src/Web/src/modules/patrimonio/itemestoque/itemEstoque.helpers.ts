// Helpers de apresentação compartilhados pelas telas de ItemEstoque (Almoxarifado).
import type { SelectOption, TagVariant } from '../../../components/ui';
import type {
  ClassificacaoAbcNome,
  MetodoCusteioNome,
  SituacaoItemEstoque,
  TipoMovimento,
} from './itemestoque.api';
import { CLASSIFICACAO_ABC, METODO_CUSTEIO } from './itemestoque.api';

/** Mapeia a situação do item para a variante semântica da Tag (cor + texto). */
export function situacaoTagVariant(situacao: SituacaoItemEstoque): TagVariant {
  return situacao === 'Ativo' ? 'success' : 'default';
}

/** Mapeia o tipo de movimento para a variante semântica da Tag. */
export function tipoMovimentoTagVariant(tipo: TipoMovimento): TagVariant {
  return tipo === 'Entrada' ? 'info' : 'warning';
}

/** Rótulo PT-BR do tipo de movimento. */
export function tipoMovimentoLabel(tipo: TipoMovimento): string {
  return tipo === 'Entrada' ? 'Entrada' : 'Saída';
}

/** Rótulo PT-BR do método de custeio. */
export function metodoCusteioLabel(metodo: MetodoCusteioNome): string {
  return metodo === 'Peps' ? 'PEPS (primeiro a entrar, primeiro a sair)' : 'Custo médio ponderado';
}

/** Rótulo PT-BR da classe ABC. */
export function classificacaoAbcLabel(classe: ClassificacaoAbcNome): string {
  switch (classe) {
    case 'A':
      return 'Classe A — alta relevância';
    case 'B':
      return 'Classe B — relevância média';
    case 'C':
      return 'Classe C — baixa relevância';
    default:
      return classe;
  }
}

/** Variante de Tag por classe ABC (A = mais crítica). */
export function classificacaoAbcTagVariant(classe: ClassificacaoAbcNome): TagVariant {
  switch (classe) {
    case 'A':
      return 'danger';
    case 'B':
      return 'warning';
    case 'C':
      return 'info';
    default:
      return 'default';
  }
}

/** Opções de filtro por situação do item (value = nome do enum no contrato). */
export const OPCOES_SITUACAO_ITEM: SelectOption[] = [
  { value: 'Ativo', label: 'Ativo' },
  { value: 'Inativo', label: 'Inativo' },
];

/** Opções de Select do método de custeio (value = valor numérico do enum). */
export const OPCOES_METODO_CUSTEIO: SelectOption[] = [
  { value: String(METODO_CUSTEIO.Peps), label: 'PEPS (FIFO)' },
  { value: String(METODO_CUSTEIO.Medio), label: 'Custo médio' },
];

/** Opções de Select da classe ABC (value = valor numérico do enum). */
export const OPCOES_CLASSIFICACAO_ABC: SelectOption[] = [
  { value: String(CLASSIFICACAO_ABC.A), label: 'A — alta relevância' },
  { value: String(CLASSIFICACAO_ABC.B), label: 'B — relevância média' },
  { value: String(CLASSIFICACAO_ABC.C), label: 'C — baixa relevância' },
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
