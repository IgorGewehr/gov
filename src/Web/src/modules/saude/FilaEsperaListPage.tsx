// Tela da FILA DE ESPERA — pacientes aguardando vaga (ordem de convocação), com inclusão
// na fila, convocação e remoção. Endpoint REAL GET /saude/fila-espera?estabelecimento&
// situacao&pagina&tamanho (ResultadoPaginado<FilaEsperaItem>), gated em "saude.agenda.ver";
// ações em "saude.agenda.marcar". LGPD: a consulta é sensível (gera trilha de acesso).
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
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
import { formatarDataHora } from '../../i18n/format';
import { useBuscarFilaDeEspera } from './api';
import type { FilaBuscaFiltro, FilaEsperaItem } from './api';
import {
  filaAguardando,
  filaRemovivel,
  opcoesSituacaoFila,
  prioridadeAgendamentoVariant,
  situacaoFilaVariant,
} from './saude.helpers';
import { ConvocarFilaModal, RemoverFilaModal } from './FilaEsperaModals';
import { EntrarNaFilaModal } from './EntrarNaFilaModal';
import { SaudeSubNav } from './SaudeSubNav';

const TAMANHO_PAGINA = 20;

type Acao = 'convocar' | 'remover';

export function FilaEsperaListPage() {
  const [estabelecimentoCampo, setEstabelecimentoCampo] = useState('');
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [estabelecimento, setEstabelecimento] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);

  const [entrarAberto, setEntrarAberto] = useState(false);
  const [acaoAtual, setAcaoAtual] = useState<{ acao: Acao; id: string } | null>(null);

  const filtro = useMemo<FilaBuscaFiltro>(
    () => ({
      estabelecimento: estabelecimento || undefined,
      situacao: situacao || undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [estabelecimento, situacao, pagina],
  );

  const query = useBuscarFilaDeEspera(filtro);

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setEstabelecimento(estabelecimentoCampo.trim());
    setSituacao(situacaoCampo);
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const columns: Column<FilaEsperaItem>[] = [
    {
      key: 'entrada',
      header: 'Entrada',
      sortAccessor: (f) => f.dataEntrada,
      render: (f) => formatarDataHora(f.dataEntrada),
    },
    { key: 'tipo', header: 'Natureza', sortAccessor: (f) => f.tipo, render: (f) => f.tipo },
    {
      key: 'destino',
      header: 'Profissional / Especialidade',
      render: (f) => f.especialidade || (f.profissionalId ? 'Profissional específico' : '—'),
    },
    {
      key: 'prioridade',
      header: 'Prioridade',
      sortAccessor: (f) => f.prioridade,
      render: (f) => <Tag variant={prioridadeAgendamentoVariant(f.prioridade)}>{f.prioridade}</Tag>,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (f) => f.situacao,
      render: (f) => <Tag variant={situacaoFilaVariant(f.situacao)}>{f.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (f) => (
        <Can permission="saude.agenda.marcar">
          <Toolbar>
            {filaAguardando(f.situacao) && (
              <Button
                variant="primary"
                className="small"
                onClick={() => setAcaoAtual({ acao: 'convocar', id: f.id })}
              >
                Convocar
              </Button>
            )}
            {filaRemovivel(f.situacao) && (
              <Button
                variant="secondary"
                className="small"
                onClick={() => setAcaoAtual({ acao: 'remover', id: f.id })}
              >
                Remover
              </Button>
            )}
          </Toolbar>
        </Can>
      ),
    },
  ];

  return (
    <>
      <SaudeSubNav />
      <PageHeader
        eyebrow="Saúde"
        title="Fila de espera"
        description="Pacientes aguardando vaga, ordenados por prioridade e ordem de entrada. Acesso registrado (LGPD)."
        actions={
          <Can permission="saude.agenda.marcar">
            <Toolbar>
              <Button variant="primary" onClick={() => setEntrarAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Incluir na fila
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
                Filtrar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-md-8">
                <FormField label="Estabelecimento (CNES)" help="Identificador do estabelecimento.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={estabelecimentoCampo}
                      onChange={(e) => setEstabelecimentoCampo(e.target.value)}
                      placeholder="Filtrar por estabelecimento"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-4">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoFila}
                      placeholder="Todas"
                      value={situacaoCampo}
                      onChange={(e) => setSituacaoCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Fila de espera"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(f) => f.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-people-line"
            title="Fila vazia"
            description="Não há pacientes na fila com os filtros selecionados."
          />
        }
      />

      {totalPaginas > 1 && (
        <nav
          className="d-flex align-items-center justify-content-between mt-3"
          aria-label="Paginação da fila de espera"
        >
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} na fila
          </span>
          <div className="d-flex">
            <Button
              variant="secondary"
              className="small"
              disabled={pagina <= 1 || query.isFetching}
              onClick={() => setPagina((p) => Math.max(1, p - 1))}
            >
              <i className="fas fa-chevron-left" aria-hidden="true" /> Anterior
            </Button>
            <Button
              variant="secondary"
              className="small ml-2"
              disabled={pagina >= totalPaginas || query.isFetching}
              onClick={() => setPagina((p) => Math.min(totalPaginas, p + 1))}
            >
              Próxima <i className="fas fa-chevron-right" aria-hidden="true" />
            </Button>
          </div>
        </nav>
      )}

      <EntrarNaFilaModal open={entrarAberto} onClose={() => setEntrarAberto(false)} />
      <ConvocarFilaModal
        open={acaoAtual?.acao === 'convocar'}
        filaId={acaoAtual?.id ?? ''}
        onClose={() => setAcaoAtual(null)}
      />
      <RemoverFilaModal
        open={acaoAtual?.acao === 'remover'}
        filaId={acaoAtual?.id ?? ''}
        onClose={() => setAcaoAtual(null)}
      />
    </>
  );
}
