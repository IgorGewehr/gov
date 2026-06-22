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

/** Mapeia a situacao de um vereador para a variante semantica da Tag. */
export function situacaoVereadorTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Ativo':
      return 'success';
    case 'Licenciado':
    case 'SuplenteEmExercicio':
      return 'info';
    case 'Afastado':
      return 'warning';
    default:
      return 'default';
  }
}

/** Mapeia a situacao de uma norma para a variante semantica da Tag. */
export function situacaoNormaTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Vigente':
      return 'success';
    case 'Alterada':
      return 'warning';
    case 'Revogada':
      return 'danger';
    default:
      return 'default';
  }
}

/** Mapeia a situacao de uma edicao do Diario para a variante semantica da Tag. */
export function situacaoEdicaoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'EmMontagem':
      return 'info';
    case 'Publicada':
      return 'success';
    default:
      return 'default';
  }
}

/** Mapeia a situacao de uma inscricao na Tribuna para a variante semantica da Tag. */
export function situacaoInscricaoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'EmUsoDaPalavra':
      return 'success';
    case 'Pausada':
      return 'warning';
    case 'Aguardando':
      return 'info';
    case 'Encerrada':
      return 'default';
    default:
      return 'default';
  }
}

/**
 * Formata uma duracao em segundos como mm:ss (ou hh:mm:ss acima de 1h). Aceita
 * valores negativos prefixando com "-" — util para exibir tempo excedente.
 */
export function formatarDuracao(segundos: number): string {
  const negativo = segundos < 0;
  const total = Math.abs(Math.trunc(segundos));
  const h = Math.floor(total / 3600);
  const m = Math.floor((total % 3600) / 60);
  const s = total % 60;
  const pad = (n: number) => String(n).padStart(2, '0');
  const corpo = h > 0 ? `${pad(h)}:${pad(m)}:${pad(s)}` : `${pad(m)}:${pad(s)}`;
  return negativo ? `-${corpo}` : corpo;
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
