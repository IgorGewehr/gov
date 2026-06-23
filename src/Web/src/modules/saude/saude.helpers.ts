// Helpers de apresentação compartilhados pelas telas do módulo Saúde.
import type { TagVariant } from '../../components/ui';
import type { ModalidadeAtendimento, Prioridade, Sexo } from './api';

/** Mapeia a situação de um atendimento para a variante semântica da Tag. */
export function situacaoAtendimentoVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'EmAndamento':
      return 'warning';
    case 'Assinado':
      return 'info';
    case 'Compartilhado':
      return 'success';
    case 'Cancelado':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação de uma solicitação de regulação para a variante da Tag. */
export function situacaoRegulacaoVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Solicitada':
      return 'warning';
    case 'Autorizada':
    case 'Executada':
      return 'success';
    case 'Devolvida':
      return 'info';
    case 'Negada':
    case 'Cancelada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a prioridade da regulação para a variante da Tag (escalonamento de risco). */
export function prioridadeVariant(prioridade: string): TagVariant {
  switch (prioridade) {
    case 'Eletiva':
      return 'default';
    case 'Prioritaria':
      return 'info';
    case 'Urgente':
      return 'warning';
    case 'Emergencia':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação cadastral do paciente para a variante semântica da Tag. */
export function situacaoPacienteVariant(situacao: string): TagVariant {
  return situacao === 'Ativo' ? 'success' : 'danger';
}

/**
 * Opções de Situação cadastral do paciente para o filtro de busca. O `value` é o
 * nome do enum serializado (igualdade exata esperada pelo backend).
 */
export const opcoesSituacaoPaciente: { value: string; label: string }[] = [
  { value: 'Ativo', label: 'Ativo' },
  { value: 'Inativo', label: 'Inativo' },
];

/** Mapeia a situação cadastral (Ativo/Inativo) de estabelecimento/profissional para a Tag. */
export function situacaoCadastroVariant(situacao: string): TagVariant {
  return situacao === 'Ativo' ? 'success' : 'danger';
}

/**
 * Opções de Situação cadastral (Ativo/Inativo) para os filtros de estabelecimento e
 * profissional. O `value` é o nome do enum serializado (igualdade exata no backend).
 */
export const opcoesSituacaoCadastro: { value: string; label: string }[] = [
  { value: 'Ativo', label: 'Ativo' },
  { value: 'Inativo', label: 'Inativo' },
];

/**
 * Tipos de estabelecimento (CNES — TipoEstabelecimento). O `value` é o NOME do enum
 * (o backend faz parse por nome), o `label` é a descrição amigável.
 */
export const opcoesTipoEstabelecimento: { value: string; label: string }[] = [
  { value: 'Ubs', label: 'Unidade Básica de Saúde (UBS)' },
  { value: 'Upa', label: 'Unidade de Pronto Atendimento (UPA)' },
  { value: 'Hospital', label: 'Hospital' },
  { value: 'Caps', label: 'Centro de Atenção Psicossocial (CAPS)' },
  { value: 'Farmacia', label: 'Farmácia / Dispensação' },
  { value: 'UnidadeSaudeFamilia', label: 'Unidade de Saúde da Família' },
  { value: 'CentroEspecialidades', label: 'Centro de Especialidades / Policlínica' },
  { value: 'Laboratorio', label: 'Laboratório' },
  { value: 'Outro', label: 'Outro' },
];

/**
 * Conselhos de classe (TipoConselho). O `value` é o CÓDIGO numérico (RegistroConselhoDto.Tipo),
 * conforme esperado pelo backend.
 */
export const opcoesConselho: { value: string; label: string }[] = [
  { value: '1', label: 'CRM — Medicina' },
  { value: '2', label: 'COREN — Enfermagem' },
  { value: '3', label: 'CRO — Odontologia' },
  { value: '4', label: 'CRF — Farmácia' },
  { value: '5', label: 'CRP — Psicologia' },
  { value: '6', label: 'CREFITO — Fisioterapia/T.O.' },
  { value: '7', label: 'CRN — Nutrição' },
  { value: '8', label: 'CRESS — Serviço Social' },
  { value: '99', label: 'Outro' },
];

/** Opções de Sexo (CADSUS) para selects, no formato { value, label }. */
export const opcoesSexo: { value: string; label: string }[] = [
  { value: '1', label: 'Feminino' },
  { value: '2', label: 'Masculino' },
  { value: '9', label: 'Ignorado' },
];

/** Opções de Modalidade de atendimento (Lei 14.510/2022). */
export const opcoesModalidade: { value: string; label: string }[] = [
  { value: '1', label: 'Presencial' },
  { value: '2', label: 'Teleconsulta' },
];

/** Opções de Prioridade da regulação (classificação de risco). */
export const opcoesPrioridade: { value: string; label: string }[] = [
  { value: '1', label: 'Eletiva' },
  { value: '2', label: 'Prioritária' },
  { value: '3', label: 'Urgente' },
  { value: '4', label: 'Emergência' },
];

/** Converte o valor textual de um <select> em Sexo tipado (default: 9 — Ignorado). */
export function paraSexo(valor: string): Sexo {
  return valor === '1' || valor === '2' ? (Number(valor) as Sexo) : 9;
}

/** Converte o valor textual de um <select> em Modalidade tipada (default: 1 — Presencial). */
export function paraModalidade(valor: string): ModalidadeAtendimento {
  return valor === '2' ? 2 : 1;
}

/** Converte o valor textual de um <select> em Prioridade tipada (default: 1 — Eletiva). */
export function paraPrioridade(valor: string): Prioridade {
  const n = Number(valor);
  return n >= 1 && n <= 4 ? (n as Prioridade) : 1;
}

// --- Guardas da máquina de estados do Atendimento (espelham o domínio) ---

/** Atendimento editável (em andamento) — admite evolução, assinatura e cancelamento. */
export function atendimentoEmAndamento(situacao: string): boolean {
  return situacao === 'EmAndamento';
}

/** Atendimento assinado/compartilhado — admite adendo, RNDS e SISAB (imutável). */
export function atendimentoAssinado(situacao: string): boolean {
  return situacao === 'Assinado' || situacao === 'Compartilhado';
}

/** Atendimento já compartilhado na RNDS. */
export function atendimentoCompartilhado(situacao: string): boolean {
  return situacao === 'Compartilhado';
}

// --- Guardas da máquina de estados da Regulação (espelham o domínio) ---

/** Solicitação em análise = { Solicitada, Devolvida } — admite autorizar/negar/devolver/cancelar. */
export function regulacaoEmAnalise(situacao: string): boolean {
  return situacao === 'Solicitada' || situacao === 'Devolvida';
}

/** Solicitação autorizada — admite execução. */
export function regulacaoAutorizada(situacao: string): boolean {
  return situacao === 'Autorizada';
}
