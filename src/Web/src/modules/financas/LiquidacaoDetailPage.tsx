// Detalhe de uma Liquidação (GET /liquidacoes/{id}) + ação Estornar (reverte o 2º estágio).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Alert, Button, Card, PageHeader, QueryState, Tag, useToast } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useEstornarLiquidacao, useLiquidacao } from './financas.api';
import type { LiquidacaoResumo } from './financas.api';
import { mensagemErro, situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function LiquidacaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const toast = useToast();
  const query = useLiquidacao(id);
  const estornar = useEstornarLiquidacao(id);
  const [confirmando, setConfirmando] = useState(false);

  function aoEstornar(): void {
    estornar.mutate(undefined, {
      onSuccess: () => {
        toast.success('Liquidação estornada.', 'Sucesso');
        setConfirmando(false);
      },
      onError: (error) => toast.error(mensagemErro(error, 'Não foi possível estornar a liquidação.')),
    });
  }

  return (
    <>
      <PageHeader
        title="Detalhe da liquidação"
        actions={
          <Link className="br-button secondary" to="/financas/liquidacoes">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <FinancasSubNav />

      <QueryState<LiquidacaoResumo>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(liquidacao) => (
          <Card
            header={
              <div className="d-flex justify-content-between align-items-center flex-wrap">
                <strong>Liquidação — {liquidacao.documento}</strong>
                <Tag variant={situacaoTagVariant(liquidacao.situacao)}>{liquidacao.situacao}</Tag>
              </div>
            }
            footer={
              <Can permission="financas.gerenciar">
                {confirmando ? (
                  <Alert variant="warning" title="Confirmar estorno?">
                    O estorno reverte a liquidação e seus efeitos no saldo do empenho.
                    <div className="d-flex justify-content-end mt-2" style={{ gap: '0.5rem' }}>
                      <Button variant="secondary" onClick={() => setConfirmando(false)} disabled={estornar.isPending}>
                        Cancelar
                      </Button>
                      <Button variant="danger" onClick={aoEstornar} loading={estornar.isPending}>
                        Estornar
                      </Button>
                    </div>
                  </Alert>
                ) : (
                  <div className="d-flex justify-content-end">
                    <Button variant="danger" onClick={() => setConfirmando(true)}>
                      Estornar
                    </Button>
                  </div>
                )}
              </Can>
            }
          >
            <dl className="row">
              <Campo rotulo="Empenho">
                <Link to={`/financas/empenhos/${liquidacao.empenhoId}`}>{liquidacao.empenhoId}</Link>
              </Campo>
              <Campo rotulo="Valor">{formatarMoeda(liquidacao.valor)}</Campo>
              <Campo rotulo="Valor pago">{formatarMoeda(liquidacao.valorPago)}</Campo>
              <Campo rotulo="Saldo a pagar">{formatarMoeda(liquidacao.saldoAPagar)}</Campo>
              <Campo rotulo="Data da liquidação">{formatarData(liquidacao.dataLiquidacao)}</Campo>
              <Campo rotulo="Identificador">{liquidacao.id}</Campo>
            </dl>
          </Card>
        )}
      </QueryState>
    </>
  );
}
