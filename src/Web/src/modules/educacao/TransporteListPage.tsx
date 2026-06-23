// Tela de LISTA de rotas de transporte escolar (PNATE). Espelha o endpoint REAL
// GET /educacao/transporte/rotas?escolaId (RotaTransporteItemLista[]). Cada rota
// abre o detalhe (alunos vinculados + ativar/encerrar). Criar gated em
// "educacao.gerenciar"; leitura em "educacao.ver".
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
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
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column, SelectOption } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useRotas } from './transporte.api';
import type { RotaTransporteItemLista } from './transporte.api';
import { useEscolasDaRede } from './escola.api';
import { situacaoRotaTagVariant } from './educacao.helpers';
import { RotaFormModal } from './RotaFormModal';
import { EducacaoSubNav } from './EducacaoSubNav';

export function TransporteListPage() {
  const [escolaCampo, setEscolaCampo] = useState('');
  const [escolaId, setEscolaId] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const escolas = useEscolasDaRede();
  const query = useRotas(escolaId || undefined);

  const opcoesEscola = useMemo<SelectOption[]>(
    () => (escolas.data ?? []).map((e) => ({ value: e.id, label: e.nome })),
    [escolas.data],
  );

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setEscolaId(escolaCampo);
  }

  const columns: Column<RotaTransporteItemLista>[] = [
    { key: 'nome', header: 'Rota', sortAccessor: (r) => r.nome, render: (r) => r.nome },
    { key: 'turno', header: 'Turno', render: (r) => r.turno },
    { key: 'modalidade', header: 'Modalidade', render: (r) => r.modalidade },
    {
      key: 'km',
      header: 'Quilometragem',
      sortAccessor: (r) => r.quilometragem,
      render: (r) => `${r.quilometragem.toLocaleString('pt-BR')} km`,
    },
    {
      key: 'alunos',
      header: 'Alunos ativos',
      sortAccessor: (r) => r.totalAtivos,
      render: (r) => `${r.totalAtivos}`,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (r) => r.situacao,
      render: (r) => <Tag variant={situacaoRotaTagVariant(r.situacao)}>{r.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (r) => (
        <Link to={`/educacao/transporte/${r.id}`} className="br-button secondary small">
          <i className="fas fa-bus" aria-hidden="true" /> Abrir rota
        </Link>
      ),
    },
  ];

  return (
    <>
      <EducacaoSubNav />
      <PageHeader
        eyebrow="Educação"
        title="Transporte escolar (PNATE)"
        description="Crie rotas por turno/escola, vincule alunos e gerencie o ciclo de vida (planejada → ativa → encerrada)."
        actions={
          <Can permission="educacao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Criar rota
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={aplicarBusca}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Buscar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12">
                <FormField label="Escola">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesEscola}
                      placeholder="Todas as escolas"
                      value={escolaCampo}
                      onChange={(e) => setEscolaCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Rotas de transporte"
        columns={columns}
        rows={query.data}
        rowKey={(r) => r.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-bus"
            title="Nenhuma rota encontrada"
            description="Ajuste os filtros ou crie uma nova rota de transporte."
          />
        }
      />

      <RotaFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
