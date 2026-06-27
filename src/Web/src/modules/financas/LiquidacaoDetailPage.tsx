// Detalhe de uma Liquidação (GET /liquidacoes/{id}) + ação Estornar (reverte o 2º estágio).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Alert, Button, Card, PageHeader, QueryState, Tag, Toolbar, useToast } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useEstornarLiquidacao, useLiquidacao } from './financas.api';
import type { LiquidacaoResumo } from './financas.api';
import { mensagemErro, situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';
import { RetencaoFormModal } from './RetencaoFormModal';

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
  const [retencaoAberta, setRetencaoAberta] = useState(false);

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
                    <Toolbar className="mt-2">
                      <Button variant="secondary" onClick={() => setConfirmando(false)} disabled={estornar.isPending}>
                        Cancelar
                      </Button>
                      <Button variant="danger" onClick={aoEstornar} loading={estornar.isPending}>
                        Estornar
                      </Button>
                    </Toolbar>
                  </Alert>
                ) : (
                  <Toolbar>
                    <Button variant="danger" onClick={() => setConfirmando(true)}>
                      Estornar
                    </Button>
                  </Toolbar>
                )}
              </Can>
            }
          >
            <dl className="row">
              <Campo rotulo="Empenho">
                <Link to={`/financas/empenhos/${liquidacao.empenhoId}`}>{liquidacao.empenhoId}</Link>
              </Campo>
              <Campo rotulo="Valor bruto">{formatarMoeda(liquidacao.valor)}</Campo>
              <Campo rotulo="Total retido">{formatarMoeda(liquidacao.totalRetido)}</Campo>
              <Campo rotulo="Valor líquido">{formatarMoeda(liquidacao.valorLiquido)}</Campo>
              <Campo rotulo="Valor pago">{formatarMoeda(liquidacao.valorPago)}</Campo>
              <Campo rotulo="Saldo a pagar">{formatarMoeda(liquidacao.saldoAPagar)}</Campo>
              <Campo rotulo="Data da liquidação">{formatarData(liquidacao.dataLiquidacao)}</Campo>
              <Campo rotulo="Identificador">{liquidacao.id}</Campo>
            </dl>

            <div className="d-flex justify-content-between align-items-center mt-3 mb-2">
              <strong>Retenções / consignações</strong>
              <Can permission="financas.gerenciar">
                {liquidacao.valorPago === 0 && liquidacao.situacao !== 'Estornada' ? (
                  <Button variant="secondary" size="sm" onClick={() => setRetencaoAberta(true)}>
                    <i className="fas fa-plus" aria-hidden="true" /> Adicionar retenção
                  </Button>
                ) : null}
              </Can>
            </div>

            {liquidacao.retencoes.length === 0 ? (
              <p className="text-gray-60 text-down-01">Nenhuma retenção apurada nesta liquidação.</p>
            ) : (
              <table className="br-table">
                <thead>
                  <tr>
                    <th scope="col">Natureza</th>
                    <th scope="col">Cód. receita</th>
                    <th scope="col" className="text-right">Valor</th>
                    <th scope="col">Recolhida</th>
                  </tr>
                </thead>
                <tbody>
                  {liquidacao.retencoes.map((r) => (
                    <tr key={r.id}>
                      <td>{r.natureza}</td>
                      <td>{r.codigoReceita ?? '—'}</td>
                      <td className="text-right">{formatarMoeda(r.valor)}</td>
                      <td>
                        <Tag variant={r.recolhida ? 'success' : 'warning'}>{r.recolhida ? 'Sim' : 'Pendente'}</Tag>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Card>
        )}
      </QueryState>

      <RetencaoFormModal open={retencaoAberta} onClose={() => setRetencaoAberta(false)} liquidacaoId={id} />
    </>
  );
}
