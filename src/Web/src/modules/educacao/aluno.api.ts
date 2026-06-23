// API do agregado Aluno (módulo Educação). DTOs + acesso HTTP + hooks TanStack
// Query. Espelha os endpoints REAIS sob /api/educacao (EducacaoEndpoints.cs):
//   GET    /educacao/alunos?termo=&situacao=&pagina=&tamanho= -> BuscarAlunos  -> ResultadoPaginado<AlunoItemLista>
//   GET    /educacao/alunos/{alunoId}                         -> ObterAluno    -> AlunoFicha | null
//   POST   /educacao/alunos                                   -> CadastrarAluno-> { id }
//   POST   /educacao/alunos/{alunoId}/responsaveis            -> AdicionarResponsavel -> { id }
//
// LGPD (minimização): a LISTA não traz CPF (AlunoItemLista); a ficha traz CPF MASCARADO.
import { useMutation, useQuery, useQueryClient, keepPreviousData } from '@tanstack/react-query';
import { http } from '../../api/http';
import { educacaoKeys } from './educacao.keys';
import type { CriadoResponse } from './educacao.keys';

/** Sexo do aluno (EducaCenso/INEP) — valor numérico esperado pelo backend (IsInEnum). */
export type Sexo = 1 | 2 | 9;

/** Situação do cadastro do aluno (Domain.Alunos.SituacaoAluno). */
export type SituacaoAluno = 'Ativo' | 'Transferido' | 'Inativo';

/** Valor numérico do enum SituacaoAluno (filtro da busca espera o número). */
export const SITUACAO_ALUNO_VALOR: Record<SituacaoAluno, number> = {
  Ativo: 1,
  Transferido: 2,
  Inativo: 3,
};

/** Grau de parentesco do responsável (Domain.Alunos.Parentesco) — valor numérico. */
export type Parentesco = 1 | 2 | 3 | 4 | 9;

/**
 * Item da lista/busca de alunos (AlunoItemLista). Projeção minimizada (LGPD): sem CPF.
 * É o picker do front — fim do GUID digitado na matrícula.
 */
export interface AlunoItemLista {
  id: string;
  nome: string;
  nomeSocial: string | null;
  dataNascimento: string;
  sexo: string;
  codigoInepAluno: string | null;
  situacao: string;
}

/** Responsável projetado na ficha do aluno (ResponsavelDto). CPF sempre mascarado (LGPD). */
export interface ResponsavelDto {
  id: string;
  nome: string;
  cpfMascarado: string | null;
  parentesco: string;
  telefone: string | null;
  responsavelFinanceiro: boolean;
  autorizadoBuscar: boolean;
}

/** Ficha completa do aluno (AlunoFicha): dados civis + endereço + responsáveis. */
export interface AlunoFicha {
  id: string;
  nome: string;
  nomeSocial: string | null;
  dataNascimento: string;
  sexo: string;
  nomeMae: string;
  nomePai: string | null;
  cpfMascarado: string | null;
  codigoInepAluno: string | null;
  situacao: string;
  logradouro: string;
  numero: string;
  bairro: string;
  municipio: string;
  uf: string;
  cep: string;
  responsaveis: ResponsavelDto[];
}

/** Envelope de leitura paginada compartilhado pelos list/search do backend (ResultadoPaginado<T>). */
export interface ResultadoPaginado<T> {
  itens: T[];
  total: number;
  pagina: number;
  tamanho: number;
}

/** Filtros da busca de alunos (todos opcionais salvo a paginação; 1-based). */
export interface AlunoBuscaFiltro {
  termo?: string;
  /** Valor numérico do enum SituacaoAluno. */
  situacao?: number;
  pagina: number;
  tamanho: number;
}

/** DadosCivisPayload — corpo dos comandos de cadastro/atualização. */
export interface DadosCivisInput {
  nome: string;
  dataNascimento: string; // ISO yyyy-mm-dd (DateOnly)
  sexo: Sexo;
  nomeMae: string;
  nomePai?: string | null;
  nomeSocial?: string | null;
}

/** EnderecoAlunoPayload — endereço residencial de entrada do aluno. */
export interface EnderecoAlunoInput {
  logradouro: string;
  numero: string;
  bairro: string;
  municipio: string;
  uf: string;
  cep: string;
}

/** ResponsavelPayload — responsável de entrada (cadastro/adição). */
export interface ResponsavelInput {
  nome: string;
  cpf?: string | null;
  parentesco: Parentesco;
  telefone?: string | null;
  responsavelFinanceiro: boolean;
  autorizadoBuscar: boolean;
}

/** Corpo de POST /alunos (CadastrarAlunoCommand). */
export interface CadastrarAlunoInput {
  dadosCivis: DadosCivisInput;
  endereco: EnderecoAlunoInput;
  cpf?: string | null;
  responsaveis: ResponsavelInput[];
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function normalizarTexto(valor?: string): string | undefined {
  const limpo = valor?.trim();
  return limpo && limpo.length > 0 ? limpo : undefined;
}

function buscarAlunos(
  filtro: AlunoBuscaFiltro,
  signal?: AbortSignal,
): Promise<ResultadoPaginado<AlunoItemLista>> {
  return http.get<ResultadoPaginado<AlunoItemLista>>('/educacao/alunos', {
    query: {
      termo: normalizarTexto(filtro.termo),
      situacao: filtro.situacao,
      pagina: filtro.pagina,
      tamanho: filtro.tamanho,
    },
    signal,
  });
}

function obterAluno(alunoId: string, signal?: AbortSignal): Promise<AlunoFicha | null> {
  return http.get<AlunoFicha | null>(`/educacao/alunos/${alunoId}`, { signal });
}

function cadastrarAluno(input: CadastrarAlunoInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>('/educacao/alunos', input);
}

function adicionarResponsavel(alunoId: string, input: ResponsavelInput): Promise<CriadoResponse> {
  return http.post<CriadoResponse>(`/educacao/alunos/${alunoId}/responsaveis`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/**
 * Lista/busca paginada de alunos (picker da matrícula). `keepPreviousData` evita
 * "flicker" ao paginar/digitar; `enabled` permite suprimir a requisição.
 */
export function useBuscarAlunos(filtro: AlunoBuscaFiltro, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.alunosBusca(filtro),
    queryFn: ({ signal }) => buscarAlunos(filtro, signal),
    placeholderData: keepPreviousData,
    enabled,
  });
}

/** Obtém a ficha completa de um aluno. `enabled` controla disparo sob demanda. */
export function useAluno(alunoId: string, enabled = true) {
  return useQuery({
    queryKey: educacaoKeys.alunoPorId(alunoId),
    queryFn: ({ signal }) => obterAluno(alunoId, signal),
    enabled: enabled && alunoId.trim().length > 0,
  });
}

/** Cadastra um novo aluno e invalida as buscas de alunos. */
export function useCadastrarAluno() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: cadastrarAluno,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.alunos() });
    },
  });
}

/** Adiciona um responsável a um aluno (LGPD art. 14) e invalida a ficha do aluno. */
export function useAdicionarResponsavel(alunoId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: ResponsavelInput) => adicionarResponsavel(alunoId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: educacaoKeys.alunoPorId(alunoId) });
    },
  });
}
