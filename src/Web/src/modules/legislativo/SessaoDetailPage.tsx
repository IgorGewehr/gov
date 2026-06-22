// Tela de DETALHE de uma Sessao. Param de rota -> useQuery, QueryState para
// loading/erro, Card com quorum/presentes + tabela da Ordem do Dia.
import { Link, useParams } from 'react-router-dom';
import { Card, DataTable, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { usePresencasSessao, useSessao } from './api';
import type { ItemOrdemDoDiaResumo, PresencaResumo, SessaoDetalhe } from './api';
import { formatarDataHora, situacaoSessaoTagVariant } from './legislativo.helpers';
import { SessaoAcoes } from './SessaoAcoes';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

const COLUNAS_ORDEM: Column<ItemOrdemDoDiaResumo>[] = [
  { key: 'ordem', header: 'Ordem', align: 'center', sortAccessor: (i) => i.ordem, render: (i) => i.ordem },
  {
    key: 'proposicao',
    header: 'Proposição',
    render: (i) => (
      <Link className="br-button tertiary small" to={`/legislativo/proposicoes/${i.proposicaoId}`}>
        {i.proposicaoId}
      </Link>
    ),
  },
];

const COLUNAS_PRESENCA: Column<PresencaResumo>[] = [
  { key: 'vereador', header: 'Vereador', render: (p) => p.vereadorId },
  {
    key: 'registrada',
    header: 'Registrada em',
    sortAccessor: (p) => p.registradaEm,
    render: (p) => formatarDataHora(p.registradaEm),
  },
];

export function SessaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useSessao(id);
  const presencas = usePresencasSessao(id);

  return (
    <>
      <PageHeader
        title="Detalhe da Sessão"
        actions={
          <Link className="br-button secondary" to="/legislativo/sessoes">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<SessaoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(sessao) => {
          const quorumAtingido = sessao.presentes >= sessao.quorumInstalacao;
          return (
            <>
              <Card className="mb-4" header={<strong>Sessão {sessao.tipo}</strong>}>
                <dl className="row">
                  <Campo rotulo="Situação">
                    <Tag variant={situacaoSessaoTagVariant(sessao.situacao)}>{sessao.situacao}</Tag>
                  </Campo>
                  <Campo rotulo="Data e hora">{formatarDataHora(sessao.dataHora)}</Campo>
                  <Campo rotulo="Total de membros">{sessao.totalMembros}</Campo>
                  <Campo rotulo="Quórum de instalação">{sessao.quorumInstalacao}</Campo>
                  <Campo rotulo="Presentes">
                    <Tag variant={quorumAtingido ? 'success' : 'warning'}>
                      {sessao.presentes} {quorumAtingido ? '(quórum atingido)' : '(sem quórum)'}
                    </Tag>
                  </Campo>
                </dl>
              </Card>

              <SessaoAcoes id={sessao.id} />

              <h2 className="text-up-01 mb-3">Ordem do Dia</h2>
              <DataTable
                caption={`Ordem do Dia da sessão ${sessao.id}`}
                columns={COLUNAS_ORDEM}
                rows={sessao.ordemDoDia}
                rowKey={(i) => `${i.ordem}-${i.proposicaoId}`}
                empty={
                  <EmptyState
                    icon="fas fa-list-ol"
                    title="Ordem do Dia vazia"
                    description="Nenhuma proposição foi pautada para deliberação nesta sessão."
                  />
                }
              />

              <h2 className="text-up-01 mb-3 mt-4">Presenças</h2>
              <DataTable
                caption={`Presenças registradas na sessão ${sessao.id}`}
                columns={COLUNAS_PRESENCA}
                rows={presencas.data}
                rowKey={(p) => p.vereadorId}
                loading={presencas.isLoading}
                error={presencas.isError ? errorMessage(presencas.error) : null}
                empty={
                  <EmptyState
                    icon="fas fa-user-check"
                    title="Nenhuma presença registrada"
                    description="Ainda não há vereadores com presença registrada nesta sessão."
                  />
                }
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
