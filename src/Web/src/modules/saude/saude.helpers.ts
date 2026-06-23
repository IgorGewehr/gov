// Helpers de apresentação compartilhados pelas telas do módulo Saúde.
import type { TagVariant } from '../../components/ui';
import type {
  ModalidadeAtendimento,
  Prioridade,
  Sexo,
  OrigemCancelamento,
  PrioridadeAgendamento,
  TipoAtendimentoAgenda,
} from './api';

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

// --- Agendamento / Agenda / Fila de espera ---

/** Mapeia a situação do agendamento (nome do enum) para a variante da Tag. */
export function situacaoAgendamentoVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Marcado':
      return 'warning';
    case 'Confirmado':
      return 'info';
    case 'Realizado':
      return 'success';
    case 'Cancelado':
    case 'Falta':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação da grade de disponibilidade para a variante da Tag. */
export function situacaoAgendaVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Rascunho':
      return 'default';
    case 'Aberta':
      return 'success';
    case 'Bloqueada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situação de uma entrada da fila de espera para a variante da Tag. */
export function situacaoFilaVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Aguardando':
      return 'warning';
    case 'Convocado':
      return 'info';
    case 'Atendido':
      return 'success';
    case 'Removido':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a prioridade do agendamento/fila (nome do enum) para a variante da Tag. */
export function prioridadeAgendamentoVariant(prioridade: string): TagVariant {
  switch (prioridade) {
    case 'Eletiva':
      return 'default';
    case 'Prioritaria':
      return 'info';
    case 'Urgente':
      return 'danger';
    default:
      return 'default';
  }
}

/** Opções de natureza do atendimento agendado (TipoAtendimentoAgenda). */
export const opcoesTipoAgenda: { value: string; label: string }[] = [
  { value: '1', label: 'Consulta' },
  { value: '2', label: 'Exame' },
];

/** Opções de prioridade do agendamento (PrioridadeAgendamento). */
export const opcoesPrioridadeAgendamento: { value: string; label: string }[] = [
  { value: '1', label: 'Eletiva' },
  { value: '2', label: 'Prioritária' },
  { value: '3', label: 'Urgente' },
];

/** Opções de situação do agendamento (nome do enum — igualdade exata no backend). */
export const opcoesSituacaoAgendamento: { value: string; label: string }[] = [
  { value: 'Marcado', label: 'Marcado' },
  { value: 'Confirmado', label: 'Confirmado' },
  { value: 'Realizado', label: 'Realizado' },
  { value: 'Cancelado', label: 'Cancelado' },
  { value: 'Falta', label: 'Falta' },
];

/** Opções de situação da fila de espera (nome do enum). */
export const opcoesSituacaoFila: { value: string; label: string }[] = [
  { value: 'Aguardando', label: 'Aguardando' },
  { value: 'Convocado', label: 'Convocado' },
  { value: 'Atendido', label: 'Atendido' },
  { value: 'Removido', label: 'Removido' },
];

/** Opções de origem do cancelamento (OrigemCancelamento). */
export const opcoesOrigemCancelamento: { value: string; label: string }[] = [
  { value: '1', label: 'Paciente' },
  { value: '2', label: 'Unidade/Gestão' },
  { value: '3', label: 'Profissional' },
];

/** Converte o valor de um <select> em TipoAtendimentoAgenda (default: 1 — Consulta). */
export function paraTipoAgenda(valor: string): TipoAtendimentoAgenda {
  return valor === '2' ? 2 : 1;
}

/** Converte o valor de um <select> em PrioridadeAgendamento (default: 1 — Eletiva). */
export function paraPrioridadeAgendamento(valor: string): PrioridadeAgendamento {
  const n = Number(valor);
  return n === 2 || n === 3 ? (n as PrioridadeAgendamento) : 1;
}

/** Converte o valor de um <select> em OrigemCancelamento (default: 2 — Unidade). */
export function paraOrigemCancelamento(valor: string): OrigemCancelamento {
  const n = Number(valor);
  return n === 1 || n === 3 ? (n as OrigemCancelamento) : 2;
}

/** Agendamento ativo (Marcado/Confirmado) — admite cancelar/falta/realizar. */
export function agendamentoAtivo(situacao: string): boolean {
  return situacao === 'Marcado' || situacao === 'Confirmado';
}

/** Agendamento somente marcado — admite confirmação. */
export function agendamentoMarcado(situacao: string): boolean {
  return situacao === 'Marcado';
}

/** Entrada da fila aguardando — admite convocação. */
export function filaAguardando(situacao: string): boolean {
  return situacao === 'Aguardando';
}

/** Entrada da fila ainda não terminal — admite remoção. */
export function filaRemovivel(situacao: string): boolean {
  return situacao === 'Aguardando' || situacao === 'Convocado';
}

// --- Farmácia (HÓRUS) ---

/** Mapeia a situação da dispensação (nome do enum) para a variante da Tag. */
export function situacaoDispensacaoVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Efetivada':
      return 'success';
    case 'Estornada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Dispensação efetivada (não estornada) — admite estorno. */
export function dispensacaoEfetivada(situacao: string): boolean {
  return situacao === 'Efetivada';
}

/**
 * Forma farmacêutica (FormaFarmaceutica). O `value` é o CÓDIGO numérico do enum
 * (o backend desserializa por código), o `label` é a descrição amigável.
 */
export const opcoesFormaFarmaceutica: { value: string; label: string }[] = [
  { value: '1', label: 'Comprimido' },
  { value: '2', label: 'Cápsula' },
  { value: '3', label: 'Solução / xarope oral' },
  { value: '4', label: 'Injetável (ampola/frasco)' },
  { value: '5', label: 'Pomada / creme' },
  { value: '99', label: 'Outra' },
];

/** Unidade de medida do estoque (UnidadeMedidaMedicamento). `value` = código numérico. */
export const opcoesUnidadeMedida: { value: string; label: string }[] = [
  { value: '1', label: 'Unidade' },
  { value: '2', label: 'Mililitro (mL)' },
  { value: '3', label: 'Grama (g)' },
  { value: '4', label: 'Caixa / embalagem' },
];

/** Classe de controle especial (TipoControleSngpc — Portaria 344/1998). `value` = código. */
export const opcoesControleSngpc: { value: string; label: string }[] = [
  { value: '0', label: 'Sem controle especial' },
  { value: '1', label: 'Antimicrobiano (RDC 471/2021)' },
  { value: '2', label: 'Controle especial C1' },
  { value: '3', label: 'Entorpecente/psicotrópico (Lista A)' },
  { value: '4', label: 'Psicotrópico (Lista B)' },
];

// --- Imunização (SI-PNI) ---

/**
 * Tipo de dose no esquema vacinal (TipoDose). `value` é o CÓDIGO numérico do enum
 * (Unica=0, Primeira=1, Segunda=2, Terceira=3, R1=11, R2=12, Anual=20).
 */
export const opcoesTipoDose: { value: string; label: string }[] = [
  { value: '1', label: '1ª dose (D1)' },
  { value: '2', label: '2ª dose (D2)' },
  { value: '3', label: '3ª dose (D3)' },
  { value: '11', label: '1º reforço (R1)' },
  { value: '12', label: '2º reforço (R2)' },
  { value: '20', label: 'Dose anual' },
  { value: '0', label: 'Dose única' },
];

/**
 * Situação vacinal do paciente para um imunobiológico, derivada da carteira no
 * cliente (o backend não consolida no DTO da carteira): há próxima dose aprazada
 * vencida (Atrasado), futura (EmDia) ou nenhuma pendência (Completo).
 */
export type SituacaoVacinalNome = 'Completo' | 'EmDia' | 'Atrasado';

/** Mapeia a situação vacinal consolidada para a variante da Tag. */
export function situacaoVacinalVariant(situacao: SituacaoVacinalNome): TagVariant {
  switch (situacao) {
    case 'Completo':
      return 'success';
    case 'EmDia':
      return 'info';
    case 'Atrasado':
      return 'danger';
    default:
      return 'default';
  }
}

/**
 * Deriva a situação vacinal de um esquema a partir da próxima dose aprazada (ISO
 * yyyy-mm-dd) e da data de referência (hoje, ISO). Sem aprazamento = Completo;
 * aprazada no passado = Atrasado; no futuro = EmDia.
 */
export function situacaoVacinalDeAprazamento(
  proximaDoseAprazada: string | null | undefined,
  hojeIso: string,
): SituacaoVacinalNome {
  if (!proximaDoseAprazada) return 'Completo';
  return proximaDoseAprazada < hojeIso ? 'Atrasado' : 'EmDia';
}
