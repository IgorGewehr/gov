// Helpers de apresentação dos RELATÓRIOS gerenciais da folha (Onda 3a).
// Reaproveita formatadores compartilhados (formatarRegimePrev) e a tag de situação
// de cargo já existente em recursosHumanos.helpers.
import type { TagVariant } from '../../components/ui';

/** Abas internas da área de relatórios. */
export type AbaRelatorio = 'folha-secretaria' | 'evolucao' | 'mapa-cargos' | 'demonstrativo-tce';

/** Definição de uma aba (rótulo + ícone). */
export interface AbaRelatorioDef {
  id: AbaRelatorio;
  label: string;
  icon: string;
}

export const ABAS_RELATORIO: ReadonlyArray<AbaRelatorioDef> = [
  { id: 'folha-secretaria', label: 'Folha por secretaria', icon: 'fas fa-building-columns' },
  { id: 'evolucao', label: 'Evolução da despesa', icon: 'fas fa-chart-line' },
  { id: 'mapa-cargos', label: 'Mapa de cargos', icon: 'fas fa-sitemap' },
  { id: 'demonstrativo-tce', label: 'Demonstrativo TCE', icon: 'fas fa-landmark' },
];

/**
 * Formata a fonte (regime previdenciário) para sigla oficial MAIÚSCULA. O backend
 * já rotula como 'RPPS'/'RGPS', mas normalizamos para tolerar PascalCase.
 */
export function formatarFonte(fonte: string): string {
  const f = fonte.trim().toUpperCase();
  if (f === 'RPPS' || f === 'RGPS') return f;
  return fonte;
}

/** Formata uma competência AAAA-MM para MM/AAAA (rótulo PT-BR amigável). */
export function formatarCompetencia(competencia: string): string {
  const partes = competencia.split('-');
  if (partes.length === 2) return `${partes[1]}/${partes[0]}`;
  return competencia;
}

/** Mapeia a situação da folha mensal para a variante semântica da Tag. */
export function situacaoFolhaRelTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Aberta':
      return 'warning';
    case 'Calculada':
    case 'Fechada':
      return 'info';
    case 'Paga':
      return 'success';
    default:
      return 'default';
  }
}
