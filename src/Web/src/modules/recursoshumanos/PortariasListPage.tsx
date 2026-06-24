// Tela de LISTA/BUSCA de PORTARIAS / atos de pessoal. Navegabilidade: busca paginada por
// tipo/situação/exercício sobre GET /api/recursoshumanos/portarias (envelope ResultadoPaginado).
// Emissão via modal; numeração sequencial por exercício é atribuída no backend.
import { useState } from 'react';
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
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useBuscarPortarias } from './api';
import type { FiltroPortarias, PortariaResumo } from './api';
import {
  PERM_RH_GERENCIAR,
  situacaoPortariaTagVariant,
  SITUACOES_PORTARIA,
  TIPOS_PORTARIA,
} from './recursosHumanos.helpers';
import { EmitirPortariaFormModal } from './EmitirPortariaFormModal';
import { RhSubNav } from './RhSubNav';

const FILTRO_INICIAL: FiltroPortarias = {
  tipo: null,
  situacao: null,
  exercicio: null,
  servidorId: null,
  pagina: 1,
};

function paraNumeroOuNull(valor: string): number | null {
  const n = Number(valor);
  return valor.trim() !== '' && !Number.isNaN(n) ? n : null;
}

export function PortariasListPage() {
  const [tipo, setTipo] = useState('');
  const [situacao, setSituacao] = useState('');
  const [exercicio, setExercicio] = useState('');
  const [filtro, setFiltro] = useState<FiltroPortarias>(FILTRO_INICIAL);
  const [formAberto, setFormAberto] = useState(false);

  const query = useBuscarPortarias(filtro);
  const totalPaginas = query.data
    ? Math.max(1, Math.ceil(query.data.total / query.data.tamanho))
    : 1;

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setFiltro({
      tipo: paraNumeroOuNull(tipo),
      situacao: paraNumeroOuNull(situacao),
      exercicio: paraNumeroOuNull(exercicio),
      servidorId: null,
      pagina: 1,
    });
  }

  function irParaPagina(pagina: number): void {
    setFiltro((atual) => ({ ...atual, pagina }));
  }

  const columns: Column<PortariaResumo>[] = [
    {
      key: 'numero',
      header: 'Número',
      sortAccessor: (p) => `${p.exercicio}-${p.sequencial}`,
      render: (p) => <span className="text-semi-bold">{p.numero}</span>,
    },
    { key: 'tipo', header: 'Natureza', sortAccessor: (p) => p.tipo, render: (p) => p.tipo },
    { key: 'ementa', header: 'Ementa', render: (p) => p.ementa },
    {
      key: 'dataAto',
      header: 'Data',
      align: 'end',
      sortAccessor: (p) => p.dataAto,
      render: (p) => formatarData(p.dataAto),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (p) => p.situacao,
      render: (p) => <Tag variant={situacaoPortariaTagVariant(p.situacao)}>{p.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (p) => (
        <Link className="br-button secondary small" to={`/recursoshumanos/portarias/${p.id}`}>
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
        title="Portarias"
        description="Atos de pessoal (nomeação, exoneração, designação, concessão) com numeração sequencial por exercício."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-file-signature" aria-hidden="true" /> Emitir portaria
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={buscar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit">
                <i className="fas fa-search" aria-hidden="true" /> Buscar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-6 col-md-4">
                <FormField label="Natureza">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      value={tipo}
                      onChange={(e) => setTipo(e.target.value)}
                      options={[{ value: '', label: 'Todas' }, ...TIPOS_PORTARIA]}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-6 col-md-4">
                <FormField label="Situação">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      value={situacao}
                      onChange={(e) => setSituacao(e.target.value)}
                      options={[{ value: '', label: 'Todas' }, ...SITUACOES_PORTARIA]}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-6 col-md-4">
                <FormField label="Exercício">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      value={exercicio}
                      onChange={(e) => setExercicio(e.target.value)}
                      placeholder="Ano"
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Resultado da busca de portarias"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(p) => p.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-file-signature"
            title="Nenhuma portaria encontrada"
            description="Ajuste os filtros ou emita uma nova portaria."
          />
        }
      />

      {query.data && query.data.total > 0 && (
        <nav
          className="d-flex align-items-center mt-3"
          aria-label="Paginação"
          style={{ gap: '0.75rem' }}
        >
          <Button
            variant="secondary"
            onClick={() => irParaPagina(filtro.pagina - 1)}
            disabled={filtro.pagina <= 1 || query.isFetching}
          >
            Anterior
          </Button>
          <span aria-live="polite">
            Página {filtro.pagina} de {totalPaginas} ({query.data.total} portarias)
          </span>
          <Button
            variant="secondary"
            onClick={() => irParaPagina(filtro.pagina + 1)}
            disabled={filtro.pagina >= totalPaginas || query.isFetching}
          >
            Próxima
          </Button>
        </nav>
      )}

      <EmitirPortariaFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
