// Camada de API do agregado Estabelecimento (CNES) — módulo Saúde. DTOs + acesso HTTP +
// hooks TanStack Query. Contrato REAL (SaudeEndpoints.cs):
//   POST /saude/estabelecimentos                          -> CadastrarEstabelecimento -> { id }   (saude.gerenciar)
//   GET  /saude/estabelecimentos?termo&tipo&situacao&pagina&tamanho -> ResultadoPaginado<EstabelecimentoItemLista> (saude.ver)
//   GET  /saude/estabelecimentos/{id}                     -> EstabelecimentoDetalhe              (saude.ver)
//   PUT  /saude/estabelecimentos/{id}                     -> AtualizarEstabelecimento -> 204      (saude.gerenciar)
//   POST /saude/estabelecimentos/{id}/inativacao          -> 204                                  (saude.gerenciar)
//   POST /saude/estabelecimentos/{id}/reativacao          -> 204                                  (saude.gerenciar)
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';
import type { ResultadoPaginado } from './paciente.api';

// --- DTOs ---

/** Item da lista de estabelecimentos (EstabelecimentoItemLista). */
export interface EstabelecimentoItemLista {
  id: string;
  cnes: string;
  nome: string;
  tipo: string;
  municipio: string;
  uf: string;
  situacao: string;
}

/** Ficha completa do estabelecimento (EstabelecimentoDetalhe). */
export interface EstabelecimentoDetalhe {
  id: string;
  cnes: string;
  nome: string;
  tipo: string;
  logradouro: string;
  numero: string;
  bairro: string;
  municipio: string;
  uf: string;
  cep: string;
  situacao: string;
}

/** Filtros da busca de estabelecimentos (paginação 1-based; tipo/situacao = nome do enum). */
export interface EstabelecimentoBuscaFiltro {
  termo?: string;
  tipo?: string;
  situacao?: string;
  pagina: number;
  tamanho: number;
}

/** EnderecoEstabelecimentoDto. */
export interface EnderecoEstabelecimentoInput {
  logradouro: string;
  numero: string;
  bairro: string;
  municipio: string;
  uf: string;
  cep: string;
}

/** CadastrarEstabelecimentoCommand(Cnes, Nome, Tipo, Endereco). Tipo = nome do enum. */
export interface CadastrarEstabelecimentoInput {
  cnes: string;
  nome: string;
  tipo: string;
  endereco: EnderecoEstabelecimentoInput;
}

/** AtualizarEstabelecimentoPayload(Nome, Tipo, Endereco) — PUT (CNES é imutável). */
export interface AtualizarEstabelecimentoInput {
  nome: string;
  tipo: string;
  endereco: EnderecoEstabelecimentoInput;
}

// --- Acesso HTTP ---

function normalizarTexto(valor?: string): string | undefined {
  const limpo = valor?.trim();
  return limpo && limpo.length > 0 ? limpo : undefined;
}

function buscarEstabelecimentos(
  filtro: EstabelecimentoBuscaFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<EstabelecimentoItemLista>> {
  return http.get<ResultadoPaginado<EstabelecimentoItemLista>>('/saude/estabelecimentos', {
    query: {
      termo: normalizarTexto(filtro.termo),
      tipo: normalizarTexto(filtro.tipo),
      situacao: normalizarTexto(filtro.situacao),
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function obterEstabelecimento(id: string, signal?: AbortSignal): Promise<EstabelecimentoDetalhe> {
  return http.get<EstabelecimentoDetalhe>(`/saude/estabelecimentos/${id}`, { signal });
}

async function cadastrarEstabelecimento(input: CadastrarEstabelecimentoInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/estabelecimentos', input);
  return id;
}

function atualizarEstabelecimento(id: string, input: AtualizarEstabelecimentoInput): Promise<void> {
  return http.put<void>(`/saude/estabelecimentos/${id}`, input);
}

function inativarEstabelecimento(id: string): Promise<void> {
  return http.post<void>(`/saude/estabelecimentos/${id}/inativacao`);
}

function reativarEstabelecimento(id: string): Promise<void> {
  return http.post<void>(`/saude/estabelecimentos/${id}/reativacao`);
}

// --- Hooks TanStack Query — QUERIES ---

/** Lista/busca paginada de estabelecimentos (CNES). `keepPreviousData` evita flicker ao paginar. */
export function useBuscarEstabelecimentos(filtro: EstabelecimentoBuscaFiltro, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.estabelecimentosBusca(filtro),
    queryFn: ({ signal }) => buscarEstabelecimentos(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

/** Ficha completa de um estabelecimento. */
export function useEstabelecimento(id: string) {
  return useQuery({
    queryKey: saudeKeys.estabelecimento(id),
    queryFn: ({ signal }) => obterEstabelecimento(id, signal),
    enabled: id.trim().length > 0,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Cadastra um estabelecimento (CNES) e invalida as listas. */
export function useCadastrarEstabelecimento() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarEstabelecimento,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.estabelecimentos() });
    },
  });
}

/** Atualiza os dados cadastrais (PUT). Invalida o detalhe e as listas. */
export function useAtualizarEstabelecimento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: AtualizarEstabelecimentoInput) => atualizarEstabelecimento(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.estabelecimentos() });
    },
  });
}

/** Inativa um estabelecimento (encerramento/suspensão). Invalida as consultas. */
export function useInativarEstabelecimento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => inativarEstabelecimento(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.estabelecimentos() });
    },
  });
}

/** Reativa um estabelecimento inativado. Invalida as consultas. */
export function useReativarEstabelecimento(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => reativarEstabelecimento(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.estabelecimentos() });
    },
  });
}
