// Detalhe de um Empenho (GET /empenhos/{id}) + ações do ciclo da despesa:
// Anular (total/parcial) e Liquidar (abre o formulário de liquidação com o empenho).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, PageHeader, QueryState, Tag, Toolbar } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { useAnularEmpenho, useEmpenho } from './financas.api';
import type { EmpenhoResumo } from './financas.api';
import { situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';
import { ValorAcaoModal } from './ValorAcaoModal';
import { LiquidacaoFormModal } from './LiquidacaoFormModal';

type Acao = 'anular' | 'liquidar' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function EmpenhoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useEmpenho(id);
  const anular = useAnularEmpenho(id);
  const [acao, setAcao] = useState<Acao>(null);

  return (
    <>
      <PageHeader
        title="Detalhe do empenho"
        actions={
          <Link className="br-button secondary" to="/financas/empenhos">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <FinancasSubNav />

      <QueryState<EmpenhoResumo>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(empenho) => (
          <>
            <Card
              header={
                <div className="d-flex justify-content-between align-items-center flex-wrap">
                  <strong>Empenho nº {empenho.numero}</strong>
                  <Tag variant={situacaoTagVariant(empenho.situacao)}>{empenho.situacao}</Tag>
                </div>
              }
              footer={
                <Can permission="financas.gerenciar">
                  <Toolbar>
                    <Button variant="secondary" onClick={() => setAcao('liquidar')}>
                      Liquidar
                    </Button>
                    <Button variant="danger" onClick={() => setAcao('anular')}>
                      Anular
                    </Button>
                  </Toolbar>
                </Can>
              }
            >
              <dl className="row">
                <Campo rotulo="Credor">{empenho.credorNome}</Campo>
                <Campo rotulo="Documento do credor">{empenho.credorDocumento}</Campo>
                <Campo rotulo="Exercício">{empenho.exercicio}</Campo>
                <Campo rotulo="Valor empenhado">{formatarMoeda(empenho.valorEmpenhado)}</Campo>
                <Campo rotulo="Valor anulado">{formatarMoeda(empenho.valorAnulado)}</Campo>
                <Campo rotulo="Valor liquidado">{formatarMoeda(empenho.valorLiquidado)}</Campo>
                <Campo rotulo="Valor pago">{formatarMoeda(empenho.valorPago)}</Campo>
                <Campo rotulo="Saldo a liquidar">{formatarMoeda(empenho.saldoALiquidar)}</Campo>
                <Campo rotulo="Saldo a pagar">{formatarMoeda(empenho.saldoAPagar)}</Campo>
                <Campo rotulo="Dotação">
                  <Link to={`/financas/dotacoes/${empenho.dotacaoId}`}>{empenho.dotacaoId}</Link>
                </Campo>
              </dl>
            </Card>

            <ValorAcaoModal
              open={acao === 'anular'}
              onClose={() => setAcao(null)}
              title="Anular empenho"
              label="Valor a anular (R$)"
              help="Deixe em branco para anular o saldo total do empenho."
              confirmLabel="Anular"
              confirmVariant="danger"
              permiteVazio
              pending={anular.isPending}
              onConfirm={(valor) => anular.mutateAsync(valor)}
              mensagemSucesso="Empenho anulado."
              mensagemErroPadrao="Não foi possível anular o empenho."
            />
            <LiquidacaoFormModal
              open={acao === 'liquidar'}
              onClose={() => setAcao(null)}
              empenhoIdInicial={empenho.id}
            />
          </>
        )}
      </QueryState>
    </>
  );
}
