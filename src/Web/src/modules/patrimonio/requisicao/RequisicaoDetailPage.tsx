// Tela de DETALHE / FLUXO de uma REQUISIÇÃO de almoxarifado (Onda 3b). Conduz a máquina
// de estados: Solicitado -> (aprovação) -> Aprovado -> (atendimento, baixa parcial) ->
// Atendido; ou Cancelado. Reúne:
//   - [Query ObterPedido] ficha (UO, setor, solicitante, datas) + linhas do pedido;
//   - linhas com Solicitada / Atendida / Pendente (a baixa é min(pendente, saldo) por item);
//   - ações WIRED, gated por situação + permissão: Aprovar, Atender, Cancelar.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
  Toolbar,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarData } from '../../../i18n/format';
import { Can } from '../../../auth/Can';
import { usePedido } from './requisicao.api';
import type { ItemPedidoDto, PedidoRequisicaoDetalhe } from './requisicao.api';
import { situacaoPedidoTagVariant } from './requisicao.helpers';
import { RequisicaoAprovarModal } from './RequisicaoAprovarModal';
import { RequisicaoAtenderModal } from './RequisicaoAtenderModal';
import { RequisicaoCancelarModal } from './RequisicaoCancelarModal';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

type ModalAtivo = 'aprovar' | 'atender' | 'cancelar' | null;

const COLUNAS_ITENS: Column<ItemPedidoDto>[] = [
  { key: 'item', header: 'Item de estoque', render: (i) => i.itemEstoqueId },
  {
    key: 'solicitada',
    header: 'Solicitada',
    align: 'end',
    sortAccessor: (i) => i.quantidadeSolicitada,
    render: (i) => i.quantidadeSolicitada,
  },
  {
    key: 'atendida',
    header: 'Atendida',
    align: 'end',
    sortAccessor: (i) => i.quantidadeAtendida,
    render: (i) => i.quantidadeAtendida,
  },
  {
    key: 'pendente',
    header: 'Pendente',
    align: 'end',
    render: (i) => Math.max(0, i.quantidadeSolicitada - i.quantidadeAtendida),
  },
  {
    key: 'status',
    header: 'Linha',
    render: (i) =>
      i.totalmenteAtendido ? (
        <Tag variant="success">Atendida</Tag>
      ) : (
        <Tag variant="warning">Pendente</Tag>
      ),
  },
];

export function RequisicaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = usePedido(id);
  const [modal, setModal] = useState<ModalAtivo>(null);

  return (
    <>
      <PageHeader
        eyebrow="Patrimônio"
        title="Requisição de almoxarifado"
        actions={
          <Link className="br-button secondary" to="/patrimonio/requisicoes">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<PedidoRequisicaoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-clipboard-list"
            title="Requisição não encontrada"
            description="Verifique o identificador informado."
          />
        }
      >
        {(pedido) => {
          const solicitado = pedido.situacao === 'Solicitado';
          const aprovado = pedido.situacao === 'Aprovado';
          const terminal = pedido.situacao === 'Atendido' || pedido.situacao === 'Cancelado';
          return (
            <>
              <Card
                className="mb-4"
                header={
                  <div className="d-flex justify-content-between align-items-center flex-wrap">
                    <strong>{pedido.setorSolicitante}</strong>
                    <Tag variant={situacaoPedidoTagVariant(pedido.situacao)}>{pedido.situacao}</Tag>
                  </div>
                }
                footer={
                  !terminal ? (
                    <Can permission="patrimonio.gerenciar">
                      <Toolbar>
                        {solicitado && (
                          <Button variant="primary" onClick={() => setModal('aprovar')}>
                            <i className="fas fa-check" aria-hidden="true" /> Aprovar
                          </Button>
                        )}
                        {aprovado && (
                          <Button variant="primary" onClick={() => setModal('atender')}>
                            <i className="fas fa-box-open" aria-hidden="true" /> Atender
                          </Button>
                        )}
                        <Button variant="danger" onClick={() => setModal('cancelar')}>
                          <i className="fas fa-ban" aria-hidden="true" /> Cancelar
                        </Button>
                      </Toolbar>
                    </Can>
                  ) : null
                }
              >
                <dl className="row">
                  <Campo rotulo="UO consumidora">{pedido.unidadeId}</Campo>
                  <Campo rotulo="Solicitante">{pedido.solicitanteId}</Campo>
                  <Campo rotulo="Data do pedido">{formatarData(pedido.data)}</Campo>
                  <Campo rotulo="Itens">{pedido.itens.length}</Campo>
                  <Campo rotulo="Aprovação">
                    {pedido.dataAprovacao ? formatarData(pedido.dataAprovacao) : '—'}
                  </Campo>
                  <Campo rotulo="Atendimento">
                    {pedido.dataAtendimento ? formatarData(pedido.dataAtendimento) : '—'}
                  </Campo>
                  {pedido.justificativa && (
                    <Campo rotulo="Justificativa">{pedido.justificativa}</Campo>
                  )}
                  {pedido.motivoCancelamento && (
                    <Campo rotulo="Motivo do cancelamento">{pedido.motivoCancelamento}</Campo>
                  )}
                </dl>
              </Card>

              <Card header={<strong>Itens da requisição</strong>}>
                <DataTable
                  caption="Linhas do pedido com quantidade solicitada, atendida e pendente"
                  columns={COLUNAS_ITENS}
                  rows={pedido.itens}
                  rowKey={(i) => i.id}
                  empty={
                    <EmptyState
                      icon="fas fa-boxes-stacked"
                      title="Sem itens"
                      description="Esta requisição não possui linhas."
                    />
                  }
                />
              </Card>

              <RequisicaoAprovarModal
                open={modal === 'aprovar'}
                onClose={() => setModal(null)}
                pedidoId={pedido.id}
              />
              <RequisicaoAtenderModal
                open={modal === 'atender'}
                onClose={() => setModal(null)}
                pedidoId={pedido.id}
              />
              <RequisicaoCancelarModal
                open={modal === 'cancelar'}
                onClose={() => setModal(null)}
                pedidoId={pedido.id}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
