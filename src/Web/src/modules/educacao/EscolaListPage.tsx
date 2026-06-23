// Tela de LISTA das escolas da rede. Padrão-ouro: DataTable com estados
// loading/vazio/erro + ordenação, link para detalhe (por código INEP) e
// abertura do formulário de credenciamento (mutation).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
  Toolbar,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useEscolasDaRede } from './api';
import type { EscolaResumo } from './api';
import { situacaoEscolaTagVariant } from './educacao.helpers';
import { EscolaFormModal } from './EscolaFormModal';

export function EscolaListPage() {
  const [formAberto, setFormAberto] = useState(false);
  const query = useEscolasDaRede();

  const columns: Column<EscolaResumo>[] = [
    {
      key: 'codigoInep',
      header: 'Código INEP',
      sortAccessor: (e) => e.codigoInep,
      render: (e) => e.codigoInep,
    },
    {
      key: 'nome',
      header: 'Nome',
      sortAccessor: (e) => e.nome,
      render: (e) => e.nome,
    },
    {
      key: 'dependencia',
      header: 'Dependência',
      sortAccessor: (e) => e.dependencia,
      render: (e) => e.dependencia,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (e) => e.situacao,
      render: (e) => <Tag variant={situacaoEscolaTagVariant(e.situacao)}>{e.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (e) => (
        <Link className="br-button tertiary small" to={`/educacao/escolas/${encodeURIComponent(e.codigoInep)}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Educação"
        title="Escolas da rede"
        description="Consulte e credencie as unidades escolares da rede de ensino do município."
        actions={
          <Toolbar>
            <Link className="br-button secondary" to="/educacao/matriculas">
              <i className="fas fa-user-graduate" aria-hidden="true" /> Matrículas
            </Link>
            <Can permission="educacao.gerenciar">
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Credenciar escola
              </Button>
            </Can>
          </Toolbar>
        }
      />

      <DataTable
        caption="Escolas da rede de ensino"
        columns={columns}
        rows={query.data}
        rowKey={(e) => e.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-school"
            title="Nenhuma escola credenciada"
            description="Comece credenciando a primeira unidade escolar da rede."
          />
        }
      />

      <EscolaFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
