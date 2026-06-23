// Helpers de apresentacao compartilhados pelas telas do PBF (condicionalidades).
import type { TagVariant } from '../../../components/ui';
import type {
  EfeitoDescumprimento,
  StatusCondicionalidade,
  TipoCondicionalidade,
} from './pbf.api';

/** Rotulo legivel do eixo de condicionalidade (PT-BR). */
export function tipoCondicionalidadeLabel(tipo: TipoCondicionalidade): string {
  switch (tipo) {
    case 'EducacaoFrequenciaEscolar':
      return 'Educação — frequência escolar';
    case 'SaudeVacinacaoNutricaoInfantil':
      return 'Saúde — vacinação/nutrição infantil';
    case 'SaudePreNatalGestante':
      return 'Saúde — pré-natal de gestante';
    default:
      return tipo;
  }
}

/** Rotulo legivel do status de cumprimento (PT-BR). */
export function statusLabel(status: StatusCondicionalidade): string {
  switch (status) {
    case 'Pendente':
      return 'Pendente';
    case 'Cumprida':
      return 'Cumprida';
    case 'Descumprida':
      return 'Descumprida';
    case 'Justificada':
      return 'Justificada';
    default:
      return status;
  }
}

/** Variante semantica da Tag para o status de cumprimento. */
export function statusTagVariant(status: StatusCondicionalidade): TagVariant {
  switch (status) {
    case 'Cumprida':
      return 'success';
    case 'Descumprida':
      return 'danger';
    case 'Justificada':
      return 'info';
    case 'Pendente':
      return 'warning';
    default:
      return 'default';
  }
}

/** Rotulo legivel do efeito gradativo (PT-BR). */
export function efeitoLabel(efeito: EfeitoDescumprimento): string {
  switch (efeito) {
    case 'Nenhum':
      return 'Sem efeito';
    case 'Advertencia':
      return 'Advertência';
    case 'Bloqueio':
      return 'Bloqueio';
    case 'Suspensao':
      return 'Suspensão';
    default:
      return efeito;
  }
}

/** Variante semantica da Tag para o efeito gradativo (semaforo de gravidade). */
export function efeitoTagVariant(efeito: EfeitoDescumprimento): TagVariant {
  switch (efeito) {
    case 'Nenhum':
      return 'success';
    case 'Advertencia':
      return 'warning';
    case 'Bloqueio':
      return 'warning';
    case 'Suspensao':
      return 'danger';
    default:
      return 'default';
  }
}

/** Opcoes do Select de eixo de condicionalidade. */
export const TIPO_CONDICIONALIDADE_OPTIONS: { value: TipoCondicionalidade; label: string }[] = [
  { value: 'EducacaoFrequenciaEscolar', label: tipoCondicionalidadeLabel('EducacaoFrequenciaEscolar') },
  {
    value: 'SaudeVacinacaoNutricaoInfantil',
    label: tipoCondicionalidadeLabel('SaudeVacinacaoNutricaoInfantil'),
  },
  { value: 'SaudePreNatalGestante', label: tipoCondicionalidadeLabel('SaudePreNatalGestante') },
];

/** Opcoes do Select de status de cumprimento (registro manual pela equipe do CRAS). */
export const STATUS_OPTIONS: { value: StatusCondicionalidade; label: string }[] = [
  { value: 'Pendente', label: statusLabel('Pendente') },
  { value: 'Cumprida', label: statusLabel('Cumprida') },
  { value: 'Descumprida', label: statusLabel('Descumprida') },
];

/** Opcoes do filtro de efeito minimo (busca ativa). Vazio = todos com algum efeito (>= advertencia). */
export const EFEITO_MINIMO_OPTIONS: { value: string; label: string }[] = [
  { value: 'Advertencia', label: 'A partir de advertência' },
  { value: 'Bloqueio', label: 'A partir de bloqueio' },
  { value: 'Suspensao', label: 'Apenas suspensões' },
];

/** Opcoes de mes (1-12) para o Select de competencia. */
export const MES_OPTIONS: { value: string; label: string }[] = [
  { value: '1', label: 'Janeiro' },
  { value: '2', label: 'Fevereiro' },
  { value: '3', label: 'Março' },
  { value: '4', label: 'Abril' },
  { value: '5', label: 'Maio' },
  { value: '6', label: 'Junho' },
  { value: '7', label: 'Julho' },
  { value: '8', label: 'Agosto' },
  { value: '9', label: 'Setembro' },
  { value: '10', label: 'Outubro' },
  { value: '11', label: 'Novembro' },
  { value: '12', label: 'Dezembro' },
];
