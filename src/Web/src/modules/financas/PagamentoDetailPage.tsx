// Detalhe de uma Ordem de Pagamento (GET /ordens-pagamento/{id}) + ações Efetuar
// (baixa financeira) e Cancelar, com confirmação inline. Lista os itens (liquidações).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  DataTable,
  PageHeader,
  QueryState,
  Tag,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useCancelarOrdem, useEfetuarPagamento, useOrdemDePagamento } from './financas.api';
import type { ItemPagamento, OrdemDePagamentoResumo } from './financas.api';
import { mensagemErro, situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';

type Confirmacao = 'efetuar' | 'cancelar' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

const COLUNAS_ITENS: Column<ItemPagamento>[] = [
  { key: 'liquidacao', header: 'Liquidação', render: (i) => i.liquidacaoId },
  { key: 'valor', header: 'Valor', align: 'end', render: (i) => formatarMoeda(i.valor) },
];

export function PagamentoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const toast = useToast();
  const query = useOrdemDePagamento(id);
  const efetuar = useEfetuarPagamento(id);
  const cancelar = useCancelarOrdem(id);
  const [confirmando, setConfirmando] = useState<Confirmacao>(null);

  function aoEfetuar(): void {
    efetuar.mutate(undefined, {
      onSuccess: () => {
        toast.success('Pagamento efetuado.', 'Sucesso');
        setConfirmando(null);
      },
      onError: (error) => toast.error(mensagemErro(error, 'Não foi possível efetuar o pagamento.')),
    });
  }

  function aoCancelar(): void {
    cancelar.mutate(undefined, {
      onSuccess: () => {
        toast.success('Ordem cancelada.', 'Sucesso');
        setConfirmando(null);
      },
      onError: (error) => toast.error(mensagemErro(error, 'Não foi possível cancelar a ordem.')),
    });
  }

  return (
    <>
      <PageHeader
        title="Detalhe da ordem de pagamento"
        actions={
          <Link className="br-button secondary" to="/financas/pagamentos">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <FinancasSubNav />

      <QueryState<OrdemDePagamentoResumo>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(ordem) => (
          <>
            <Card
              className="mb-4"
              header={
                <div className="d-flex justify-content-between align-items-center flex-wrap">
                  <strong>Ordem nº {ordem.numero}</strong>
                  <Tag variant={situacaoTagVariant(ordem.situacao)}>{ordem.situacao}</Tag>
                </div>
              }
              footer={
                <Can permission="financas.gerenciar">
                  {confirmando ? (
                    <Alert variant="warning" title="Confirmar ação?">
                      {confirmando === 'efetuar'
                        ? 'Efetuar o pagamento realiza a baixa financeira da ordem.'
                        : 'Cancelar a ordem reverte os efeitos e libera as liquidações.'}
                      <div className="d-flex justify-content-end mt-2" style={{ gap: '0.5rem' }}>
                        <Button variant="secondary" onClick={() => setConfirmando(null)}
                          disabled={efetuar.isPending || cancelar.isPending}>
                          Voltar
                        </Button>
                        {confirmando === 'efetuar' ? (
                          <Button variant="primary" onClick={aoEfetuar} loading={efetuar.isPending}>
                            Efetuar
                          </Button>
                        ) : (
                          <Button variant="danger" onClick={aoCancelar} loading={cancelar.isPending}>
                            Cancelar ordem
                          </Button>
                        )}
                      </div>
                    </Alert>
                  ) : (
                    <div className="d-flex justify-content-end" style={{ gap: '0.5rem' }}>
                      <Button variant="primary" onClick={() => setConfirmando('efetuar')}>
                        Efetuar pagamento
                      </Button>
                      <Button variant="danger" onClick={() => setConfirmando('cancelar')}>
                        Cancelar ordem
                      </Button>
                    </div>
                  )}
                </Can>
              }
            >
              <dl className="row">
                <Campo rotulo="Valor total">{formatarMoeda(ordem.valorTotal)}</Campo>
                <Campo rotulo="Data do pagamento">{formatarData(ordem.dataPagamento)}</Campo>
                <Campo rotulo="Conta bancária">{ordem.contaBancaria}</Campo>
                <Campo rotulo="Identificador">{ordem.id}</Campo>
              </dl>
            </Card>

            <Card header={<strong>Itens da ordem</strong>}>
              <DataTable
                caption={`Itens da ordem ${ordem.numero}`}
                columns={COLUNAS_ITENS}
                rows={ordem.itens}
                rowKey={(i) => i.liquidacaoId}
              />
            </Card>
          </>
        )}
      </QueryState>
    </>
  );
}
