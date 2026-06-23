// Tela de LISTA/BUSCA de servidores do tenant (página de entrada do módulo RH).
// Navegabilidade (Onda 0): busca paginada por nome/matrícula + filtros situação/regime,
// sobre o endpoint GET /api/recursoshumanos/servidores (envelope ResultadoPaginado).
// DataTable com Ações sticky (link para a FICHA FUNCIONAL) e formulário de admissão.
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
import { useBuscaServidores } from './api';
import type { BuscaServidoresFiltro, ServidorResumo } from './api';
import {
  formatarRegimePrev,
  PERM_RH_GERENCIAR,
  REGIMES_SERVIDOR_FILTRO,
  situacaoServidorTagVariant,
  SITUACOES_SERVIDOR_FILTRO,
} from './recursosHumanos.helpers';
import { AdmitirServidorFormModal } from './AdmitirServidorFormModal';
import { RhSubNav } from './RhSubNav';

const FILTRO_INICIAL: BuscaServidoresFiltro = {
  termo: '',
  situacao: '',
  regime: '',
  cargoId: '',
  pagina: 1,
};

export function ServidoresListPage() {
  const [termo, setTermo] = useState('');
  const [situacao, setSituacao] = useState('');
  const [regime, setRegime] = useState('');
  const [filtro, setFiltro] = useState<BuscaServidoresFiltro>(FILTRO_INICIAL);
  const [formAberto, setFormAberto] = useState(false);

  const query = useBuscaServidores(filtro);
  const totalPaginas = query.data
    ? Math.max(1, Math.ceil(query.data.total / query.data.tamanho))
    : 1;

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setFiltro({ termo: termo.trim(), situacao, regime, cargoId: '', pagina: 1 });
  }

  function irParaPagina(pagina: number): void {
    setFiltro((atual) => ({ ...atual, pagina }));
  }

  const columns: Column<ServidorResumo>[] = [
    {
      key: 'matricula',
      header: 'Matrícula',
      sortAccessor: (s) => s.matricula,
      render: (s) => <span className="text-semi-bold">{s.matricula}</span>,
    },
    {
      key: 'nome',
      header: 'Servidor',
      sortAccessor: (s) => s.nomeServidor,
      render: (s) => s.nomeServidor,
    },
    { key: 'cpf', header: 'CPF', render: (s) => s.cpf },
    {
      key: 'regime',
      header: 'Regime',
      sortAccessor: (s) => s.regime,
      render: (s) => formatarRegimePrev(s.regime),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (s) => s.situacao,
      render: (s) => (
        <Tag variant={situacaoServidorTagVariant(s.situacao)}>{s.situacao}</Tag>
      ),
    },
    {
      key: 'nomeacao',
      header: 'Nomeação',
      align: 'end',
      sortAccessor: (s) => s.dataNomeacao,
      render: (s) => formatarData(s.dataNomeacao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (s) => (
        <Link
          className="br-button secondary small"
          to={`/recursoshumanos/servidores/${encodeURIComponent(s.id)}/ficha`}
        >
          Ficha funcional
        </Link>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Servidores"
        description="Pesquise o quadro de pessoal do órgão por nome, matrícula, situação ou regime."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-user-plus" aria-hidden="true" /> Admitir servidor
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
              <div className="col-sm-12 col-md-6">
                <FormField label="Termo">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={termo}
                      onChange={(e) => setTermo(e.target.value)}
                      placeholder="Nome (trecho) ou matrícula (completa)"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-6 col-md-3">
                <FormField label="Situação">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      value={situacao}
                      onChange={(e) => setSituacao(e.target.value)}
                      options={[
                        { value: '', label: 'Todas' },
                        ...SITUACOES_SERVIDOR_FILTRO,
                      ]}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-6 col-md-3">
                <FormField label="Regime">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      value={regime}
                      onChange={(e) => setRegime(e.target.value)}
                      options={[{ value: '', label: 'Todos' }, ...REGIMES_SERVIDOR_FILTRO]}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Resultado da busca de servidores"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(s) => s.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-users"
            title="Nenhum servidor encontrado"
            description="Ajuste os filtros de busca ou admita um novo servidor."
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
            Página {filtro.pagina} de {totalPaginas} ({query.data.total} servidores)
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

      <AdmitirServidorFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
