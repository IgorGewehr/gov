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

  rubricas: () => [...rhKeys.all, 'rubricas'] as const,
  rubricasVigentes: (ano: number, mes: number) =>
    [...rhKeys.rubricas(), 'vigentes', ano, mes] as const,

  folhas: () => [...rhKeys.all, 'folhas'] as const,
  folhaPorCompetencia: (ano: number, mes: number) =>
    [...rhKeys.folhas(), 'competencia', ano, mes] as const,
  contracheque: (folhaId: string, servidorId: string) =>
    [...rhKeys.folhas(), folhaId, 'contracheque', servidorId] as const,
  conferencia: (folhaId: string) => [...rhKeys.folhas(), folhaId, 'conferencia'] as const,

  esocial: () => [...rhKeys.all, 'esocial'] as const,
  esocialEventos: () => [...rhKeys.esocial(), 'eventos'] as const,

  minhaFolha: () => [...rhKeys.all, 'minha-folha'] as const,
  meuContracheque: (ano: number, mes: number, tipo: string) =>
    [...rhKeys.minhaFolha(), 'contracheque', ano, mes, tipo] as const,
  meuEspelhoPonto: (ano: number, mes: number) =>
    [...rhKeys.minhaFolha(), 'espelho-ponto', ano, mes] as const,
  minhasFerias: (ano: number) => [...rhKeys.minhaFolha(), 'ferias', ano] as const,
  meuInformeRendimentos: (ano: number) =>
    [...rhKeys.minhaFolha(), 'informe-rendimentos', ano] as const,

  ponto: () => [...rhKeys.all, 'ponto'] as const,
  jornadaVigente: (servidorId: string, ano: number, mes: number) =>
    [...rhKeys.ponto(), 'jornada', servidorId, ano, mes] as const,
  marcacoes: (servidorId: string, ano: number, mes: number) =>
    [...rhKeys.ponto(), 'marcacoes', servidorId, ano, mes] as const,
  apuracao: (servidorId: string, ano: number, mes: number) =>
    [...rhKeys.ponto(), 'apuracao', servidorId, ano, mes] as const,
};
