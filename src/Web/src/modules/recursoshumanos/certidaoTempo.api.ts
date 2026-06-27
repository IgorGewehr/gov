// Camada de API da Certidao de Tempo de Servico/Contribuicao (CTC) no modulo RecursosHumanos.
// Contrato real: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.TempoServico.cs
//   POST /recursoshumanos/certidoes-tempo                       -> emite certidao (apura tempo) -> { id }
//   GET  /recursoshumanos/certidoes-tempo/servidores/{id}       -> ficha de certidoes do servidor
//   GET  /recursoshumanos/certidoes-tempo/{id}                  -> detalhe do documento (periodos)
//   GET  /recursoshumanos/certidoes-tempo/validar/{codigo}      -> validacao publica por codigo
//   POST /recursoshumanos/certidoes-tempo/{id}/anulacao         -> anula (204)
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { http } from '../../api/http';
import { rhKeys } from './rhKeys';
import type { CriacaoResponse } from './rhKeys';

// ---------------------------------------------------------------------------
// DTOs (rotulos -> enums string do backend)
// ---------------------------------------------------------------------------

/** Finalidade da certidao (determina o regime de contagem). */
export type FinalidadeCertidao =
  | 'Aposentadoria'
  | 'Disponibilidade'
  | 'AdicionalTempoServico'
  | 'LicencaPremio'
  | 'Declaratoria';

/** Natureza de um periodo computado. */
export type NaturezaPeriodo = 'EfetivoExercicio' | 'Averbado';

/** Regime de origem de um periodo averbado. */
export type RegimeOrigemPeriodo = 'Rgps' | 'Rpps' | 'Militar';

/** Situacao da certidao. */
export type SituacaoCertidao = 'Emitida' | 'Anulada';

/** Rotulos legiveis das finalidades (pt-BR). */
export const FINALIDADES: ReadonlyArray<{ valor: FinalidadeCertidao; rotulo: string }> = [
  { valor: 'Aposentadoria', rotulo: 'Aposentadoria (contagem reciproca)' },
  { valor: 'Disponibilidade', rotulo: 'Disponibilidade' },
  { valor: 'AdicionalTempoServico', rotulo: 'Adicional/Gratificacao por tempo de servico' },
  { valor: 'LicencaPremio', rotulo: 'Licenca-premio' },
  { valor: 'Declaratoria', rotulo: 'Declaratoria (fins gerais)' },
];

/** Rotulos legiveis dos regimes de origem (pt-BR). */
export const REGIMES_ORIGEM: ReadonlyArray<{ valor: RegimeOrigemPeriodo; rotulo: string }> = [
  { valor: 'Rgps', rotulo: 'RGPS (INSS)' },
  { valor: 'Rpps', rotulo: 'RPPS (regime proprio)' },
  { valor: 'Militar', rotulo: 'Militar' },
];

/** Periodo averbado informado na emissao. */
export interface PeriodoAverbadoInput {
  inicio: string;
  fim: string;
  regimeOrigem: RegimeOrigemPeriodo;
  origem: string;
  diasNaoComputaveis?: number;
  fator?: number;
  observacao?: string | null;
}

/** Entrada da emissao de certidao. */
export interface EmitirCertidaoInput {
  servidorId: string;
  finalidade: FinalidadeCertidao;
  orgaoEmissor: string;
  dataBase?: string | null;
  averbados?: PeriodoAverbadoInput[];
  incluirEfetivoExercicio?: boolean;
  finalidadeDescrita?: string | null;
  observacao?: string | null;
}

/** Resumo de certidao (ficha). */
export interface CertidaoResumoView {
  id: string;
  numero: string;
  finalidade: FinalidadeCertidao;
  dataEmissao: string;
  situacao: SituacaoCertidao;
  totalDias: number;
  tempoFormatado: string;
  codigoAutenticacao: string;
}

/** Periodo computado (linha do documento). */
export interface PeriodoTempoView {
  inicio: string;
  fim: string;
  natureza: NaturezaPeriodo;
  regimeOrigem: RegimeOrigemPeriodo | null;
  origem: string | null;
  diasBrutos: number;
  diasNaoComputaveis: number;
  diasLiquidos: number;
  fator: number;
  diasEquivalentes: number;
  observacao: string | null;
}

/** Detalhe completo da certidao (documento). */
export interface CertidaoDetalheView {
  id: string;
  servidorId: string;
  servidorNome: string;
  matricula: string;
  cpfMascarado: string;
  numero: string;
  finalidade: FinalidadeCertidao;
  finalidadeDescrita: string | null;
  dataEmissao: string;
  orgaoEmissor: string;
  situacao: SituacaoCertidao;
  motivoAnulacao: string | null;
  observacao: string | null;
  codigoAutenticacao: string;
  totalDias: number;
  totalDiasLiquidos: number;
  anos: number;
  meses: number;
  dias: number;
  tempoFormatado: string;
  periodos: PeriodoTempoView[];
}

/** Resultado da validacao publica por codigo. */
export interface ValidacaoCertidaoView {
  valida: boolean;
  numero: string | null;
  servidorNome: string | null;
  finalidade: FinalidadeCertidao | null;
  dataEmissao: string | null;
  tempoFormatado: string | null;
}

// ---------------------------------------------------------------------------
// Acesso HTTP + hooks
// ---------------------------------------------------------------------------

const BASE = '/recursoshumanos/certidoes-tempo';

/** Lista as certidoes de tempo de um servidor (mais recentes primeiro). */
export function useCertidoesDoServidor(servidorId: string) {
  return useQuery({
    queryKey: rhKeys.certidoesDoServidor(servidorId),
    queryFn: () => http.get<CertidaoResumoView[]>(`${BASE}/servidores/${servidorId}`),
    enabled: servidorId.length > 0,
  });
}

/** Detalhe completo (documento) de uma certidao. */
export function useCertidao(certidaoId: string) {
  return useQuery({
    queryKey: rhKeys.certidao(certidaoId),
    queryFn: () => http.get<CertidaoDetalheView>(`${BASE}/${certidaoId}`),
    enabled: certidaoId.length > 0,
  });
}

/** Validacao publica por codigo de autenticacao (so dispara quando habilitada). */
export function useValidarCertidao(codigo: string, habilitada: boolean) {
  return useQuery({
    queryKey: rhKeys.certidaoValidacao(codigo),
    queryFn: () => http.get<ValidacaoCertidaoView>(`${BASE}/validar/${encodeURIComponent(codigo)}`),
    enabled: habilitada && codigo.length > 0,
  });
}

/** Emite uma certidao e invalida a ficha do servidor. */
export function useEmitirCertidao() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: EmitirCertidaoInput) => http.post<CriacaoResponse>(BASE, input),
    onSuccess: (_data, input) => {
      queryClient.invalidateQueries({ queryKey: rhKeys.certidoesDoServidor(input.servidorId) });
    },
  });
}

/** Anula uma certidao e invalida a ficha do servidor. */
export function useAnularCertidao(servidorId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ certidaoId, motivo }: { certidaoId: string; motivo: string }) =>
      http.post<void>(`${BASE}/${certidaoId}/anulacao`, { motivo }),
    onSuccess: (_data, { certidaoId }) => {
      queryClient.invalidateQueries({ queryKey: rhKeys.certidoesDoServidor(servidorId) });
      queryClient.invalidateQueries({ queryKey: rhKeys.certidao(certidaoId) });
    },
  });
}
