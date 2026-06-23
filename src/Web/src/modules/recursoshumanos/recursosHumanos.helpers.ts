// Helpers de apresentação compartilhados pelas telas de RecursosHumanos
// (espelha src/modules/tributos/dividaAtiva.helpers.ts).
import type { TagVariant, SelectOption } from '../../components/ui';

/** Permissão de LEITURA do módulo (gating de UI; backend é a fonte da verdade). */
export const PERM_RH_VER = 'recursoshumanos.ver';

/** Permissão de AÇÃO (criar/editar/comandos) do módulo. */
export const PERM_RH_GERENCIAR = 'recursoshumanos.gerenciar';

/**
 * Permissão de AUTOSSERVIÇO (área "Minha Folha"): o servidor acessa SOMENTE os
 * próprios dados (dado-próprio resolvido do JWT no backend — a UI nunca envia servidorId).
 */
export const PERM_RH_AUTOSSERVICO = 'autosservico.proprio';

/** Opções de tipo de folha do meu contracheque (rótulo PT-BR → enum TipoFolha do backend). */
export const TIPOS_FOLHA_MINHA: SelectOption[] = [
  { value: 'Mensal', label: 'Mensal' },
  { value: 'DecimoTerceiro', label: '13º salário' },
  { value: 'Ferias', label: 'Férias' },
  { value: 'Rescisao', label: 'Rescisão' },
];

/**
 * Formata o regime previdenciário para exibição em siglas oficiais MAIÚSCULAS.
 * O backend serializa o enum como PascalCase ('Rpps'/'Rgps'), que renderizado cru
 * aparece "minúsculo" (Rgps/Rpps) — aqui normalizamos para RGPS/RPPS (EC 103/2019).
 */
export function formatarRegimePrev(regime: string): string {
  switch (regime) {
    case 'Rpps':
    case 'RPPS':
      return 'RPPS';
    case 'Rgps':
    case 'RGPS':
      return 'RGPS';
    default:
      return regime.toUpperCase();
  }
}

/**
 * Opções de SITUAÇÃO do servidor para o filtro da busca. O valor enviado é o NOME do
 * enum `SituacaoServidor` do backend (bind por nome a partir da query string), não índice.
 */
export const SITUACOES_SERVIDOR_FILTRO: SelectOption[] = [
  { value: 'Nomeado', label: 'Nomeado' },
  { value: 'Empossado', label: 'Empossado' },
  { value: 'EmExercicio', label: 'Em exercício' },
  { value: 'Estavel', label: 'Estável' },
  { value: 'Afastado', label: 'Afastado' },
  { value: 'Desligado', label: 'Desligado' },
];

/**
 * Opções de REGIME previdenciário para o filtro da busca. O valor enviado é o NOME do
 * enum `RegimePrevidenciario` do backend ('Rpps'/'Rgps'), não o índice numérico.
 */
export const REGIMES_SERVIDOR_FILTRO: SelectOption[] = [
  { value: 'Rpps', label: 'RPPS (Regime Próprio)' },
  { value: 'Rgps', label: 'RGPS (Regime Geral)' },
];

/** Formata o tipo de folha (enum PascalCase do backend) para rótulo PT-BR amigável. */
export function formatarTipoFolha(tipo: string): string {
  switch (tipo) {
    case 'Mensal':
      return 'Mensal';
    case 'DecimoTerceiro':
      return '13º salário';
    case 'Ferias':
      return 'Férias';
    case 'Rescisao':
      return 'Rescisão';
    default:
      return tipo;
  }
}

/** Mapeia a situação do servidor para a variante semântica da Tag (cor + texto). */
export function situacaoServidorTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Estavel':
    case 'EmExercicio':
      return 'success';
    case 'Empossado':
      return 'info';
    case 'Nomeado':
      return 'warning';
    case 'Afastado':
      return 'info';
    case 'Desligado':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação do cargo para a variante semântica da Tag. */
export function situacaoCargoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Ativo':
      return 'success';
    case 'Vago':
      return 'warning';
    case 'Extinto':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação da folha para a variante semântica da Tag. */
export function situacaoFolhaTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Aberta':
      return 'warning';
    case 'Calculada':
      return 'info';
    case 'Fechada':
      return 'info';
    case 'Paga':
      return 'success';
    default:
      return 'default';
  }
}

/** Opções de tipo de cargo (rótulo PT-BR → enum numérico do backend). */
export const TIPOS_CARGO: SelectOption[] = [
  { value: '1', label: 'Efetivo' },
  { value: '2', label: 'Comissionado' },
  { value: '3', label: 'Temporário' },
];

/** Opções de regime previdenciário (rótulo → enum numérico do backend). */
export const REGIMES_PREVIDENCIARIOS: SelectOption[] = [
  { value: '1', label: 'RPPS (Regime Próprio)' },
  { value: '2', label: 'RGPS (Regime Geral)' },
];

/** Opções de tipo de evento da folha (rótulo → enum numérico do backend). */
export const TIPOS_EVENTO: SelectOption[] = [
  { value: '1', label: 'Provento' },
  { value: '2', label: 'Desconto' },
];

/** Opções de natureza de rubrica (rótulo → enum numérico do backend, S-1010). */
export const NATUREZAS_RUBRICA: SelectOption[] = [
  { value: '1', label: 'Provento' },
  { value: '2', label: 'Desconto' },
  { value: '3', label: 'Informativa' },
  { value: '4', label: 'Informativa dedutora' },
];

/** Mapeia a natureza da rubrica para a variante semântica da Tag. */
export function naturezaRubricaTagVariant(natureza: string): TagVariant {
  switch (natureza) {
    case 'Provento':
      return 'success';
    case 'Desconto':
      return 'danger';
    case 'Informativa':
    case 'InformativaDedutora':
      return 'info';
    default:
      return 'default';
  }
}

/** Formata uma fração decimal (ex.: 0,14) como percentual PT-BR (ex.: "14%"). */
export function formatarAliquota(fracao: number): string {
  return fracao.toLocaleString('pt-BR', {
    style: 'percent',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  });
}

/** Opções de regime da jornada de ponto (rótulo → enum numérico do backend). */
export const REGIMES_JORNADA: SelectOption[] = [
  { value: '1', label: 'Estatutário' },
  { value: '2', label: 'Celetista' },
];

/** Opções de sentido da marcação de ponto (rótulo → enum numérico do backend). */
export const SENTIDOS_MARCACAO: SelectOption[] = [
  { value: '1', label: 'Entrada' },
  { value: '2', label: 'Saída' },
];

/** Opções de origem (tipo de REP) da marcação (rótulo → enum numérico do backend). */
export const ORIGENS_REP: SelectOption[] = [
  { value: '1', label: 'REP-C (convencional)' },
  { value: '2', label: 'REP-A (alternativo)' },
  { value: '3', label: 'REP-P (programa)' },
];

/**
 * Formata uma duração em minutos como "HHh MMmin" (com sinal para saldos negativos do
 * banco de horas). Ex.: 510 → "8h 30min"; -45 → "-0h 45min".
 */
export function formatarMinutos(minutos: number): string {
  const sinal = minutos < 0 ? '-' : '';
  const abs = Math.abs(minutos);
  const horas = Math.floor(abs / 60);
  const min = abs % 60;
  return `${sinal}${horas}h ${String(min).padStart(2, '0')}min`;
}

/** Opções da parcela do 13º salário (rótulo → enum numérico do backend). */
export const PARCELAS_13: SelectOption[] = [
  { value: '1', label: '1ª parcela — adiantamento (sem descontos)' },
  { value: '2', label: '2ª parcela — integral (com INSS/RPPS/IRRF)' },
];

/** Opções de regime jurídico do vínculo na rescisão (RegimeVinculo, backend). */
export const REGIMES_VINCULO: SelectOption[] = [
  { value: '1', label: 'Estatutário' },
  { value: '2', label: 'Empregado público (Celetista)' },
];

/**
 * Opções de tipo de desligamento (TipoDesligamento, backend). A matriz parametrizável
 * (tipo × regime) é quem decide as verbas devidas — aqui apenas o rótulo PT-BR.
 */
export const TIPOS_DESLIGAMENTO: SelectOption[] = [
  { value: '1', label: 'Dispensa sem justa causa' },
  { value: '2', label: 'Pedido de demissão / exoneração a pedido' },
  { value: '3', label: 'Justa causa' },
  { value: '4', label: 'Distrato (acordo CLT 484-A)' },
  { value: '5', label: 'Aposentadoria' },
  { value: '6', label: 'Falecimento' },
  { value: '7', label: 'Exoneração / vacância (estatutário)' },
];

/** Indica se o tipo de desligamento/regime habilita verbas celetistas (aviso + multa FGTS). */
export function permiteVerbasCeletistas(regimeVinculo: string): boolean {
  return regimeVinculo === '2';
}

/** Opções de mês (1–12) para seletor de competência. */
export const MESES: SelectOption[] = [
  { value: '1', label: 'Janeiro' },
  { value: '2', label: 'Fevereiro' },
  { value: '3', label: 'Março' },
  { value: '4', label: 'Abril' },
  { value: '5', label: 'Maio' },
  { value: '6', label: 'Junho' },
  { value: '7', label: 'Julho' },
  { value: '8', label: 'Agosto' },
  { value: '9', label: 'Setembro' },
  { value: '10', label: 'Outubro' },
  { value: '11', label: 'Novembro' },
  { value: '12', label: 'Dezembro' },
];
