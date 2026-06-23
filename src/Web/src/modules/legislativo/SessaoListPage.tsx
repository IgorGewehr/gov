// Tela de LISTA de Sessoes agendadas. DataTable com estados loading/vazio/erro,
// link para detalhe e abertura do formulario de agendamento (mutation).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button, DataTable, EmptyState, PageHeader, Tag, Toolbar } from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useSessoesAgendadas } from './api';
import type { SessaoResumo } from './api';
import { formatarDataHora, situacaoSessaoTagVariant } from './legislativo.helpers';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import { SessaoFormModal } from './SessaoFormModal';

export function SessaoListPage() {
  const [formAberto, setFormAberto] = useState(false);
  const query = useSessoesAgendadas();

  const columns: Column<SessaoResumo>[] = [
    { key: 'tipo', header: 'Tipo', sortAccessor: (s) => s.tipo, render: (s) => s.tipo },
    {
      key: 'dataHora',
      header: 'Data e hora',
      sortAccessor: (s) => s.dataHora,
      render: (s) => formatarDataHora(s.dataHora),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (s) => s.situacao,
      render: (s) => <Tag variant={situacaoSessaoTagVariant(s.situacao)}>{s.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (s) => (
        <Link className="br-button tertiary small" to={`/legislativo/sessoes/${s.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Legislativo"
        title="Sessões"
        description="Sessões plenárias agendadas da Câmara Municipal."
        actions={
          <Can permission="legislativo.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Agendar sessão
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <LegislativoSecoesNav />

      <DataTable
        caption="Sessões agendadas"
        columns={columns}
        rows={query.data}
        rowKey={(s) => s.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-calendar-day"
            title="Nenhuma sessão agendada"
            description="Não há sessões plenárias agendadas no momento."
          />
        }
      />

      <SessaoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
