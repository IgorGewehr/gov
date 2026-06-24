// Tela de LISTA de cargos com vagas disponíveis. Padrão-ouro: filtro por tipo (Select),
// DataTable com estados loading/vazio/erro + ordenação, link para detalhe e abertura do
// formulário de criação (mutation).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  PageHeader,
  Select,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useCargosComVagas } from './api';
import type { CargoResumo, TipoCargo } from './api';
import { PERM_RH_GERENCIAR, TIPOS_CARGO } from './recursosHumanos.helpers';
import { CriarCargoFormModal } from './CriarCargoFormModal';
import { ReajusteEmLoteModal } from './ReajusteEmLoteModal';
import { RhSubNav } from './RhSubNav';

const ROTULO_TIPO: Record<string, TipoCargo> = {
  '1': 'Efetivo',
  '2': 'Comissionado',
  '3': 'Temporario',
};

export function CargosListPage() {
  const [filtroTipo, setFiltroTipo] = useState('');
  const [formAberto, setFormAberto] = useState(false);
  const [reajusteAberto, setReajusteAberto] = useState(false);

  const tipo = filtroTipo ? ROTULO_TIPO[filtroTipo] : null;
  const query = useCargosComVagas(tipo);

  const columns: Column<CargoResumo>[] = [
    {
      key: 'denominacao',
      header: 'Denominação',
      sortAccessor: (c) => c.denominacao,
      render: (c) => <span className="text-semi-bold">{c.denominacao}</span>,
    },
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (c) => c.tipo,
      render: (c) => c.tipo,
    },
    {
      key: 'vagas',
      header: 'Vagas disponíveis',
      align: 'end',
      sortAccessor: (c) => c.vagasDisponiveis,
      render: (c) => c.vagasDisponiveis,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (c) => (
        <Link className="br-button secondary small" to={`/recursoshumanos/cargos/${c.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Cargos"
        description="Estrutura de cargos públicos com vagas disponíveis para provimento."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="secondary" onClick={() => setReajusteAberto(true)}>
                <i className="fas fa-percent" aria-hidden="true" /> Reajuste em lote
              </Button>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Criar cargo
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={(e) => e.preventDefault()}>
          <FormRow
            acao={
              filtroTipo !== '' ? (
                <Button variant="secondary" onClick={() => setFiltroTipo('')}>
                  Limpar filtro
                </Button>
              ) : undefined
            }
          >
            <FormField label="Filtrar por tipo">
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={filtroTipo}
                  onChange={(e) => setFiltroTipo(e.target.value)}
                  placeholder="Todos os tipos"
                  options={TIPOS_CARGO}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Cargos com vagas disponíveis"
        columns={columns}
        rows={query.data}
        rowKey={(c) => c.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-briefcase"
            title="Nenhum cargo com vaga"
            description="Não há cargos com vagas disponíveis para o filtro atual."
          />
        }
      />

      <CriarCargoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
      <ReajusteEmLoteModal open={reajusteAberto} onClose={() => setReajusteAberto(false)} />
    </>
  );
}
