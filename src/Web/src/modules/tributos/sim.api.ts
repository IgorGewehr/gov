// Camada de API do submódulo S.I.M. — Serviço de Inspeção Municipal (título de registro).
// Espelha o contrato REAL da Minimal API /api/tributos/sim:
//   POST /titulos                          -> RequererTituloSim -> { id }            [tributos.gerenciar]
//   POST /titulos/{id}/conceder            -> ConcederTituloSim -> ResultadoConcessao [tributos.gerenciar]
//   POST /titulos/{id}/produtos            -> HabilitarProdutoSim -> { ok }           [tributos.gerenciar]
//   POST /titulos/{id}/situacao            -> AlterarSituacaoTituloSim -> { ok }       [tributos.gerenciar]
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

/** Natureza do produto inspecionado (back: NaturezaProdutoSim). */
export type NaturezaProdutoSim = 'OrigemAnimal' | 'OrigemVegetal';

/** Produto inspecionado a habilitar (back: ProdutoSimInput). */
export interface ProdutoSimInput {
  denominacao: string;
  classificacao: string;
  numeroRotulo: string;
}

/** RequererTituloSimCommand. */
export interface RequererTituloSimInput {
  responsavelId: string;
  razaoSocialEstabelecimento: string;
  natureza: NaturezaProdutoSim;
  enderecoEstabelecimento: string;
  /** Data do requerimento ("yyyy-MM-dd"). */
  dataRequerimento: string;
  produtos: ProdutoSimInput[];
}

/** Ação de mudança de situação do título (back: AcaoTituloSim). */
export type AcaoTituloSim = 'Suspender' | 'Reativar' | 'Cassar';

/** Resultado da concessão (espelha ResultadoConcessaoSim). */
export interface ResultadoConcessaoSim {
  tituloId: string;
  numeroSim: string;
}

function requererTitulo(input: RequererTituloSimInput): Promise<{ id: string }> {
  return http.post<{ id: string }>('/tributos/sim/titulos', input);
}

function concederTitulo(
  tituloId: string,
  input: { dataRegistro: string; fimVigencia: string },
): Promise<ResultadoConcessaoSim> {
  return http.post<ResultadoConcessaoSim>(`/tributos/sim/titulos/${tituloId}/conceder`, input);
}

function habilitarProduto(tituloId: string, input: ProdutoSimInput): Promise<{ ok: boolean }> {
  return http.post<{ ok: boolean }>(`/tributos/sim/titulos/${tituloId}/produtos`, input);
}

function alterarSituacao(
  tituloId: string,
  input: { acao: AcaoTituloSim; motivo: string | null },
): Promise<{ ok: boolean }> {
  return http.post<{ ok: boolean }>(`/tributos/sim/titulos/${tituloId}/situacao`, input);
}

/** Protocola o requerimento de registro no S.I.M. */
export function useRequererTituloSim() {
  return useMutation({ mutationFn: requererTitulo });
}

/** Concede o registro (atribui o número do S.I.M.). */
export function useConcederTituloSim(tituloId: string) {
  return useMutation({
    mutationFn: (input: { dataRegistro: string; fimVigencia: string }) =>
      concederTitulo(tituloId, input),
  });
}

/** Habilita um novo produto inspecionado no título. */
export function useHabilitarProdutoSim(tituloId: string) {
  return useMutation({ mutationFn: (input: ProdutoSimInput) => habilitarProduto(tituloId, input) });
}

/** Suspende/reativa/cassa o título. */
export function useAlterarSituacaoTituloSim(tituloId: string) {
  return useMutation({
    mutationFn: (input: { acao: AcaoTituloSim; motivo: string | null }) =>
      alterarSituacao(tituloId, input),
  });
}
