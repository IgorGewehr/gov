// Tela de LISTA de edicoes do Diario Oficial. DataTable com estados, link para a
// edicao (montagem/visualizacao) e abertura do formulario de montagem de edicao
// (gated `legislativo.diario.publicar`).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, DataTable, EmptyState, PageHeader, Tag, Toolbar } from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { Can } from '../../auth/Can';
import { useEdicoes } from './diario.api';
import type { EdicaoResumo } from './diario.api';
import { situacaoEdicaoTagVariant } from './legislativo.helpers';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import { EdicaoFormModal } from './EdicaoFormModal';

export function DiarioListPage() {
  const query = useEdicoes();
  const [formAberto, setFormAberto] = useState(false);

  const columns: Column<EdicaoResumo>[] = [
    {
      key: 'numero',
      header: 'Edição',
      align: 'center',
      sortAccessor: (e) => e.numero,
      render: (e) => `Nº ${e.numero}`,
    },
    {
      key: 'ano',
      header: 'Ano',
      align: 'center',
      sortAccessor: (e) => e.ano,
      render: (e) => e.ano,
    },
    {
      key: 'publicacao',
      header: 'Publicada em',
      sortAccessor: (e) => e.dataPublicacao ?? '',
      render: (e) => (e.dataPublicacao ? formatarData(e.dataPublicacao) : '—'),
    },
    {
      key: 'materias',
      header: 'Matérias',
      align: 'center',
      sortAccessor: (e) => e.totalMaterias,
      render: (e) => e.totalMaterias,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (e) => e.situacao,
      render: (e) => <Tag variant={situacaoEdicaoTagVariant(e.situacao)}>{e.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (e) => (
        <Link className="br-button tertiary small" to={`/legislativo/diario/${e.id}`}>
          {e.situacao === 'Publicada' ? 'Visualizar' : 'Montar'}
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Legislativo"
        title="Diário Oficial"
        description="Edições do Diário Oficial do Legislativo (publicidade dos atos)."
        actions={
          <Can permission="legislativo.diario.publicar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Montar edição
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <LegislativoSecoesNav />

      <DataTable
        caption="Edições do Diário Oficial"
        columns={columns}
        rows={query.data}
        rowKey={(e) => e.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-newspaper"
            title="Nenhuma edição"
            description="Monte a primeira edição do Diário Oficial."
          />
        }
      />

      <EdicaoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
