// Helpers de apresentação compartilhados pelas telas do agregado BemPatrimonial.
import type { TagVariant, SelectOption } from '../../../components/ui';
import type { SituacaoBemPatrimonial, TipoBemNumero } from './bempatrimonial.api';

/** Rótulo legível para a situação do bem (enum SituacaoBemPatrimonial). */
const SITUACAO_LABEL: Record<SituacaoBemPatrimonial, string> = {
  EmIncorporacao: 'Em incorporação',
  Tombado: 'Tombado',
  Cedido: 'Cedido',
  Baixada: 'Baixado',
  Alienada: 'Alienado',
};

export function situacaoLabel(situacao: SituacaoBemPatrimonial): string {
  return SITUACAO_LABEL[situacao] ?? situacao;
}

/** Mapeia a situação do bem para a variante semântica da Tag (cor). */
export function situacaoTagVariant(situacao: SituacaoBemPatrimonial): TagVariant {
  switch (situacao) {
    case 'EmIncorporacao':
      return 'info';
    case 'Tombado':
      return 'success';
    case 'Cedido':
      return 'warning';
    case 'Baixada':
    case 'Alienada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Conjunto "ativo no acervo" (admite reavaliação/impairment/transferência). */
export function ativoNoAcervo(situacao: SituacaoBemPatrimonial): boolean {
  return situacao === 'Tombado' || situacao === 'Cedido';
}

/** Conjunto "encerrado" (Baixada/Alienada) — não admite novas transições. */
export function encerrado(situacao: SituacaoBemPatrimonial): boolean {
  return situacao === 'Baixada' || situacao === 'Alienada';
}

/** Opções do tipo de bem (TipoBem: 1 = Móvel, 2 = Imóvel). */
export const TIPO_BEM_OPCOES: SelectOption[] = [
  { value: '1', label: 'Móvel' },
  { value: '2', label: 'Imóvel' },
];

/** Opções de filtro por situação do bem (value = nome do enum no contrato). */
export const SITUACAO_BEM_OPCOES: SelectOption[] = [
  { value: 'EmIncorporacao', label: 'Em incorporação' },
  { value: 'Tombado', label: 'Tombado' },
  { value: 'Cedido', label: 'Cedido' },
  { value: 'Baixada', label: 'Baixado' },
  { value: 'Alienada', label: 'Alienado' },
];

export function tipoBemNumero(value: string): TipoBemNumero {
  return value === '2' ? 2 : 1;
}

/**
 * Motivos de baixa (enum MotivoBaixa serializado por inteiro no payload).
 * Mantido parametrizável conforme MCASP (inservível, perda, alienação, doação).
 */
export const MOTIVO_BAIXA_OPCOES: SelectOption[] = [
  { value: '1', label: 'Inservível / obsoleto' },
  { value: '2', label: 'Perda / extravio' },
  { value: '3', label: 'Sinistro / dano irreparável' },
  { value: '4', label: 'Doação' },
  { value: '5', label: 'Alienação' },
];

const GUID_VAZIO = '00000000-0000-0000-0000-000000000000';

/** Validação leve de GUID (não vazio + formato canônico). */
export function guidInvalido(valor: string): boolean {
  const v = valor.trim();
  if (v === '' || v === GUID_VAZIO) return true;
  return !/^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/.test(v);
}

/** Competência atual (ano/mês) — pré-preenchimento do formulário de depreciação. */
export function competenciaAtual(): { ano: number; mes: number } {
  const agora = new Date();
  return { ano: agora.getFullYear(), mes: agora.getMonth() + 1 };
}
