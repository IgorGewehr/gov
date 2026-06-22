// Tela de DETALHE de um item de almoxarifado (ItemEstoque). Reúne:
//   - [Query ObterItemEstoque §6.1] cabeçalho com dados/mensuração do item;
//   - [Query ListarMovimentosDoItem §6.3] extrato de movimentos por período (DataTable);
//   - ações WIRED de TODOS os commands de transição/operação:
//       RegistrarEntrada, AtenderRequisicao, AjustarVrl, ReclassificarAbc, InativarItem.
// Ações de movimentação só aparecem com item Ativo (I-9). Inativar é destrutivo (Modal de confirmação).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryState,
  Tag,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useItemEstoque, useMovimentosDoItem } from './itemestoque.api';
import type { ItemEstoqueDetalhe, MovimentoResumo } from './itemestoque.api';
import {
  classificacaoAbcLabel,
  classificacaoAbcTagVariant,
  hojeIso,
  metodoCusteioLabel,
  situacaoTagVariant,
  tipoMovimentoLabel,
  tipoMovimentoTagVariant,
} from './itemEstoque.helpers';
import { ItemEstoqueEntradaModal } from './ItemEstoqueEntradaModal';
import { ItemEstoqueRequisicaoModal } from './ItemEstoqueRequisicaoModal';
import { ItemEstoqueVrlModal } from './ItemEstoqueVrlModal';
import { ItemEstoqueAbcModal } from './ItemEstoqueAbcModal';
import { ItemEstoqueInativarModal } from './ItemEstoqueInativarModal';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

type ModalAtivo = 'entrada' | 'requisicao' | 'vrl' | 'abc' | 'inativar' | null;

function primeiroDiaDoMes(): string {
  const agora = new Date();
  return new Date(agora.getFullYear(), agora.getMonth(), 1).toISOString().slice(0, 10);
}

export function ItemEstoqueDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useItemEstoque(id);
  const [modal, setModal] = useState<ModalAtivo>(null);

  // Filtro de período do extrato de movimentos.
  const [de, setDe] = useState(primeiroDiaDoMes());
  const [ate, setAte] = useState(hojeIso());
  const movimentos = useMovimentosDoItem(id, de, ate, id.trim().length > 0);

  function aplicarPeriodo(event: FormEvent): void {
    event.preventDefault();
    movimentos.refetch();
  }

  const colunasMovimentos: Column<MovimentoResumo>[] = [
    {
      key: 'data',
      header: 'Data',
      sortAccessor: (m) => m.data,
      render: (m) => formatarData(m.data),
    },
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (m) => m.tipo,
      render: (m) => <Tag variant={tipoMovimentoTagVariant(m.tipo)}>{tipoMovimentoLabel(m.tipo)}</Tag>,
    },
    {
      key: 'quantidade',
      header: 'Quantidade',
      align: 'end',
      sortAccessor: (m) => m.quantidade,
      render: (m) => m.quantidade,
    },
    {
      key: 'valorUnitario',
      header: 'Valor unitário',
      align: 'end',
      sortAccessor: (m) => m.valorUnitario,
      render: (m) => formatarMoeda(m.valorUnitario),
    },
    { key: 'documento', header: 'Documento', render: (m) => m.documento || '—' },
  ];

  return (
    <>
      <PageHeader
        title="Detalhe do item de almoxarifado"
        actions={
          <Link className="br-button secondary" to="/patrimonio">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ItemEstoqueDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-box-open"
            title="Item não encontrado"
            description="Verifique o identificador informado."
          />
        }
      >
        {(item) => {
          const ativo = item.situacao === 'Ativo';
          return (
            <>
              <Card
                className="mb-4"
                header={
                  <div className="d-flex justify-content-between align-items-center flex-wrap">
                    <strong>
                      {item.codigo} — {item.descricao}
                    </strong>
                    <Tag variant={situacaoTagVariant(item.situacao)}>{item.situacao}</Tag>
                  </div>
                }
                footer={
                  <Can permission="patrimonio.gerenciar">
                  <div className="d-flex flex-wrap" style={{ gap: 'var(--spacing-scale-1x)' }}>
                    <Button variant="primary" onClick={() => setModal('entrada')} disabled={!ativo}>
                      <i className="fas fa-arrow-down" aria-hidden="true" /> Registrar entrada
                    </Button>
                    <Button variant="secondary" onClick={() => setModal('requisicao')} disabled={!ativo}>
                      <i className="fas fa-arrow-up" aria-hidden="true" /> Atender requisição
                    </Button>
                    <Button variant="secondary" onClick={() => setModal('vrl')} disabled={!ativo}>
                      <i className="fas fa-scale-balanced" aria-hidden="true" /> Ajustar VRL
                    </Button>
                    <Button variant="secondary" onClick={() => setModal('abc')} disabled={!ativo}>
                      <i className="fas fa-layer-group" aria-hidden="true" /> Reclassificar ABC
                    </Button>
                    <Button variant="danger" onClick={() => setModal('inativar')} disabled={!ativo}>
                      <i className="fas fa-ban" aria-hidden="true" /> Inativar
                    </Button>
                  </div>
                  </Can>
                }
              >
                <dl className="row">
                  <Campo rotulo="Saldo disponível">
                    {item.saldo} {item.unidadeMedida}
                  </Campo>
                  <Campo rotulo="Ponto de pedido">
                    {item.pontoPedido} {item.unidadeMedida}
                  </Campo>
                  <Campo rotulo="Método de custeio">{metodoCusteioLabel(item.metodoCusteio)}</Campo>
                  <Campo rotulo="Custo médio">{formatarMoeda(item.custoMedio)}</Campo>
                  <Campo rotulo="Classificação ABC">
                    <Tag variant={classificacaoAbcTagVariant(item.classificacaoAbc)}>
                      {classificacaoAbcLabel(item.classificacaoAbc)}
                    </Tag>
                  </Campo>
                  <Campo rotulo="Unidade de medida">{item.unidadeMedida}</Campo>
                </dl>
              </Card>

              <Card header={<strong>Extrato de movimentos</strong>}>
                <form className="br-form mb-3" onSubmit={aplicarPeriodo}>
                  <div className="row align-items-end">
                    <div className="col-sm">
                      <FormField label="De">
                        {({ id: campoId, describedBy }) => (
                          <Input
                            id={campoId}
                            type="date"
                            aria-describedby={describedBy}
                            value={de}
                            onChange={(e) => setDe(e.target.value)}
                          />
                        )}
                      </FormField>
                    </div>
                    <div className="col-sm">
                      <FormField label="Até">
                        {({ id: campoId, describedBy }) => (
                          <Input
                            id={campoId}
                            type="date"
                            aria-describedby={describedBy}
                            value={ate}
                            onChange={(e) => setAte(e.target.value)}
                          />
                        )}
                      </FormField>
                    </div>
                    <div className="col-auto mb-3">
                      <Button variant="secondary" type="submit" loading={movimentos.isFetching}>
                        Filtrar
                      </Button>
                    </div>
                  </div>
                </form>

                <DataTable
                  caption={`Movimentos do item ${item.codigo} no período`}
                  columns={colunasMovimentos}
                  rows={movimentos.data}
                  rowKey={(m) => m.id}
                  loading={movimentos.isLoading}
                  error={movimentos.isError ? errorMessage(movimentos.error) : null}
                  empty={
                    <EmptyState
                      icon="fas fa-list"
                      title="Nenhum movimento no período"
                      description="Ajuste o período ou registre entradas/saídas."
                    />
                  }
                />
              </Card>

              <ItemEstoqueEntradaModal
                open={modal === 'entrada'}
                onClose={() => setModal(null)}
                itemId={item.id}
                codigoItem={item.codigo}
              />
              <ItemEstoqueRequisicaoModal
                open={modal === 'requisicao'}
                onClose={() => setModal(null)}
                itemId={item.id}
                saldoAtual={item.saldo}
                pontoPedido={item.pontoPedido}
                unidadeMedida={item.unidadeMedida}
              />
              <ItemEstoqueVrlModal
                open={modal === 'vrl'}
                onClose={() => setModal(null)}
                itemId={item.id}
                custoMedio={item.custoMedio}
              />
              <ItemEstoqueAbcModal
                open={modal === 'abc'}
                onClose={() => setModal(null)}
                itemId={item.id}
                classeAtual={item.classificacaoAbc}
              />
              <ItemEstoqueInativarModal
                open={modal === 'inativar'}
                onClose={() => setModal(null)}
                itemId={item.id}
                codigoItem={item.codigo}
                saldoAtual={item.saldo}
                unidadeMedida={item.unidadeMedida}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
