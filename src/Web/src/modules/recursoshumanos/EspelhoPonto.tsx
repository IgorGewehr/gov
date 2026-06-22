// ESPELHO de ponto de uma apuracao: trabalhado/devido/extras/faltas e o banco de horas
// (anterior, no periodo e atual), com a acao de FECHAMENTO (congela espelho/AEJ; gancho
// para a folha via Outbox). Quando o backend nao devolve os minutos (resposta apenas com
// id), exibe um aviso orientando a conferencia pelo AEJ. Mantido isolado para a pagina de
// operacao do servidor ficar enxuta (< 300 linhas, padrao-ouro).
import { Alert, Button, Tag } from '../../components/ui';
import { Can } from '../../auth/Can';
import type { ApuracaoResultado } from './api';
import { PERM_RH_GERENCIAR, formatarMinutos } from './recursosHumanos.helpers';

export interface EspelhoPontoProps {
  apuracao: ApuracaoResultado;
  fechada: boolean;
  fechando: boolean;
  onFechar: () => void;
}

/** True quando a apuração trouxe ao menos um campo do espelho (além do id). */
function temEspelho(a: ApuracaoResultado): boolean {
  return (
    a.minutosTrabalhados !== undefined ||
    a.minutosDevidos !== undefined ||
    a.minutosExtras !== undefined ||
    a.minutosFalta !== undefined ||
    a.saldoBancoHorasAtualMinutos !== undefined
  );
}

export function EspelhoPonto({ apuracao, fechada, fechando, onFechar }: EspelhoPontoProps) {
  return (
    <>
      <div className="d-flex align-items-center mb-3" style={{ gap: '0.5rem' }}>
        <strong>Espelho de ponto</strong>
        <Tag variant={fechada ? 'success' : 'warning'}>{fechada ? 'Fechada' : 'Aberta'}</Tag>
      </div>

      {!temEspelho(apuracao) && (
        <Alert variant="info" title="Apuração concluída">
          A competência foi apurada (id {apuracao.id}). Gere o AEJ para conferir o espelho
          consolidado da jornada.
        </Alert>
      )}

      <dl className="row">
        <Item rotulo="Minutos trabalhados" minutos={apuracao.minutosTrabalhados} />
        <Item rotulo="Minutos devidos" minutos={apuracao.minutosDevidos} />
        <Item rotulo="Horas extras" minutos={apuracao.minutosExtras} />
        <Item rotulo="Faltas" minutos={apuracao.minutosFalta} />
        <Item rotulo="Banco de horas (anterior)" minutos={apuracao.saldoBancoHorasAnteriorMinutos} />
        <Item rotulo="Saldo no período" minutos={apuracao.saldoPeriodoMinutos} />
        <Item rotulo="Banco de horas (atual)" minutos={apuracao.saldoBancoHorasAtualMinutos} />
      </dl>

      <Can permission={PERM_RH_GERENCIAR}>
        <Button variant="secondary" onClick={onFechar} loading={fechando} disabled={fechada}>
          <i className="fas fa-lock" aria-hidden="true" /> Fechar apuração
        </Button>
      </Can>
      <p className="text-down-01 text-gray-60 mt-3 mb-0">
        O fechamento congela o espelho/AEJ e aciona, via Outbox, a tradução de extras/faltas
        em rubricas na folha.
      </p>
    </>
  );
}

/** Item do espelho: rótulo + duração formatada (oculto quando o backend não a devolve). */
function Item({ rotulo, minutos }: { rotulo: string; minutos: number | undefined }) {
  if (minutos === undefined) return null;
  return (
    <div className="col-sm-6 col-md-4 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{formatarMinutos(minutos)}</dd>
    </div>
  );
}
