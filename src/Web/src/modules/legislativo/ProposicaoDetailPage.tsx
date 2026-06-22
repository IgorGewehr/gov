// Tela de DETALHE de uma Proposicao. Param de rota -> useQuery, QueryState para
// loading/erro, Card com pares rotulo/valor + tabela da trilha de tramitacao (LAI).
import { Link, useParams } from 'react-router-dom';
import { Card, DataTable, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { useProposicao } from './api';
import type { ProposicaoDetalhe, TramitacaoResumo } from './api';
import { situacaoProposicaoTagVariant } from './legislativo.helpers';
import { ProposicaoAcoes } from './ProposicaoAcoes';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

const COLUNAS_TRAMITACAO: Column<TramitacaoResumo>[] = [
  { key: 'fase', header: 'Fase', sortAccessor: (t) => t.fase, render: (t) => t.fase },
  { key: 'comissao', header: 'Comissão', render: (t) => t.comissao ?? '—' },
  {
    key: 'parecer',
    header: 'Parecer',
    render: (t) =>
      t.parecerFavoravel === null ? (
        '—'
      ) : (
        <Tag variant={t.parecerFavoravel ? 'success' : 'danger'}>
          {t.parecerFavoravel ? 'Favorável' : 'Contrário'}
        </Tag>
      ),
  },
  { key: 'data', header: 'Data', sortAccessor: (t) => t.data, render: (t) => formatarData(t.data) },
];

export function ProposicaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useProposicao(id);

  return (
    <>
      <PageHeader
        title="Detalhe da Proposição"
        actions={
          <Link className="br-button secondary" to="/legislativo">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ProposicaoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(proposicao) => (
          <>
            <Card className="mb-4" header={<strong>Protocolo {proposicao.protocolo}</strong>}>
              <dl className="row">
                <Campo rotulo="Tipo">{proposicao.tipo}</Campo>
                <Campo rotulo="Situação">
                  <Tag variant={situacaoProposicaoTagVariant(proposicao.situacao)}>{proposicao.situacao}</Tag>
                </Campo>
                <Campo rotulo="Ementa">{proposicao.ementa}</Campo>
                <Campo rotulo="Autoria">{proposicao.autoria}</Campo>
                <Campo rotulo="Regime de tramitação">{proposicao.regime}</Campo>
                <Campo rotulo="Data de apresentação">{formatarData(proposicao.dataApresentacao)}</Campo>
                <Campo rotulo="Número do autógrafo">{proposicao.numeroAutografo ?? '—'}</Campo>
              </dl>
            </Card>

            <ProposicaoAcoes id={proposicao.id} />

            <h2 className="text-up-01 mb-3">Tramitação</h2>
            <DataTable
              caption={`Trilha de tramitação da proposição ${proposicao.protocolo}`}
              columns={COLUNAS_TRAMITACAO}
              rows={proposicao.tramitacoes}
              rowKey={(t) => t.id}
              empty={
                <EmptyState
                  icon="fas fa-list-check"
                  title="Sem fases registradas"
                  description="A proposição ainda não possui fases de tramitação registradas."
                />
              }
            />
          </>
        )}
      </QueryState>
    </>
  );
}
