// Cliente HTTP DEDICADO do Portal do Cidadao (realm externo).
//
// Por que NAO reutiliza o api/http.ts compartilhado: aquele cliente injeta o token do
// ADMIN e, no 401, faz logout do admin + navega para /login. O cidadao e outro realm
// (outro token, outro fluxo de logout). Este wrapper injeta o token do cidadao e, no
// 401, limpa SO o token do cidadao — sem tocar a sessao do admin.
//
// Os DTOs abaixo espelham FIELMENTE os contratos do backend:
//   - login/cadastro: src/Modules/Cidadao/.../CidadaoEndpoints.cs
//   - meus-debitos / minha-divida-ativa / dams/{id}/segunda-via:
//     src/Modules/Tributos/.../IConsultaTributariaCidadao.cs
//   - meus-processos: src/Modules/Protocolo/.../IConsultaProcessoCidadao.cs
import { ApiError, type ProblemDetails } from '../../api/problemDetails';
import { clearCidadaoToken, getCidadaoToken } from './cidadaoAuthToken';

const BASE_URL = '/api/cidadao';

// === Contratos de autenticacao (CidadaoEndpoints.cs) ===

/** Cadastro self-service. O cidadao escolhe o municipio (tenantId). */
export interface CadastroCidadaoRequest {
  tenantId: string;
  documento: string;
  nome: string;
  senha: string;
  email?: string | null;
  telefone?: string | null;
}

/** Login local por CPF/CNPJ + senha no municipio (tenantId). */
export interface LoginCidadaoRequest {
  tenantId: string;
  documento: string;
  senha: string;
}

/** Resposta do /login: { accessToken, expiraEm }. */
export interface LoginCidadaoResponse {
  accessToken: string;
  expiraEm: string;
}

// === DTOs de "Meus dados" (espelham os Contracts do backend) ===

/** MeuLancamentoDto (IConsultaTributariaCidadao). */
export interface MeuLancamento {
  lancamentoId: string;
  tributo: string;
  competencia: string;
  vencimento: string;
  valorPrincipal: number;
  situacao: string;
}

/** MinhaDividaAtivaDto (IConsultaTributariaCidadao). */
export interface MinhaDividaAtiva {
  dividaAtivaId: string;
  tributo: string;
  numeroInscricao: number;
  numeroCda: string | null;
  valorOriginario: number;
  valorAtualizado: number;
  situacao: string;
  parcelada: boolean;
  dataInscricao: string;
}

/** MinhaParcelaDamDto (IConsultaTributariaCidadao). */
export interface MinhaParcelaDam {
  numero: number;
  valor: number;
  vencimento: string;
  paga: boolean;
}

/** MeuDamDto (IConsultaTributariaCidadao). */
export interface MeuDam {
  damId: string;
  lancamentoId: string;
  valorTotal: number;
  quitado: boolean;
  parcelas: MinhaParcelaDam[];
}

/** MeuProcessoDto (IConsultaProcessoCidadao). */
export interface MeuProcesso {
  processoId: string;
  nup: string;
  classificacao: string;
  situacao: string;
  dataAutuacao: string;
}

type Method = 'GET' | 'POST';

async function parseProblem(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) return null;
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

/** Requisicao tipada com o token do cidadao; trata 401 limpando SO a sessao do cidadao. */
async function request<T>(method: Method, path: string, body?: unknown): Promise<T> {
  const token = getCidadaoToken();
  let response: Response;
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers: {
        Accept: 'application/json',
        ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError('Nao foi possivel conectar ao servidor. Verifique sua conexao.', 0);
  }

  if (response.status === 401) {
    // Sessao do cidadao expirou/invalida: limpa SO o token do cidadao (o evento
    // CIDADAO_TOKEN_CHANGED leva o provider do portal a derrubar a sessao e redirecionar).
    clearCidadaoToken();
    throw new ApiError('Sua sessao expirou. Entre novamente.', 401, await parseProblem(response));
  }

  if (!response.ok) {
    const problem = await parseProblem(response);
    const message =
      problem?.detail ?? problem?.title ?? `Erro ao processar a solicitacao (HTTP ${response.status}).`;
    throw new ApiError(message, response.status, problem);
  }

  if (response.status === 204) return undefined as T;
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) return undefined as T;
  return (await response.json()) as T;
}

export const cidadaoApi = {
  // --- Anonimos (sem token): o cidadao se cadastra/loga no municipio escolhido ---
  cadastrar: (payload: CadastroCidadaoRequest): Promise<{ id: string }> =>
    request<{ id: string }>('POST', '/cadastro', payload),
  login: (payload: LoginCidadaoRequest): Promise<LoginCidadaoResponse> =>
    request<LoginCidadaoResponse>('POST', '/login', payload),

  // --- Dado-proprio (token tipo=cidadao): o backend resolve a pessoa do PROPRIO token ---
  meusDebitos: (): Promise<MeuLancamento[]> => request<MeuLancamento[]>('GET', '/meus-debitos'),
  minhaDividaAtiva: (): Promise<MinhaDividaAtiva[]> =>
    request<MinhaDividaAtiva[]>('GET', '/minha-divida-ativa'),
  segundaViaDam: (damId: string): Promise<MeuDam> =>
    request<MeuDam>('GET', `/dams/${damId}/segunda-via`),
  meusProcessos: (): Promise<MeuProcesso[]> => request<MeuProcesso[]>('GET', '/meus-processos'),
};
