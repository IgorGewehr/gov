// Helpers de apresentação, validação e achatamento da árvore de UOs.
import type { TagVariant } from '../../../components/ui';
import type { NoUnidade, TipoUnidade } from './unidades.api';

/** Catálogo de tipos de UO para selects (valor = NOME aceito pelo backend). */
export const TIPOS_UNIDADE: { value: TipoUnidade; label: string }[] = [
  { value: 'Secretaria', label: 'Secretaria' },
  { value: 'Departamento', label: 'Departamento' },
  { value: 'Setor', label: 'Setor' },
  { value: 'Gabinete', label: 'Gabinete' },
  { value: 'Fundo', label: 'Fundo' },
];

/** Rótulo legível do tipo de UO. */
export function tipoLabel(tipo: TipoUnidade): string {
  return TIPOS_UNIDADE.find((t) => t.value === tipo)?.label ?? tipo;
}

/** Variante da Tag para o status ativa/inativa da UO. */
export function ativaTagVariant(ativa: boolean): TagVariant {
  return ativa ? 'success' : 'default';
}

/** Rótulo legível para o status ativa/inativa. */
export function ativaLabel(ativa: boolean): string {
  return ativa ? 'Ativa' : 'Inativa';
}

/** Linha achatada da árvore, com o nível (profundidade) para indentação visual. */
export interface UnidadeLinha {
  unidade: NoUnidade;
  nivel: number;
}

/**
 * Achata a árvore de UOs em uma lista linear (pré-ordem: pai antes dos filhos),
 * anotando o nível de cada nó para indentação. Mantém a ordem já fornecida pela
 * API (por código, recursivamente).
 */
export function achatarArvore(raizes: readonly NoUnidade[], nivel = 0): UnidadeLinha[] {
  const linhas: UnidadeLinha[] = [];
  for (const unidade of raizes) {
    linhas.push({ unidade, nivel });
    if (unidade.filhos.length > 0) {
      linhas.push(...achatarArvore(unidade.filhos, nivel + 1));
    }
  }
  return linhas;
}

/**
 * Coleta os IDs de uma subárvore (a própria UO + todos os descendentes). Útil
 * para impedir mover uma UO para dentro dela mesma (ciclo).
 */
export function idsDaSubarvore(raiz: NoUnidade): Set<string> {
  const ids = new Set<string>();
  const pilha: NoUnidade[] = [raiz];
  while (pilha.length > 0) {
    const atual = pilha.pop()!;
    ids.add(atual.id);
    pilha.push(...atual.filhos);
  }
  return ids;
}
