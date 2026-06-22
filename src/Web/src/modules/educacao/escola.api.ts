// API do agregado Escola (módulo Educação). DTOs + acesso HTTP + hooks TanStack
// Query. Espelha os endpoints reais sob /api/educacao/escolas (EducacaoEndpoints.cs):
//   GET    /educacao/escolas                       -> ListarEscolasDaRede  -> EscolaResumo[]
//   GET    /educacao/escolas/{codigoInep}          -> ObterEscolaPorInep   -> EscolaResumo
//   POST   /educacao/escolas                       -> CredenciarEscola     -> { id }
//   PUT    /educacao/escolas/{escolaId}/dados-censo -> AtualizarDadosCenso -> 204
//   POST   /educacao/escolas/{escolaId}/desativacao -> DesativarEscola     -> 204
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';
import type { CriadoResponse } from './educacao.keys';

/** Dependência administrativa da escola (Domain.Escolas.DependenciaAdministrativa). */
export type DependenciaAdministrativa = 'Federal' | 'Estadual' | 'Municipal' | 'Privada';

/** Situação da escola (Domain.Escolas.SituacaoEscola). */
export type SituacaoEscola = 'EmCadastro' | 'Credenciada' | 'Desativada';

/** Projeção EscolaResumo (.../Application/Escolas). */
export interface EscolaResumo {
  id: string;
  codigoInep: string;
  nome: string;
  dependencia: string;
  situacao: string;
}

/** Endereço/georreferenciamento (Domain.ValueObjects.Endereco). */
export interface Endereco {
  logradouro: string;
  municipio: string;
  uf: string;
  cep: string;
  latitude: number;
  longitude: number;
}

/** Infraestrutura física/acessibilidade (Domain.ValueObjects.Infraestrutura). */
export interface Infraestrutura {
  numeroSalas: number;
  numeroDependencias: number;
  possuiAcessibilidade: boolean;
}

/** Corpo de POST /escolas (CredenciarEscolaCommand). */
export interface CredenciarEscolaInput {
  codigoInep: string;
  nome: string;
  dependencia: DependenciaAdministrativa;
  endereco: Endereco;
  infraestrutura: Infraestrutura;
}

/** Corpo de PUT /escolas/{escolaId}/dados-censo (AtualizarDadosCensoPayload). */
export interface AtualizarDadosCensoInput {
  endereco: Endereco;
  infraestrutura: Infraestrutura;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function listarEscolas(signal?: AbortSignal): Promise<EscolaResumo[]> {
  return http.get<EscolaResumo[]>('/educacao/escolas', { signal });
}

function obterEscolaPorInep(codigoInep: string, signal?: AbortSignal): Promise<EscolaResumo> {
  return http.get<EscolaResumo>(`/educacao/escolas/${encodeURIComponent(codigoInep)}`, { signal });
}

function credenciarEscola(input: CredenciarEscolaInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/escolas', input);
}

function atualizarDadosCenso(escolaId: string, input: AtualizarDadosCensoInput): Promise<void> {
  return http.put<void>(`/educacao/escolas/${escolaId}/dados-censo`, input);
}

function desativarEscola(escolaId: string): Promise<void> {
  return http.post<void>(`/educacao/escolas/${escolaId}/desativacao`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** Lista as escolas da rede (escopo do tenant atual). */
export function useEscolasDaRede() {
  return useQuery({
    queryKey: educacaoKeys.escolas(),
    queryFn: ({ signal }) => listarEscolas(signal),
  });
}

/** Obtém o resumo de uma escola por código INEP. `enabled` controla disparo sob demanda. */
export function useEscolaPorInep(codigoInep: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.escolaPorInep(codigoInep),
    queryFn: ({ signal }) => obterEscolaPorInep(codigoInep, signal),
    enabled: enabled && codigoInep.trim().length > 0,
  });
}

/** Credencia uma nova escola e invalida a listagem da rede. */
export function useCredenciarEscola() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: credenciarEscola,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.escolas() });
    },
  });
}

/** Atualiza os dados do EducaCenso (endereço/infraestrutura) de uma escola. */
export function useAtualizarDadosCenso(codigoInep?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ escolaId, input }: { escolaId: string; input: AtualizarDadosCensoInput }) =>
      atualizarDadosCenso(escolaId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.escolas() });
      if (codigoInep) {
        queryClient.invalidateQueries({ queryKey: educacaoKeys.escolaPorInep(codigoInep) });
      }
    },
  });
}

/** Desativa uma escola (estado terminal) e invalida a listagem/detalhe. */
export function useDesativarEscola(codigoInep?: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (escolaId: string) => desativarEscola(escolaId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.escolas() });
      if (codigoInep) {
        queryClient.invalidateQueries({ queryKey: educacaoKeys.escolaPorInep(codigoInep) });
      }
    },
  });
}
