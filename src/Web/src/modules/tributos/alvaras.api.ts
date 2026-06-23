// Camada de API do submódulo ALVARÁS + TLL (Tributos M6). Espelha FIELMENTE o
// contrato REAL de TaxasCosipAlvaraMelhoriaEndpoints.cs:
//   POST /api/tributos/alvaras/emitir          -> EmitirAlvaraCommand  -> ResultadoEmissaoAlvara   [tributos.gerenciar]
//   POST /api/tributos/alvaras/{id}/renovar    -> RenovarAlvaraPayload -> ResultadoRenovacaoAlvara [tributos.gerenciar]
//
// O alvará é o ATO de polícia; a TLL (Taxa de Licença) é o tributo correlato,
// lançado à parte a partir da tabela de taxa vigente (código + exercício).
// Convenções (PADRÃO-OURO): enums por valor INT no payload; DTOs no topo.
import { useMutation } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// Enums (string na UI; INT no payload via *_VALOR — espelham o domínio C#)
// ---------------------------------------------------------------------------

/** Espécie do alvará — enum EspecieAlvara (1..4). */
export type EspecieAlvara = 'LocalizacaoFuncionamento' | 'Sanitario' | 'Ambiental' | 'Obras';

export const ESPECIE_ALVARA_VALOR: Record<EspecieAlvara, number> = {
  LocalizacaoFuncionamento: 1,
  Sanitario: 2,
  Ambiental: 3,
  Obras: 4,
};

export const ESPECIE_ALVARA_LABEL: Record<EspecieAlvara, string> = {
  LocalizacaoFuncionamento: 'Localização e funcionamento',
  Sanitario: 'Sanitário',
  Ambiental: 'Ambiental',
  Obras: 'Obras / construção',
};

// ---------------------------------------------------------------------------
// Entradas de comando (espelham os Commands/Payloads reais)
// ---------------------------------------------------------------------------

/** EmitirAlvaraCommand (emite o alvará + lança a TLL). */
export interface EmitirAlvaraInput {
  contribuinteId: string;
  imovelId?: string | null;
  /** enum EspecieAlvara (INT). */
  especie: number;
  nomeEstabelecimento: string;
  atividadeCnae: string;
  /** "yyyy-MM-dd". */
  inicioVigencia: string;
  fimVigencia: string;
  /** Código da taxa de licença (TLL) no CTM. */
  codigoTaxaTll: string;
  exercicio: number;
  /** Quantidade-base da TLL (ex.: área/risco); ignorada no modo ValorFixo. */
  quantidadeBaseTll: number;
  vencimentoTll: string;
}

/** Resultado da emissão — back: ResultadoEmissaoAlvara. */
export interface ResultadoEmissaoAlvara {
  alvaraId: string;
  lancamentoTllId: string;
  damId: string;
  valorTll: number;
}

/** RenovarAlvaraPayload (renova por novo período + lança a TLL de renovação). */
export interface RenovarAlvaraInput {
  novoInicioVigencia: string;
  novoFimVigencia: string;
  codigoTaxaTll: string;
  exercicio: number;
  quantidadeBaseTll: number;
  vencimentoTll: string;
}

/** Resultado da renovação — back: ResultadoRenovacaoAlvara. */
export interface ResultadoRenovacaoAlvara {
  alvaraId: string;
  lancamentoTllId: string;
  damId: string;
  valorTll: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function emitirAlvara(input: EmitirAlvaraInput): Promise<ResultadoEmissaoAlvara> {
  return http.post<ResultadoEmissaoAlvara>('/tributos/alvaras/emitir', input);
}

function renovarAlvara(alvaraId: string, input: RenovarAlvaraInput): Promise<ResultadoRenovacaoAlvara> {
  return http.post<ResultadoRenovacaoAlvara>(`/tributos/alvaras/${alvaraId}/renovar`, input);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query — COMMANDS
// ---------------------------------------------------------------------------

/** Emite um alvará e lança a TLL correspondente. */
export function useEmitirAlvara() {
  return useMutation({ mutationFn: emitirAlvara });
}

/** Renova um alvará por novo período e lança a TLL de renovação. */
export function useRenovarAlvara() {
  return useMutation({
    mutationFn: ({ alvaraId, input }: { alvaraId: string; input: RenovarAlvaraInput }) =>
      renovarAlvara(alvaraId, input),
  });
}
