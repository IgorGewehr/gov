// Camada de API do agregado Profissional (CBO/CRM + vínculos CNES) — módulo Saúde.
// Contrato REAL (SaudeEndpoints.cs):
//   POST /saude/profissionais                              -> CadastrarProfissional -> { id }   (saude.gerenciar)
//   GET  /saude/profissionais?termo&cbo&estabelecimentoId&situacao&pagina&tamanho
//                                                          -> ResultadoPaginado<ProfissionalItemLista> (saude.ver)
//   GET  /saude/profissionais/{id}                         -> ProfissionalDetalhe                (saude.ver)
//   POST /saude/profissionais/{id}/vinculos                -> VincularProfissional -> { id }     (saude.gerenciar)
//   POST /saude/profissionais/{id}/vinculos/encerramento   -> 204                                 (saude.gerenciar)
//   POST /saude/profissionais/{id}/inativacao              -> 204                                 (saude.gerenciar)
// LGPD: a lista NÃO devolve CPF; a ficha devolve CPF mascarado.
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import { saudeKeys } from './saude.keys';
import type { IdResponse } from './saude.keys';
import type { ResultadoPaginado } from './paciente.api';

// --- DTOs ---

/** Item da lista de profissionais (ProfissionalItemLista). CPF NÃO é devolvido (LGPD). */
export interface ProfissionalItemLista {
  id: string;
  nome: string;
  conselho: string | null;
  qtdVinculosAtivos: number;
  situacao: string;
}

/** Vínculo CNES/CBO (VinculoCnesDto). */
export interface VinculoCnes {
  estabelecimentoId: string;
  cbo: string;
  dataInicio: string;
  dataFim: string | null;
}

/** Ficha completa do profissional (ProfissionalDetalhe). CPF mascarado (LGPD). */
export interface ProfissionalDetalhe {
  id: string;
  nome: string;
  cpfMascarado: string;
  cns: string | null;
  conselho: string | null;
  temCrmAtivo: boolean;
  situacao: string;
  vinculos: VinculoCnes[];
}

/** Filtros da busca de profissionais (paginação 1-based; situacao = nome do enum). */
export interface ProfissionalBuscaFiltro {
  termo?: string;
  cbo?: string;
  estabelecimentoId?: string;
  situacao?: string;
  pagina: number;
  tamanho: number;
}

/** RegistroConselhoDto(Tipo, Uf, Numero) — Tipo é o código numérico do conselho. */
export interface RegistroConselhoInput {
  tipo: number;
  uf: string;
  numero: string;
}

/** CadastrarProfissionalCommand(Cpf, Nome, Cns, Registro). */
export interface CadastrarProfissionalInput {
  cpf: string;
  nome: string;
  cns?: string | null;
  registro?: RegistroConselhoInput | null;
}

/** VincularProfissionalPayload(EstabelecimentoId, Cbo, DataInicio). */
export interface VincularProfissionalInput {
  estabelecimentoId: string;
  cbo: string;
  dataInicio: string;
}

/** EncerrarVinculoPayload(EstabelecimentoId, DataFim). */
export interface EncerrarVinculoInput {
  estabelecimentoId: string;
  dataFim: string;
}

// --- Acesso HTTP ---

function normalizarTexto(valor?: string): string | undefined {
  const limpo = valor?.trim();
  return limpo && limpo.length > 0 ? limpo : undefined;
}

function buscarProfissionais(
  filtro: ProfissionalBuscaFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<ProfissionalItemLista>> {
  return http.get<ResultadoPaginado<ProfissionalItemLista>>('/saude/profissionais', {
    query: {
      termo: normalizarTexto(filtro.termo),
      cbo: normalizarTexto(filtro.cbo),
      estabelecimentoId: normalizarTexto(filtro.estabelecimentoId),
      situacao: normalizarTexto(filtro.situacao),
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function obterProfissional(id: string, signal?: AbortSignal): Promise<ProfissionalDetalhe> {
  return http.get<ProfissionalDetalhe>(`/saude/profissionais/${id}`, { signal });
}

async function cadastrarProfissional(input: CadastrarProfissionalInput): Promise<string> {
  const { id } = await http.post<IdResponse>('/saude/profissionais', input);
  return id;
}

function vincularProfissional(id: string, input: VincularProfissionalInput): Promise<void> {
  return http.post<void>(`/saude/profissionais/${id}/vinculos`, input);
}

function encerrarVinculo(id: string, input: EncerrarVinculoInput): Promise<void> {
  return http.post<void>(`/saude/profissionais/${id}/vinculos/encerramento`, input);
}

function inativarProfissional(id: string): Promise<void> {
  return http.post<void>(`/saude/profissionais/${id}/inativacao`);
}

// --- Hooks TanStack Query — QUERIES ---

/** Lista/busca paginada de profissionais. `keepPreviousData` evita flicker ao paginar. */
export function useBuscarProfissionais(filtro: ProfissionalBuscaFiltro, enabled = true) {
  return useQuery({
    queryKey: saudeKeys.profissionaisBusca(filtro),
    queryFn: ({ signal }) => buscarProfissionais(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

/** Ficha completa de um profissional (com vínculos). */
export function useProfissional(id: string) {
  return useQuery({
    queryKey: saudeKeys.profissional(id),
    queryFn: ({ signal }) => obterProfissional(id, signal),
    enabled: id.trim().length > 0,
  });
}

// --- Hooks TanStack Query — COMMANDS ---

/** Cadastra um profissional e invalida as listas. */
export function useCadastrarProfissional() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarProfissional,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.profissionais() });
    },
  });
}

/** Abre um vínculo CNES/CBO. Invalida a ficha e as listas. */
export function useVincularProfissional(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: VincularProfissionalInput) => vincularProfissional(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.profissionais() });
    },
  });
}

/** Encerra o vínculo ativo do profissional num estabelecimento. Invalida a ficha e as listas. */
export function useEncerrarVinculo(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: EncerrarVinculoInput) => encerrarVinculo(id, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.profissionais() });
    },
  });
}

/** Inativa o profissional (desligamento) — não recebe novos vínculos. Invalida as consultas. */
export function useInativarProfissional(id: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => inativarProfissional(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: saudeKeys.profissionais() });
    },
  });
}
