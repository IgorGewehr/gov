// Detalhe de uma Dotação (GET /dotacoes/{id}) + ações de movimentação de crédito:
// Reforçar (suplementação) e Anular crédito. Param de rota -> useQuery + QueryState.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, PageHeader, QueryState, Tag, Toolbar } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { useAnularCreditoDotacao, useDotacao, useReforcarDotacao } from './financas.api';
import type { DotacaoResumo } from './financas.api';
import { situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';
import { ValorAcaoModal } from './ValorAcaoModal';

type Acao = 'reforcar' | 'anular' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function DotacaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useDotacao(id);
  const reforcar = useReforcarDotacao(id);
  const anular = useAnularCreditoDotacao(id);
  const [acao, setAcao] = useState<Acao>(null);

  return (
    <>
      <PageHeader
        title="Detalhe da dotação"
        actions={
          <Link className="br-button secondary" to="/financas">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <FinancasSubNav />

      <QueryState<DotacaoResumo>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(dotacao) => (
          <>
            <Card
              header={
                <div className="d-flex justify-content-between align-items-center flex-wrap">
                  <strong>{dotacao.classificacao}</strong>
                  <Tag variant={situacaoTagVariant(dotacao.situacao)}>{dotacao.situacao}</Tag>
                </div>
              }
              footer={
                <Can permission="financas.gerenciar">
                  <Toolbar>
                    <Button variant="secondary" onClick={() => setAcao('reforcar')}>
                      Reforçar
                    </Button>
                    <Button variant="danger" onClick={() => setAcao('anular')}>
                      Anular crédito
                    </Button>
                  </Toolbar>
                </Can>
              }
            >
              <dl className="row">
                <Campo rotulo="Exercício">{dotacao.exercicio}</Campo>
                <Campo rotulo="Valor dotado inicial">{formatarMoeda(dotacao.valorDotadoInicial)}</Campo>
                <Campo rotulo="Valor reforçado">{formatarMoeda(dotacao.valorReforcado)}</Campo>
                <Campo rotulo="Valor anulado">{formatarMoeda(dotacao.valorAnulado)}</Campo>
                <Campo rotulo="Valor atualizado">{formatarMoeda(dotacao.valorAtualizado)}</Campo>
                <Campo rotulo="Empenhado líquido">{formatarMoeda(dotacao.valorEmpenhadoLiquido)}</Campo>
                <Campo rotulo="Saldo disponível">{formatarMoeda(dotacao.saldoDisponivel)}</Campo>
                <Campo rotulo="Identificador">{dotacao.id}</Campo>
              </dl>
            </Card>

            <ValorAcaoModal
              open={acao === 'reforcar'}
              onClose={() => setAcao(null)}
              title="Reforçar dotação"
              label="Valor do reforço (R$)"
              help="Suplementação do crédito orçamentário."
              confirmLabel="Reforçar"
              pending={reforcar.isPending}
              onConfirm={(valor) => reforcar.mutateAsync(valor as number)}
              mensagemSucesso="Crédito reforçado."
              mensagemErroPadrao="Não foi possível reforçar a dotação."
            />
            <ValorAcaoModal
              open={acao === 'anular'}
              onClose={() => setAcao(null)}
              title="Anular crédito da dotação"
              label="Valor a anular (R$)"
              confirmLabel="Anular crédito"
              confirmVariant="danger"
              pending={anular.isPending}
              onConfirm={(valor) => anular.mutateAsync(valor as number)}
              mensagemSucesso="Crédito anulado."
              mensagemErroPadrao="Não foi possível anular o crédito."
            />
          </>
        )}
      </QueryState>
    </>
  );
}
