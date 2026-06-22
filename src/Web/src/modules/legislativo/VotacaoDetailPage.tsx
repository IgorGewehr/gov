// Tela de DETALHE de uma Votacao. Param de rota -> useQuery, QueryState para
// loading/erro, Card com placar agregado (Sim/Nao/Abstencao) e resultado apurado.
import { Link, useParams } from 'react-router-dom';
import { Card, DataTable, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { useVotacao, useVotosNominais } from './api';
import type { VotacaoDetalhe, VotoResumo } from './api';
import { formatarDataHora, resultadoVotacaoTagVariant, situacaoVotacaoTagVariant } from './legislativo.helpers';
import { VotacaoAcoes } from './VotacaoAcoes';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

function Placar({ rotulo, valor, variant }: { rotulo: string; valor: number; variant: 'success' | 'danger' | 'default' }) {
  return (
    <div className="col-4 text-center mb-3">
      <span className="d-block text-down-01 text-gray-60">{rotulo}</span>
      <span className="d-block">
        <Tag variant={variant}>{valor}</Tag>
      </span>
    </div>
  );
}

const COLUNAS_VOTOS: Column<VotoResumo>[] = [
  { key: 'vereador', header: 'Vereador', render: (v) => v.vereadorId },
  { key: 'sentido', header: 'Sentido', sortAccessor: (v) => v.sentido, render: (v) => v.sentido },
  {
    key: 'registrado',
    header: 'Registrado em',
    sortAccessor: (v) => v.registradoEm,
    render: (v) => formatarDataHora(v.registradoEm),
  },
];

export function VotacaoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useVotacao(id);
  const votos = useVotosNominais(id);

  return (
    <>
      <PageHeader
        title="Detalhe da Votação"
        actions={
          <Link className="br-button secondary" to="/legislativo/votacoes">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<VotacaoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(votacao) => (
          <>
            <Card className="mb-4" header={<strong>Votação {votacao.tipo}</strong>}>
              <dl className="row">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoVotacaoTagVariant(votacao.situacao)}>{votacao.situacao}</Tag>
                </Campo>
                <Campo rotulo="Resultado">
                  {votacao.resultado === null ? (
                    '— (em apuração)'
                  ) : (
                    <Tag variant={resultadoVotacaoTagVariant(votacao.resultado)}>{votacao.resultado}</Tag>
                  )}
                </Campo>
                <Campo rotulo="Maioria exigida">{votacao.maioriaExigida}</Campo>
                <Campo rotulo="Turno">{votacao.turno}º</Campo>
                <Campo rotulo="Total de membros">{votacao.totalMembros}</Campo>
                <Campo rotulo="Presentes">{votacao.presentes}</Campo>
              </dl>
            </Card>

            <Card className="mb-4" header={<strong>Placar</strong>}>
              <dl className="row mb-0">
                <Placar rotulo="Sim" valor={votacao.votosSim} variant="success" />
                <Placar rotulo="Não" valor={votacao.votosNao} variant="danger" />
                <Placar rotulo="Abstenções" valor={votacao.abstencoes} variant="default" />
              </dl>
            </Card>

            <VotacaoAcoes id={votacao.id} />

            <h2 className="text-up-01 mb-3">Votos nominais</h2>
            <DataTable
              caption={`Votos nominais da votação ${votacao.id}`}
              columns={COLUNAS_VOTOS}
              rows={votos.data}
              rowKey={(v) => v.vereadorId}
              loading={votos.isLoading}
              error={votos.isError ? errorMessage(votos.error) : null}
              empty={
                <EmptyState
                  icon="fas fa-square-poll-vertical"
                  title="Sem votos nominais"
                  description="Não há votos nominais a exibir (votação simbólica/secreta ou ainda sem registros)."
                />
              }
            />
          </>
        )}
      </QueryState>
    </>
  );
}
