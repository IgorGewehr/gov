// Query keys centralizadas do módulo RecursosHumanos (fonte única para invalidação)
// e tipos comuns compartilhados pelas camadas de API por entidade. Mantém a
// invalidação consistente entre servidor.api / cargo.api / folha.api.
import type { TipoCargo } from './cargo.api';

/** Resposta dos endpoints de criação (Minimal API retorna `{ id }`). */
export interface CriacaoResponse {
  id: string;
}

export const rhKeys = {
  all: ['recursoshumanos'] as const,

  servidores: () => [...rhKeys.all, 'servidores'] as const,
  servidoresAtivos: () => [...rhKeys.servidores(), 'ativos'] as const,
  servidorPorMatricula: (matricula: string) =>
    [...rhKeys.servidores(), 'por-matricula', matricula] as const,

  cargos: () => [...rhKeys.all, 'cargos'] as const,
  cargosComVagas: (tipo: TipoCargo | null) =>
    [...rhKeys.cargos(), 'com-vagas', tipo ?? 'todos'] as const,
  cargo: (id: string) => [...rhKeys.cargos(), 'detalhe', id] as const,

  folhas: () => [...rhKeys.all, 'folhas'] as const,
  folhaPorCompetencia: (ano: number, mes: number) =>
    [...rhKeys.folhas(), 'competencia', ano, mes] as const,
  contracheque: (folhaId: string, servidorId: string) =>
    [...rhKeys.folhas(), folhaId, 'contracheque', servidorId] as const,
};
