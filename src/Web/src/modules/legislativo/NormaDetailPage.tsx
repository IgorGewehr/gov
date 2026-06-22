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
import type { NormaDetalhe, NormaVinculoResumo } from './normas.api';
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

const COLUNAS_VINCULO: Column<NormaVinculoResumo>[] = [
  { key: 'tipo', header: 'Tipo', render: (v) => v.tipo },
  {
    key: 'norma',
    header: 'Norma',
    render: (v) => (
      <Link className="br-button tertiary small" to={`/legislativo/normas/${v.normaId}`}>
        {v.numero}
      </Link>
    ),
  },
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
                <Campo rotulo="Data de publicação">{formatarData(norma.dataPublicacao)}</Campo>
                <div className="col-12 mb-3">
                  <dt className="text-gray-60 text-down-01">Ementa</dt>
                  <dd className="mb-0">{norma.ementa}</dd>
                </div>
                <div className="col-12">
                  <dt className="text-gray-60 text-down-01">Texto integral</dt>
                  <dd className="mb-0" style={{ whiteSpace: 'pre-wrap' }}>
                    {norma.textoIntegral}
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

            <h2 className="text-up-01 mb-3">Normas revogadas por esta</h2>
            <DataTable
              caption="Normas revogadas"
              columns={COLUNAS_VINCULO}
              rows={norma.revogaNormas}
              rowKey={(v) => v.normaId}
              empty={
                <EmptyState
                  icon="fas fa-ban"
                  title="Nenhuma revogação"
                  description="Esta norma não revoga outras normas."
                />
              }
            />

            <h2 className="text-up-01 mb-3 mt-4">Normas alteradas por esta</h2>
            <DataTable
              caption="Normas alteradas"
              columns={COLUNAS_VINCULO}
              rows={norma.alteraNormas}
              rowKey={(v) => v.normaId}
              empty={
                <EmptyState
                  icon="fas fa-pen"
                  title="Nenhuma alteração"
                  description="Esta norma não altera outras normas."
                />
              }
            />
          </>
        )}
      </QueryState>
    </>
  );
}
