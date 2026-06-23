// Tela de LISTA/CONSULTA de Inventários patrimoniais (Lei 4.320 art. 96).
//   - LISTA NAVEGÁVEL: filtros por exercício/setor/situação, paginada
//     (GET /patrimonio/inventarios) -> link para o detalhe;
//   - ação de abrir inventário ([Command AbrirInventario] via Modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
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
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import { useInventariosLista } from './inventario.api';
import type { InventarioItemLista, SituacaoInventarioValor } from './inventario.api';
import {
  OPCOES_SITUACAO_INVENTARIO,
  situacaoInventarioLabel,
  situacaoInventarioTagVariant,
  tipoInventarioLabel,
} from './inventario.helpers';
import { InventarioAbrirModal } from './InventarioAbrirModal';
import { PatrimonioSubNav } from '../PatrimonioSubNav';
import { Paginacao } from '../shared/Paginacao';
import { TAMANHO_PAGINA_PADRAO } from '../shared/paginacaoTipos';

export function InventarioListPage() {
  const navigate = useNavigate();
  const [abrirAberto, setAbrirAberto] = useState(false);

  const [exercicioInput, setExercicioInput] = useState('');
  const [exercicio, setExercicio] = useState('');
  const [setorInput, setSetorInput] = useState('');
  const [setor, setSetor] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);

  const query = useInventariosLista({
    exercicio: exercicio === '' ? undefined : Number(exercicio),
    setor: setor || undefined,
    situacao: situacao === '' ? undefined : (Number(situacao) as SituacaoInventarioValor),
    pagina,
    tamanho: TAMANHO_PAGINA_PADRAO,
  });

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setExercicio(exercicioInput.trim());
    setSetor(setorInput.trim());
    setPagina(1);
  }

  const columns: Column<InventarioItemLista>[] = [
    {
      key: 'exercicio',
      header: 'Exercício',
      sortAccessor: (i) => i.exercicio,
      render: (i) => i.exercicio,
    },
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (i) => i.tipo,
      render: (i) => tipoInventarioLabel(i.tipo),
    },
    { key: 'setor', header: 'Setor', render: (i) => i.setor ?? 'Geral' },
    {
      key: 'situacao',
      header: 'Situação',
      render: (i) => (
        <Tag variant={situacaoInventarioTagVariant(i.situacao)}>
          {situacaoInventarioLabel(i.situacao)}
        </Tag>
      ),
    },
    {
      key: 'dataAbertura',
      header: 'Abertura',
      sortAccessor: (i) => i.dataAbertura,
      render: (i) => formatarData(i.dataAbertura),
    },
    {
      key: 'totalItens',
      header: 'Itens',
      align: 'end',
      sortAccessor: (i) => i.totalItens,
      render: (i) => i.totalItens,
    },
    {
      key: 'totalDivergencias',
      header: 'Divergências',
      align: 'end',
      sortAccessor: (i) => i.totalDivergencias,
      render: (i) =>
        i.totalDivergencias > 0 ? (
          <Tag variant="warning">{i.totalDivergencias}</Tag>
        ) : (
          i.totalDivergencias
        ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) => (
        <Link className="br-button secondary small" to={`/patrimonio/inventarios/${i.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PatrimonioSubNav />
      <PageHeader
        eyebrow="Patrimônio"
        title="Inventários"
        description="Levantamento físico × contábil do acervo (Lei 4.320, art. 96)."
        actions={
          <Can permission="patrimonio.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setAbrirAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Abrir inventário
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4" header={<strong>Buscar inventários</strong>}>
        <form className="br-form mb-3" onSubmit={buscar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                <i className="fas fa-magnifying-glass" aria-hidden="true" /> Buscar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-3">
                <FormField label="Exercício">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      min="2000"
                      aria-describedby={describedBy}
                      value={exercicioInput}
                      onChange={(e) => setExercicioInput(e.target.value)}
                      placeholder="Ano"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-5">
                <FormField label="Setor">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={setorInput}
                      onChange={(e) => setSetorInput(e.target.value)}
                      placeholder="Trecho do setor/UO"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-4">
                <FormField label="Situação">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      options={OPCOES_SITUACAO_INVENTARIO}
                      placeholder="Todas"
                      value={situacao}
                      onChange={(e) => {
                        setSituacao(e.target.value);
                        setPagina(1);
                      }}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>

        <DataTable
          caption="Inventários patrimoniais"
          columns={columns}
          rows={query.data?.itens}
          rowKey={(i) => i.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-clipboard-list"
              title="Nenhum inventário encontrado"
              description="Ajuste os filtros ou abra um novo inventário."
            />
          }
        />

        {query.data && query.data.total > 0 && (
          <div className="mt-3">
            <Paginacao
              pagina={query.data.pagina}
              tamanho={query.data.tamanho}
              total={query.data.total}
              onPaginaChange={setPagina}
              carregando={query.isFetching}
            />
          </div>
        )}
      </Card>

      <InventarioAbrirModal
        open={abrirAberto}
        onClose={() => setAbrirAberto(false)}
        onAberto={(id) => navigate(`/patrimonio/inventarios/${id}`)}
      />
    </>
  );
}
