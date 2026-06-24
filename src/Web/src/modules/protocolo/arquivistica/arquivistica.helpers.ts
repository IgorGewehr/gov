// Helpers de apresentacao compartilhados pelas telas arquivisticas (CONARQ).
import type { SelectOption, TagVariant } from '../../../components/ui';
import {
  DESTINACAO_VALOR,
  EVENTO_CONTAGEM_VALOR,
  type Destinacao,
  type EventoContagem,
} from './arquivistica.api';

/** Resolve a chave do enum Destinacao a partir do valor numerico do backend. */
export function chaveDestinacao(valor: number): Destinacao {
  return valor === DESTINACAO_VALOR.GuardaPermanente ? 'GuardaPermanente' : 'Eliminacao';
}

/** Resolve a chave do enum EventoContagem a partir do valor numerico do backend. */
export function chaveEventoContagem(valor: number): EventoContagem {
  if (valor === EVENTO_CONTAGEM_VALOR.DataAutuacao) return 'DataAutuacao';
  if (valor === EVENTO_CONTAGEM_VALOR.AprovacaoContas) return 'AprovacaoContas';
  return 'DataArquivamento';
}

/** Rotulo legivel em PT-BR para a destinacao final. */
export const DESTINACAO_LABEL: Record<Destinacao, string> = {
  Eliminacao: 'Eliminação',
  GuardaPermanente: 'Guarda permanente',
};

/** Rotulo legivel em PT-BR para o evento de contagem do prazo. */
export const EVENTO_CONTAGEM_LABEL: Record<EventoContagem, string> = {
  DataAutuacao: 'Data de autuação',
  DataArquivamento: 'Data de arquivamento',
  AprovacaoContas: 'Aprovação das contas',
};

/** Variante semantica da Tag para a destinacao (eliminacao = atencao; permanente = sucesso). */
export function destinacaoTagVariant(destinacao: Destinacao): TagVariant {
  return destinacao === 'GuardaPermanente' ? 'success' : 'warning';
}

/** Opcoes do Select de destinacao (rotulo PT-BR + chave do enum). */
export const DESTINACAO_OPCOES: SelectOption[] = (
  ['Eliminacao', 'GuardaPermanente'] as Destinacao[]
).map((d) => ({ value: d, label: DESTINACAO_LABEL[d] }));

/** Opcoes do Select de evento de contagem. */
export const EVENTO_CONTAGEM_OPCOES: SelectOption[] = (
  ['DataAutuacao', 'DataArquivamento', 'AprovacaoContas'] as EventoContagem[]
).map((e) => ({ value: e, label: EVENTO_CONTAGEM_LABEL[e] }));

/** Comprimento do hash SHA-256 em hexadecimal (Hash.ComprimentoSha256 no backend). */
export const COMPRIMENTO_HASH_SHA256 = 64;

/** Verdadeiro se a string e um hash SHA-256 hexadecimal valido (64 chars hex). */
export function hashSha256Valido(valor: string): boolean {
  return /^[0-9a-fA-F]{64}$/.test(valor.trim());
}

/** Verdadeiro se a string e um GUID valido (identificador de setor/autoridade/ficha). */
export function guidValido(valor: string): boolean {
  return /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(
    valor.trim(),
  );
}
