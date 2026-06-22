// Tela de LISTA de Comissoes parlamentares. DataTable com estados, link para a
// composicao (detalhe) e abertura do formulario de criacao (gated
// `legislativo.comissoes.gerenciar`).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, DataTable, EmptyState, PageHeader, Tag } from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useComissoes } from './comissoes.api';
import type { ComissaoResumo } from './comissoes.api';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import { ComissaoFormModal } from './ComissaoFormModal';

export function ComissaoListPage() {
  const query = useComissoes();
  const [formAberto, setFormAberto] = useState(false);

  const columns: Column<ComissaoResumo>[] = [
    { key: 'nome', header: 'Comissão', sortAccessor: (c) => c.nome, render: (c) => c.nome },
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (c) => c.tipo,
      render: (c) => <Tag variant="info">{c.tipo}</Tag>,
    },
    {
      key: 'membros',
      header: 'Membros',
      align: 'center',
      sortAccessor: (c) => c.totalMembros,
      render: (c) => c.totalMembros,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (c) => (
        <Link className="br-button tertiary small" to={`/legislativo/comissoes/${c.id}`}>
          Composição
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Comissões"
        description="Comissões permanentes e temporárias da Câmara e suas composições."
        actions={
          <Can permission="legislativo.comissoes.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Criar comissão
            </Button>
          </Can>
        }
      />

      <LegislativoSecoesNav />

      <DataTable
        caption="Comissões da Câmara"
        columns={columns}
        rows={query.data}
        rowKey={(c) => c.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-people-group"
            title="Nenhuma comissão"
            description="Crie a primeira comissão da Câmara."
          />
        }
      />

      <ComissaoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
