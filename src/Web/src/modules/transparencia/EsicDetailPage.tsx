// DETALHE INTERNO de um pedido e-SIC (LAI) — COM dados do solicitante (PII; a leitura
// gera trilha de acesso LG-3 no backend). Exibe situação, PRAZO LEGAL (dias úteis
// restantes), solicitante/pedido e o desfecho registrado. As transições do ciclo vivem
// no <EsicAcoesPanel> (gated transparencia.esic.responder).
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  CardSecao,
  Metrica,
  MetricaGrade,
  QueryState,
  Tag,
} from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { usePedidoSic } from './esic.api';
import type { PedidoSicDetalheInterno } from './esic.api';
import {
  calcularPrazoLegal,
  situacaoPedidoLabel,
  situacaoPedidoTagVariant,
} from './esic.helpers';
import { EsicAcoesPanel } from './EsicAcoesPanel';

export function EsicDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = usePedidoSic(id);

  return (
    <>
      <div className="mb-3">
        <Link className="br-button tertiary small" to="/transparencia/esic">
          <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar à fila
        </Link>
      </div>
      <QueryState
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(pedido) => <Conteudo pedido={pedido} />}
      </QueryState>
    </>
  );
}

function Conteudo({ pedido }: { pedido: PedidoSicDetalheInterno }) {
  const prazoVigente = pedido.prorrogadoAte ?? pedido.prazoResposta;
  const prazo = calcularPrazoLegal(pedido.situacao, prazoVigente);
  const tomPrazo = prazo
    ? prazo.vencido
      ? 'perigo'
      : prazo.diasUteisRestantes <= 3
        ? 'alerta'
        : 'sucesso'
    : 'neutro';

  return (
    <>
      <CardSecao
        titulo={`Pedido ${pedido.protocolo}`}
        subtitulo="Pedido de acesso à informação (LAI Lei 12.527/2011)"
        acao={
          <Tag variant={situacaoPedidoTagVariant(pedido.situacao)}>
            {situacaoPedidoLabel[pedido.situacao]}
          </Tag>
        }
        nota="LGPD: esta tela exibe dados pessoais do solicitante. A abertura do detalhe é registrada na trilha de acesso (base legal: obrigação legal — atender à LAI)."
        className="mb-4"
      >
        <MetricaGrade>
          <Metrica label="Abertura" valor={formatarData(pedido.dataAbertura)} />
          <Metrica
            label="Prazo legal vigente"
            valor={formatarData(prazoVigente)}
            secundario={
              pedido.prorrogadoAte ? 'Prorrogado (+10 dias úteis — art. 11 §2º)' : 'Prazo base'
            }
          />
          <Metrica
            label="Prazo restante"
            valor={prazo ? prazo.rotulo : '—'}
            secundario={prazo ? undefined : 'Pedido já desfechado'}
            tom={tomPrazo}
          />
        </MetricaGrade>

        {prazo?.vencido && (
          <Alert variant="warning" title="Prazo legal vencido" className="mt-3">
            O prazo legal de resposta venceu. Conclua o atendimento (resposta ou indeferimento
            fundamentado) com prioridade — LAI art. 11.
          </Alert>
        )}
      </CardSecao>

      <CardSecao titulo="Solicitante e pedido" className="mb-4">
        <dl className="row mb-0">
          <dt className="col-sm-3">Nome</dt>
          <dd className="col-sm-9">{pedido.solicitanteNome}</dd>
          <dt className="col-sm-3">Documento</dt>
          <dd className="col-sm-9">{pedido.solicitanteDocumento ?? '—'}</dd>
          <dt className="col-sm-3">Contato</dt>
          <dd className="col-sm-9">{pedido.solicitanteContato ?? '—'}</dd>
          <dt className="col-sm-3">Descrição do pedido</dt>
          <dd className="col-sm-9" style={{ whiteSpace: 'pre-wrap' }}>
            {pedido.descricao}
          </dd>
        </dl>
      </CardSecao>

      {(pedido.textoResposta || pedido.fundamentoIndeferimento || pedido.motivoProrrogacao) && (
        <CardSecao titulo="Desfecho registrado" className="mb-4">
          <dl className="row mb-0">
            {pedido.motivoProrrogacao && (
              <>
                <dt className="col-sm-3">Motivo da prorrogação</dt>
                <dd className="col-sm-9" style={{ whiteSpace: 'pre-wrap' }}>
                  {pedido.motivoProrrogacao}
                </dd>
              </>
            )}
            {pedido.textoResposta && (
              <>
                <dt className="col-sm-3">Resposta</dt>
                <dd className="col-sm-9" style={{ whiteSpace: 'pre-wrap' }}>
                  {pedido.textoResposta}
                </dd>
              </>
            )}
            {pedido.fundamentoIndeferimento && (
              <>
                <dt className="col-sm-3">Fundamento do indeferimento</dt>
                <dd className="col-sm-9" style={{ whiteSpace: 'pre-wrap' }}>
                  {pedido.fundamentoIndeferimento}
                </dd>
              </>
            )}
          </dl>
        </CardSecao>
      )}

      <EsicAcoesPanel pedido={pedido} />
    </>
  );
}
