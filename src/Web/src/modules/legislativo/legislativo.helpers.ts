// Helpers de apresentacao compartilhados pelas telas do modulo Legislativo.
// As projecoes do backend devolvem a situacao como string (Enum.ToString()),
// entao o mapeamento de variante semantica e feito por nome.
import type { TagVariant } from '../../components/ui';

/** Mapeia a situacao de uma proposicao para a variante semantica da Tag. */
export function situacaoProposicaoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Apresentada':
      return 'info';
    case 'Distribuida':
    case 'EmOrdemDoDia':
      return 'warning';
    case 'Aprovada':
    case 'AutografoEnviado':
      return 'success';
    case 'Rejeitada':
    case 'Arquivada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situacao de uma sessao para a variante semantica da Tag. */
export function situacaoSessaoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Agendada':
      return 'info';
    case 'Aberta':
      return 'success';
    case 'Suspensa':
      return 'warning';
    case 'Encerrada':
      return 'default';
    case 'Cancelada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situacao de uma votacao para a variante semantica da Tag. */
export function situacaoVotacaoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Aberta':
      return 'warning';
    case 'Encerrada':
      return 'success';
    case 'Cancelada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia o resultado de uma votacao para a variante semantica da Tag. */
export function resultadoVotacaoTagVariant(resultado: string | null): TagVariant {
  switch (resultado) {
    case 'Aprovado':
      return 'success';
    case 'Rejeitado':
      return 'danger';
    default:
      return 'default';
  }
}

/** Formata um instante ISO (yyyy-MM-ddTHH:mm:ssZ) em dd/mm/aaaa HH:mm (pt-BR). */
const dataHoraFormat = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

/** Formata data e hora de uma sessao. Devolve travessao quando invalido. */
export function formatarDataHora(iso: string | null | undefined): string {
  if (!iso) return '—';
  const parsed = new Date(iso);
  return Number.isNaN(parsed.getTime()) ? '—' : dataHoraFormat.format(parsed);
}
