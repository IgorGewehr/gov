// Lista das OBRAS (W9.3, Lei 14.133/2021). Lista paginada por objeto/município
// com filtro por situação (GET /patrimonio/obras), link para a ficha e a ação de
// abrir nova obra vinculada a contrato ([Command AbrirObra] via ObraAbrirModal).
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
import { formatarMoeda } from '../../../i18n/format';
import { useObrasLista } from './obra.api';
import type { ObraLinha, SituacaoObraValor } from './obra.api';
import {
  OPCOES_SITUACAO_OBRA,
  situacaoObraLabel,
  situacaoObraTagVariant,
} from './obra.helpers';
import { ObraAbrirModal } from './ObraAbrirModal';
import { PatrimonioSubNav } from '../PatrimonioSubNav';
import { Paginacao } from '../shared/Paginacao';
import { TAMANHO_PAGINA_PADRAO } from '../shared/paginacaoTipos';

export function ObraListPage() {
  const navigate = useNavigate();
  const [abrirAberto, setAbrirAberto] = useState(false);

  const [termoInput, setTermoInput] = useState('');
  const [termo, setTermo] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);

  const query = useObrasLista({
    termo: termo || undefined,
    situacao: situacao === '' ? undefined : (Number(situacao) as SituacaoObraValor),
    pagina,
    tamanho: TAMANHO_PAGINA_PADRAO,
  });

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setTermo(termoInput.trim());
    setPagina(1);
  }

  const columns: Column<ObraLinha>[] = [
    {
      key: 'objeto',
      header: 'Objeto',
      sortAccessor: (o) => o.objeto,
      render: (o) => o.objeto,
    },
    {
      key: 'local',
      header: 'Município/UF',
      sortAccessor: (o) => `${o.municipio}/${o.uf}`,
      render: (o) => `${o.municipio}/${o.uf}`,
    },
    {
      key: 'valor',
      header: 'Valor contratado',
      align: 'end',
      sortAccessor: (o) => o.valorContratado,
      render: (o) => formatarMoeda(o.valorContratado),
    },
    {
      key: 'fisico',
      header: '% físico',
      align: 'end',
      sortAccessor: (o) => o.percentualFisicoAcumulado,
      render: (o) => `${o.percentualFisicoAcumulado.toFixed(2)}%`,
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (o) => (
        <Tag variant={situacaoObraTagVariant(o.situacao)}>{situacaoObraLabel(o.situacao)}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (o) => (
        <Link className="br-button secondary small" to={`/patrimonio/obras/${o.id}`}>
          Ficha
        </Link>
      ),
    },
  ];

  return (
    <>
      <PatrimonioSubNav />
      <PageHeader
        eyebrow="Patrimônio"
        title="Obras e serviços de engenharia"
        description="Obras como bem patrimonial em formação (Lei 14.133/2021): cronograma físico-financeiro, RDO, medição, fiscalização e prazos do art. 94 §3."
        actions={
          <Can permission="patrimonio.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setAbrirAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Abrir obra
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4" header={<strong>Filtrar obras</strong>}>
        <form className="br-form mb-3" onSubmit={buscar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                <i className="fas fa-magnifying-glass" aria-hidden="true" /> Filtrar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-8">
                <FormField label="Objeto ou município">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={termoInput}
                      onChange={(e) => setTermoInput(e.target.value)}
                      placeholder="Trecho do objeto ou do município"
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
                      options={OPCOES_SITUACAO_OBRA}
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
          caption="Obras e serviços de engenharia"
          columns={columns}
          rows={query.data?.itens}
          rowKey={(o) => o.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-helmet-safety"
              title="Nenhuma obra encontrada"
              description="Ajuste os filtros ou abra uma nova obra vinculada a contrato."
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

      <ObraAbrirModal
        open={abrirAberto}
        onClose={() => setAbrirAberto(false)}
        onAberta={(id) => navigate(`/patrimonio/obras/${id}`)}
      />
    </>
  );
}
