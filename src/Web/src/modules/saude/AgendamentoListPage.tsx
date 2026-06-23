// Tela da AGENDA — lista de agendamentos (consultas/exames) por data/situação, com ações
// do ciclo de vida (confirmar/cancelar/falta/realizar) e marcação de nova consulta.
// Endpoint REAL GET /saude/agendamentos?paciente&profissional&data&situacao&pagina&tamanho
// (ResultadoPaginado<AgendamentoItemLista>), gated em "saude.agenda.ver"; ações em
// "saude.agenda.marcar". LGPD: a consulta é sensível (gera trilha de acesso no backend).
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
import { useBuscarAgendamentos } from './api';
import type { AgendamentoBuscaFiltro, AgendamentoItemLista } from './api';
import {
  agendamentoAtivo,
  agendamentoMarcado,
  opcoesSituacaoAgendamento,
  prioridadeAgendamentoVariant,
  situacaoAgendamentoVariant,
} from './saude.helpers';
import { MarcarAgendamentoModal } from './MarcarAgendamentoModal';
import { AbrirAgendaModal } from './AbrirAgendaModal';
import {
  CancelarAgendamentoModal,
  ConfirmarAgendamentoModal,
  RealizarAgendamentoModal,
  RegistrarFaltaModal,
} from './AgendamentoAcaoModals';
import { SaudeSubNav } from './SaudeSubNav';

const TAMANHO_PAGINA = 20;

type Acao = 'confirmar' | 'cancelar' | 'falta' | 'realizar';

export function AgendamentoListPage() {
  const [dataCampo, setDataCampo] = useState('');
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [data, setData] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);

  const [marcarAberto, setMarcarAberto] = useState(false);
  const [abrirAgendaAberto, setAbrirAgendaAberto] = useState(false);
  const [acaoAtual, setAcaoAtual] = useState<{ acao: Acao; id: string } | null>(null);

  const filtro = useMemo<AgendamentoBuscaFiltro>(
    () => ({
      data: data || undefined,
      situacao: situacao || undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [data, situacao, pagina],
  );

  const query = useBuscarAgendamentos(filtro);

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setData(dataCampo);
    setSituacao(situacaoCampo);
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const columns: Column<AgendamentoItemLista>[] = [
    {
      key: 'dataHora',
      header: 'Data/hora',
      sortAccessor: (a) => a.dataHora,
      render: (a) => formatarDataHora(a.dataHora),
    },
    { key: 'tipo', header: 'Natureza', sortAccessor: (a) => a.tipo, render: (a) => a.tipo },
    {
      key: 'prioridade',
      header: 'Prioridade',
      sortAccessor: (a) => a.prioridade,
      render: (a) => <Tag variant={prioridadeAgendamentoVariant(a.prioridade)}>{a.prioridade}</Tag>,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (a) => a.situacao,
      render: (a) => <Tag variant={situacaoAgendamentoVariant(a.situacao)}>{a.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (a) => (
        <Can permission="saude.agenda.marcar">
          <Toolbar>
            {agendamentoMarcado(a.situacao) && (
              <Button
                variant="secondary"
                className="small"
                onClick={() => setAcaoAtual({ acao: 'confirmar', id: a.id })}
              >
                Confirmar
              </Button>
            )}
            {agendamentoAtivo(a.situacao) && (
              <>
                <Button
                  variant="primary"
                  className="small"
                  onClick={() => setAcaoAtual({ acao: 'realizar', id: a.id })}
                >
                  Realizar
                </Button>
                <Button
                  variant="secondary"
                  className="small"
                  onClick={() => setAcaoAtual({ acao: 'falta', id: a.id })}
                >
                  Falta
                </Button>
                <Button
                  variant="secondary"
                  className="small"
                  onClick={() => setAcaoAtual({ acao: 'cancelar', id: a.id })}
                >
                  Cancelar
                </Button>
              </>
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
        title="Agenda"
        description="Consultas e exames agendados. Acesso registrado em trilha de auditoria (LGPD)."
        actions={
          <Toolbar>
            <Can permission="saude.agenda.gerenciar">
              <Button variant="secondary" onClick={() => setAbrirAgendaAberto(true)}>
                <i className="fas fa-calendar-plus" aria-hidden="true" /> Abrir agenda
              </Button>
            </Can>
            <Can permission="saude.agenda.marcar">
              <Button variant="primary" onClick={() => setMarcarAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Marcar consulta
              </Button>
            </Can>
          </Toolbar>
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
              <div className="col-12 col-md-6">
                <FormField label="Data do atendimento">
                  {({ id }) => (
                    <Input
                      id={id}
                      type="date"
                      value={dataCampo}
                      onChange={(e) => setDataCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-6">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoAgendamento}
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
        caption="Agendamentos"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(a) => a.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-calendar-day"
            title="Nenhum agendamento encontrado"
            description="Ajuste os filtros ou marque uma nova consulta."
          />
        }
      />

      {totalPaginas > 1 && (
        <nav
          className="d-flex align-items-center justify-content-between mt-3"
          aria-label="Paginação de agendamentos"
        >
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} agendamento(s)
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

      <MarcarAgendamentoModal open={marcarAberto} onClose={() => setMarcarAberto(false)} />
      <AbrirAgendaModal open={abrirAgendaAberto} onClose={() => setAbrirAgendaAberto(false)} />

      <ConfirmarAgendamentoModal
        open={acaoAtual?.acao === 'confirmar'}
        agendamentoId={acaoAtual?.id ?? ''}
        onClose={() => setAcaoAtual(null)}
      />
      <RealizarAgendamentoModal
        open={acaoAtual?.acao === 'realizar'}
        agendamentoId={acaoAtual?.id ?? ''}
        onClose={() => setAcaoAtual(null)}
      />
      <RegistrarFaltaModal
        open={acaoAtual?.acao === 'falta'}
        agendamentoId={acaoAtual?.id ?? ''}
        onClose={() => setAcaoAtual(null)}
      />
      <CancelarAgendamentoModal
        open={acaoAtual?.acao === 'cancelar'}
        agendamentoId={acaoAtual?.id ?? ''}
        onClose={() => setAcaoAtual(null)}
      />
    </>
  );
}
