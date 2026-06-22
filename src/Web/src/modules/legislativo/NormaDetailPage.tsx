// Tela de DETALHE de uma Norma. Param de rota -> useQuery, QueryState para
// loading/erro, Card com cabecalho + texto integral + vinculos (revoga/altera) e
// barra de acoes (revogar/alterar) gated por `legislativo.normas.gerenciar`.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, Card, DataTable, EmptyState, PageHeader, QueryState, Tag } from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { Can } from '../../auth/Can';
import { useNorma } from './normas.api';
import type { EventoVigencia, NormaDetalhe } from './normas.api';
import { situacaoNormaTagVariant } from './legislativo.helpers';
import { NormaAcaoModal } from './NormaAcaoModal';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

const COLUNAS_EVENTO: Column<EventoVigencia>[] = [
  { key: 'tipo', header: 'Tipo', render: (e) => e.tipo },
  { key: 'data', header: 'Data', render: (e) => formatarData(e.data) },
  {
    key: 'norma',
    header: 'Norma referenciada',
    render: (e) =>
      e.normaReferenciaId ? (
        <Link className="br-button tertiary small" to={`/legislativo/normas/${e.normaReferenciaId}`}>
          Ver norma
        </Link>
      ) : (
        '—'
      ),
  },
  { key: 'obs', header: 'Observação', render: (e) => e.observacao ?? '—' },
];

type AcaoAtiva = null | 'revogar' | 'alterar';

export function NormaDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useNorma(id);
  const [acao, setAcao] = useState<AcaoAtiva>(null);

  return (
    <>
      <PageHeader
        title="Detalhe da Norma"
        actions={
          <Link className="br-button secondary" to="/legislativo/normas">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<NormaDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(norma) => (
          <>
            <Card className="mb-4" header={<strong>{`${norma.tipo} nº ${norma.numero}/${norma.ano}`}</strong>}>
              <dl className="row">
                <Campo rotulo="Situação">
                  <Tag variant={situacaoNormaTagVariant(norma.situacao)}>{norma.situacao}</Tag>
                </Campo>
                <Campo rotulo="Data de promulgação">{formatarData(norma.dataPromulgacao)}</Campo>
                <div className="col-12 mb-3">
                  <dt className="text-gray-60 text-down-01">Ementa</dt>
                  <dd className="mb-0">{norma.ementa}</dd>
                </div>
                <div className="col-12">
                  <dt className="text-gray-60 text-down-01">Texto articulado</dt>
                  <dd className="mb-0" style={{ whiteSpace: 'pre-wrap' }}>
                    {norma.textoArticulado ?? '—'}
                  </dd>
                </div>
              </dl>
            </Card>

            <Can permission="legislativo.normas.gerenciar">
              <div className="d-flex flex-wrap gap-2 mb-4" role="group" aria-label="Ações da norma">
                <Button variant="secondary" onClick={() => setAcao('revogar')}>
                  Revogar norma
                </Button>
                <Button variant="secondary" onClick={() => setAcao('alterar')}>
                  Registrar alteração
                </Button>
              </div>

              <NormaAcaoModal
                open={acao === 'revogar'}
                onClose={() => setAcao(null)}
                id={norma.id}
                tipo="revogar"
              />
              <NormaAcaoModal
                open={acao === 'alterar'}
                onClose={() => setAcao(null)}
                id={norma.id}
                tipo="alterar"
              />
            </Can>

            <h2 className="text-up-01 mb-3">Histórico de vigência</h2>
            <DataTable
              caption="Trilha de vigência da norma (revogações e alterações)"
              columns={COLUNAS_EVENTO}
              rows={norma.historico}
              rowKey={(e) => `${e.tipo}-${e.data}-${e.normaReferenciaId ?? 'sem-ref'}`}
              empty={
                <EmptyState
                  icon="fas fa-clock-rotate-left"
                  title="Sem eventos de vigência"
                  description="Esta norma não possui revogações ou alterações registradas."
                />
              }
            />
          </>
        )}
      </QueryState>
    </>
  );
}
