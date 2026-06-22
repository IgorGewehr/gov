// Helpers de apresentação compartilhados pelas telas do agregado Processo.
import type { TagVariant } from '../../../components/ui';
import type { NivelDeAcesso, SituacaoProcesso } from './processo.api';

/** Rótulo legível em PT-BR para a situação do processo. */
export const SITUACAO_LABEL: Record<SituacaoProcesso, string> = {
  Autuado: 'Autuado',
  EmTramitacao: 'Em tramitação',
  Sobrestado: 'Sobrestado',
  Arquivado: 'Arquivado',
};

/** Rótulo legível em PT-BR para o nível de acesso. */
export const NIVEL_ACESSO_LABEL: Record<NivelDeAcesso, string> = {
  Publico: 'Público',
  Restrito: 'Restrito',
  Sigiloso: 'Sigiloso',
};

/** Mapeia a situação do processo para a variante semântica da Tag (cor + texto). */
export function situacaoTagVariant(situacao: SituacaoProcesso): TagVariant {
  switch (situacao) {
    case 'Autuado':
      return 'info';
    case 'EmTramitacao':
      return 'warning';
    case 'Sobrestado':
      return 'default';
    case 'Arquivado':
      return 'success';
    default:
      return 'default';
  }
}

/** Mapeia o nível de acesso para a variante da Tag (sigiloso = perigo/atenção). */
export function nivelAcessoTagVariant(nivel: NivelDeAcesso): TagVariant {
  switch (nivel) {
    case 'Publico':
      return 'success';
    case 'Restrito':
      return 'warning';
    case 'Sigiloso':
      return 'danger';
    default:
      return 'default';
  }
}

// --- Guardas de máquina de estados (espelham as guardas do domínio) ---

/** Conjunto "em andamento" = { Autuado, EmTramitacao } — admite tramitar/despachar/sobrestar. */
export function emAndamento(situacao: SituacaoProcesso): boolean {
  return situacao === 'Autuado' || situacao === 'EmTramitacao';
}

/** Estado terminal = { Arquivado }. */
export function arquivado(situacao: SituacaoProcesso): boolean {
  return situacao === 'Arquivado';
}

/** Sobrestado — não tramita até reativação. */
export function sobrestado(situacao: SituacaoProcesso): boolean {
  return situacao === 'Sobrestado';
}
