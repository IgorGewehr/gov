// Helpers de apresentação compartilhados pelas telas do módulo Finanças.
import type { TagVariant } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  CATEGORIA_ECONOMICA,
  TIPO_DOCUMENTO_COMPROBATORIO,
  TIPO_EMPENHO,
  TIPO_PESSOA,
} from './financas.api';
import type { SelectOption } from '../../components/ui';

/**
 * Variante semântica da Tag a partir da string de situação (qualquer agregado do
 * ciclo da despesa). Mapeamento por palavra-chave para uniformizar a cor.
 */
export function situacaoTagVariant(situacao: string): TagVariant {
  const s = situacao.toLowerCase();
  if (s.includes('pago') || s.includes('quitad') || s.includes('efetuad')) return 'success';
  if (s.includes('anulad') || s.includes('cancelad') || s.includes('estornad')) return 'danger';
  if (s.includes('liquidad')) return 'info';
  if (s.includes('empenhad') || s.includes('inscrit') || s.includes('aberto') || s.includes('emitid'))
    return 'warning';
  return 'default';
}

/** Opções de categoria econômica para <Select> (value = string do número do enum). */
export const CATEGORIAS_ECONOMICAS: SelectOption[] = [
  { value: String(CATEGORIA_ECONOMICA.DespesasCorrentes), label: 'Despesas Correntes (custeio)' },
  { value: String(CATEGORIA_ECONOMICA.DespesasDeCapital), label: 'Despesas de Capital (investimentos)' },
];

/** Opções de tipo de empenho. */
export const TIPOS_EMPENHO: SelectOption[] = [
  { value: String(TIPO_EMPENHO.Ordinario), label: 'Ordinário' },
  { value: String(TIPO_EMPENHO.Estimativo), label: 'Estimativo' },
  { value: String(TIPO_EMPENHO.Global), label: 'Global' },
];

/** Opções de natureza do credor. */
export const TIPOS_PESSOA: SelectOption[] = [
  { value: String(TIPO_PESSOA.Fisica), label: 'Pessoa física (CPF)' },
  { value: String(TIPO_PESSOA.Juridica), label: 'Pessoa jurídica (CNPJ)' },
];

/** Opções de documento comprobatório da liquidação. */
export const TIPOS_DOCUMENTO: SelectOption[] = [
  { value: String(TIPO_DOCUMENTO_COMPROBATORIO.NotaFiscal), label: 'Nota fiscal' },
  { value: String(TIPO_DOCUMENTO_COMPROBATORIO.NfseChaveAcesso), label: 'NFS-e (chave de acesso)' },
  { value: String(TIPO_DOCUMENTO_COMPROBATORIO.Fatura), label: 'Fatura' },
  { value: String(TIPO_DOCUMENTO_COMPROBATORIO.Recibo), label: 'Recibo' },
  { value: String(TIPO_DOCUMENTO_COMPROBATORIO.Outro), label: 'Outro documento hábil' },
];

/** Data de hoje em ISO yyyy-mm-dd (default de campos de data). */
export function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}

/** Exercício corrente (default de filtros). */
export function exercicioCorrente(): number {
  return new Date().getFullYear();
}

/**
 * Mapeia ProblemDetails.fieldErrors para os campos conhecidos do formulário
 * (chave em camelCase). Espelha o `tratarErroCampos` do módulo Protocolo.
 */
export function tratarErroCampos(
  error: unknown,
  conhecidos: Record<string, true>,
): Record<string, string> {
  const mapped: Record<string, string> = {};
  if (error instanceof ApiError) {
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      if (key in conhecidos) mapped[key] = messages[0];
    }
  }
  return mapped;
}

/** Mensagem amigável a partir de um erro (ApiError ou genérico). */
export function mensagemErro(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.userMessage : fallback;
}
