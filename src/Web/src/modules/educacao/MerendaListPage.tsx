// Tela de LISTA de cardápios da merenda escolar (PNAE). Espelha o endpoint REAL
// GET /educacao/merenda/cardapios?escolaId&semana (CardapioDto[]). Cada cardápio
// abre o detalhe (itens + publicação + distribuição). Planejar gated em
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
  Input,
  PageHeader,
  Select,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column, SelectOption } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useCardapios } from './merenda.api';
import type { CardapioDto } from './merenda.api';
import { useEscolasDaRede } from './escola.api';
import { situacaoCardapioTagVariant } from './educacao.helpers';
import { CardapioFormModal } from './CardapioFormModal';
import { EducacaoSubNav } from './EducacaoSubNav';

export function MerendaListPage() {
  const [escolaCampo, setEscolaCampo] = useState('');
  const [semanaCampo, setSemanaCampo] = useState('');
  const [escolaId, setEscolaId] = useState('');
  const [semana, setSemana] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const escolas = useEscolasDaRede();
  const query = useCardapios(escolaId || undefined, semana || undefined);

  const opcoesEscola = useMemo<SelectOption[]>(
    () => (escolas.data ?? []).map((e) => ({ value: e.id, label: e.nome })),
    [escolas.data],
  );

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setEscolaId(escolaCampo);
    setSemana(semanaCampo);
  }

  const columns: Column<CardapioDto>[] = [
    {
      key: 'semana',
      header: 'Semana',
      sortAccessor: (c) => c.semana,
      render: (c) => new Date(`${c.semana}T00:00:00`).toLocaleDateString('pt-BR'),
    },
    { key: 'faixa', header: 'Faixa etária', render: (c) => c.faixaEtaria },
    {
      key: 'itens',
      header: 'Itens',
      sortAccessor: (c) => c.itens.length,
      render: (c) => `${c.itens.length} item(ns)`,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (c) => c.situacao,
      render: (c) => <Tag variant={situacaoCardapioTagVariant(c.situacao)}>{c.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (c) => (
        <Link to={`/educacao/merenda/${c.id}`} className="br-button secondary small">
          <i className="fas fa-utensils" aria-hidden="true" /> Abrir cardápio
        </Link>
      ),
    },
  ];

  return (
    <>
      <EducacaoSubNav />
      <PageHeader
        eyebrow="Educação"
        title="Merenda escolar (PNAE)"
        description="Planeje cardápios semanais por escola/faixa etária e registre a distribuição/consumo de gêneros."
        actions={
          <Can permission="educacao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Planejar cardápio
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
              <div className="col-12 col-md-8">
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
              <div className="col-12 col-md-4">
                <FormField label="Semana (segunda-feira)">
                  {({ id }) => (
                    <Input
                      id={id}
                      type="date"
                      value={semanaCampo}
                      onChange={(e) => setSemanaCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Cardápios planejados"
        columns={columns}
        rows={query.data}
        rowKey={(c) => c.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-utensils"
            title="Nenhum cardápio encontrado"
            description="Ajuste os filtros ou planeje um novo cardápio semanal."
          />
        }
      />

      <CardapioFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
