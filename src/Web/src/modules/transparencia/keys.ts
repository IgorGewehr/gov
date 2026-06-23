// Query keys do módulo Transparência (fonte única para invalidação consistente).
import type { ListarRemessasParams } from './remessa.api';
import type { ListarDeclaracoesParams } from './declaracao-fiscal.api';

export const transparenciaKeys = {
  all: ['transparencia'] as const,
  remessas: () => [...transparenciaKeys.all, 'remessas-tce'] as const,
  remessasLista: (params: ListarRemessasParams) =>
    [...transparenciaKeys.remessas(), 'lista', params] as const,
  remessa: (id: string) => [...transparenciaKeys.remessas(), 'detalhe', id] as const,
  remessaCriticas: (id: string) => [...transparenciaKeys.remessas(), 'criticas', id] as const,
  declaracoes: () => [...transparenciaKeys.all, 'declaracoes-fiscais'] as const,
  declaracoesLista: (params: ListarDeclaracoesParams) =>
    [...transparenciaKeys.declaracoes(), 'lista', params] as const,
  declaracao: (id: string) => [...transparenciaKeys.declaracoes(), 'detalhe', id] as const,
  fiscal: () => [...transparenciaKeys.all, 'fiscal'] as const,
  minimos: (exercicio: number) => [...transparenciaKeys.fiscal(), 'minimos', exercicio] as const,
  asps: (exercicio: number) => [...transparenciaKeys.fiscal(), 'asps', exercicio] as const,
};

/** Resposta dos endpoints de criação (POST) — `{ id }`. */
export interface CriacaoResponse {
  id: string;
}
