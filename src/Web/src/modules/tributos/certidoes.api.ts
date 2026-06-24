// Camada de API do submódulo Certidões de regularidade fiscal — CND/CPEN (PARIDADE-PoC SW-A3).
// Espelha o contrato REAL da Minimal API /api/tributos/certidoes:
//   POST /api/tributos/certidoes/regularidade   -> EmitirCertidaoRegularidade -> CertidaoRegularidade [tributos.ver]
//   GET  /api/tributos/certidoes/conferir        -> ConferirCertidao          -> ConferenciaCertidao  [anônimo]
import { useMutation, useQuery } from '@tanstack/react-query';
import { http } from '../../api/http';

// ---------------------------------------------------------------------------
// Projeções de leitura
// ---------------------------------------------------------------------------

/** Tipo apurado da certidão (espelha TipoCertidaoRegularidade). */
export type TipoCertidao = 'Negativa' | 'PositivaComEfeitoNegativa' | 'Positiva';

/** Rótulo amigável do tipo de certidão. */
export const ROTULO_TIPO_CERTIDAO: Record<TipoCertidao, string> = {
  Negativa: 'Negativa (CND)',
  PositivaComEfeitoNegativa: 'Positiva com efeito de Negativa (CPEN)',
  Positiva: 'Positiva',
};

/** Certidão de regularidade emitida (espelha CertidaoRegularidadeDto). */
export interface CertidaoRegularidade {
  certidaoId: string;
  numero: string;
  tipo: TipoCertidao;
  atestaRegularidade: boolean;
  documento: string;
  nomeContribuinte: string;
  dataEmissao: string;
  dataValidade: string;
  codigoAutenticacao: string;
  fundamentoLegal: string;
  observacao: string | null;
}

/** Resultado da conferência de autenticidade (espelha ConferenciaCertidaoDto). */
export interface ConferenciaCertidao {
  autentica: boolean;
  vigente: boolean;
  tipo: TipoCertidao | null;
  atestaRegularidade: boolean;
  numero: string | null;
  nomeContribuinte: string | null;
  dataEmissao: string | null;
  dataValidade: string | null;
}

// ---------------------------------------------------------------------------
// Entradas de comando
// ---------------------------------------------------------------------------

/** EmitirCertidaoRegularidadeCommand. */
export interface EmitirCertidaoInput {
  contribuinteId: string;
  /** Fundamento legal (CTN + CTM), obrigatório. */
  fundamentoLegal: string;
  /** Prazo de validade em dias (1..180); usa o padrão do domínio (90) se omitido. */
  diasValidade?: number;
}

// ---------------------------------------------------------------------------
// Acesso HTTP
// ---------------------------------------------------------------------------

function emitirCertidao(input: EmitirCertidaoInput): Promise<CertidaoRegularidade> {
  return http.post<CertidaoRegularidade>('/tributos/certidoes/regularidade', input);
}

function conferirCertidao(numero: string, codigo: string): Promise<ConferenciaCertidao> {
  const query = new URLSearchParams({ numero, codigo }).toString();
  return http.get<ConferenciaCertidao>(`/tributos/certidoes/conferir?${query}`);
}

// ---------------------------------------------------------------------------
// Hooks TanStack Query
// ---------------------------------------------------------------------------

/** EMITE a certidão de regularidade fiscal do contribuinte (CND/CPEN). */
export function useEmitirCertidao() {
  return useMutation({ mutationFn: emitirCertidao });
}

/** CONFERE a autenticidade de uma certidão (número + código). Habilitado sob demanda. */
export function useConferirCertidao(numero: string, codigo: string, enabled: boolean) {
  return useQuery({
    queryKey: ['tributos', 'certidoes', 'conferir', numero, codigo],
    queryFn: () => conferirCertidao(numero.trim(), codigo.trim()),
    enabled: enabled && numero.trim() !== '' && codigo.trim() !== '',
  });
}
