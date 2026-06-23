// Helpers de apresentação da Vigilância Sanitária (VISA): rótulos PT-BR dos enums do
// backend e mapeamento para a variante semântica da Tag (status, não decoração).
import type { SelectOption, TagVariant } from '../../components/ui';
import type {
  ConformidadeItem,
  GrauRiscoSanitario,
  RamoVisa,
  ResultadoInspecao,
  SituacaoAutoVisa,
  SituacaoEstabelecimentoVisa,
  SituacaoInspecao,
  SituacaoLicenca,
  TipoAutoVisa,
} from './vigilancia.api';

export const ramoLabel: Record<RamoVisa, string> = {
  Alimentacao: 'Serviço de alimentação',
  Alimentos: 'Indústria/comércio de alimentos',
  Farmacia: 'Farmácia/drogaria',
  Saude: 'Estabelecimento de saúde',
  Estetica: 'Estética/embelezamento',
  Hospedagem: 'Hospedagem/eventos',
  InstituicaoColetiva: 'Instituição coletiva (creche/ILPI)',
  Outro: 'Outro',
};

export const riscoLabel: Record<GrauRiscoSanitario, string> = {
  Baixo: 'Risco I (baixo)',
  Medio: 'Risco II (médio)',
  Alto: 'Risco III (alto)',
};

export const situacaoEstabLabel: Record<SituacaoEstabelecimentoVisa, string> = {
  Ativo: 'Ativo',
  Inativo: 'Inativo',
  Interditado: 'Interditado',
};

export const situacaoInspecaoLabel: Record<SituacaoInspecao, string> = {
  Aberta: 'Aberta',
  Concluida: 'Concluída',
  Cancelada: 'Cancelada',
};

export const conformidadeLabel: Record<ConformidadeItem, string> = {
  Conforme: 'Conforme',
  NaoConforme: 'Não conforme',
  NaoAplicavel: 'Não se aplica',
};

export const resultadoLabel: Record<ResultadoInspecao, string> = {
  Aprovado: 'Aprovado',
  AprovadoComPendencias: 'Aprovado com pendências',
  Reprovado: 'Reprovado',
};

export const tipoAutoLabel: Record<TipoAutoVisa, string> = {
  Intimacao: 'Auto de intimação',
  Infracao: 'Auto de infração',
  ImposicaoPenalidade: 'Imposição de penalidade',
};

export const situacaoAutoLabel: Record<SituacaoAutoVisa, string> = {
  Lavrado: 'Lavrado',
  DefesaApresentada: 'Defesa apresentada',
  Deferido: 'Deferido',
  Indeferido: 'Indeferido',
  Regularizado: 'Regularizado',
};

export const situacaoLicencaLabel: Record<SituacaoLicenca, string> = {
  Vigente: 'Vigente',
  Vencida: 'Vencida',
  Cassada: 'Cassada',
};

function opcoes<T extends string>(labels: Record<T, string>): SelectOption[] {
  return (Object.keys(labels) as T[]).map((value) => ({ value, label: labels[value] }));
}

export const opcoesRamo = opcoes(ramoLabel);
export const opcoesRisco = opcoes(riscoLabel);
export const opcoesSituacaoEstab = opcoes(situacaoEstabLabel);
export const opcoesSituacaoInspecao = opcoes(situacaoInspecaoLabel);
export const opcoesConformidade = opcoes(conformidadeLabel);
export const opcoesTipoAuto = opcoes(tipoAutoLabel);
export const opcoesSituacaoAuto = opcoes(situacaoAutoLabel);

export function situacaoEstabVariant(s: SituacaoEstabelecimentoVisa): TagVariant {
  switch (s) {
    case 'Ativo':
      return 'success';
    case 'Interditado':
      return 'danger';
    default:
      return 'default';
  }
}

export function riscoVariant(r: GrauRiscoSanitario): TagVariant {
  switch (r) {
    case 'Baixo':
      return 'success';
    case 'Medio':
      return 'info';
    case 'Alto':
      return 'warning';
    default:
      return 'default';
  }
}

export function situacaoInspecaoVariant(s: SituacaoInspecao): TagVariant {
  switch (s) {
    case 'Aberta':
      return 'warning';
    case 'Concluida':
      return 'success';
    case 'Cancelada':
      return 'danger';
    default:
      return 'default';
  }
}

export function conformidadeVariant(c: ConformidadeItem): TagVariant {
  switch (c) {
    case 'Conforme':
      return 'success';
    case 'NaoConforme':
      return 'danger';
    default:
      return 'default';
  }
}

export function resultadoVariant(r: ResultadoInspecao): TagVariant {
  switch (r) {
    case 'Aprovado':
      return 'success';
    case 'AprovadoComPendencias':
      return 'warning';
    case 'Reprovado':
      return 'danger';
    default:
      return 'default';
  }
}

export function situacaoAutoVariant(s: SituacaoAutoVisa): TagVariant {
  switch (s) {
    case 'Lavrado':
      return 'warning';
    case 'DefesaApresentada':
      return 'info';
    case 'Deferido':
    case 'Regularizado':
      return 'success';
    case 'Indeferido':
      return 'danger';
    default:
      return 'default';
  }
}

export function situacaoLicencaVariant(s: SituacaoLicenca): TagVariant {
  switch (s) {
    case 'Vigente':
      return 'success';
    case 'Vencida':
      return 'warning';
    case 'Cassada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Formata um valor monetário (multa) em BRL. */
export function formatarMoeda(valor: number | null | undefined): string {
  if (valor === null || valor === undefined) return '—';
  return valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}
