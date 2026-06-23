// Painel de EVENTOS do eSocial (módulo RecursosHumanos). Lista os eventos do tenant
// com filtro por estado/tipo e badge de estado; permite GERAR (tabela/servidor/folha),
// ASSINAR (A1 via Cofre), TRANSMITIR o lote e consultar RETORNOS. Padrão-ouro: QueryState
// + DataTable; ações mutáveis sob Can(gerenciar). A transmissão real depende de credenciais
// de homologação — por ora o gateway é SIMULADO (deixado explícito na UI).
import { useMemo, useState } from 'react';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  PageHeader,
  QueryState,
  Select,
  Tag,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { PERM_RH_GERENCIAR } from './recursosHumanos.helpers';
import {
  useAssinarEvento,
  useConsultarRetornos,
  useEventosESocial,
  useTransmitirEventos,
} from './esocial.api';
import type { EventoESocial } from './esocial.api';
import {
  estadoEventoTagVariant,
  FILTRO_ESTADOS,
  FILTRO_TIPOS,
  podeAssinar,
  rotuloEstado,
  rotuloTipoEvento,
} from './esocial.helpers';
import { RhSubNav } from './RhSubNav';
import { GerarEventoTabelaModal } from './GerarEventoTabelaModal';
import { GerarEventoServidorModal } from './GerarEventoServidorModal';
import { GerarPeriodicosFolhaModal } from './GerarPeriodicosFolhaModal';
import { EventoXmlModal } from './EventoXmlModal';

type ModalGeracao = 'tabela' | 'servidor' | 'folha' | null;

export function ESocialPainelPage() {
  const toast = useToast();
  const query = useEventosESocial();
  const assinar = useAssinarEvento();
  const transmitir = useTransmitirEventos();
  const retornos = useConsultarRetornos();

  const [filtroEstado, setFiltroEstado] = useState('');
  const [filtroTipo, setFiltroTipo] = useState('');
  const [modal, setModal] = useState<ModalGeracao>(null);
  const [xmlEvento, setXmlEvento] = useState<EventoESocial | null>(null);

  const eventos = useMemo(() => {
    const todos = query.data ?? [];
    return todos.filter(
      (e) =>
        (filtroEstado === '' || e.estado === filtroEstado) &&
        (filtroTipo === '' || e.tipo === filtroTipo),
    );
  }, [query.data, filtroEstado, filtroTipo]);

  const totalAssinados = useMemo(
    () => (query.data ?? []).filter((e) => e.estado === 'Assinado').length,
    [query.data],
  );

  function aoErro(error: unknown, padrao: string): void {
    toast.error(error instanceof ApiError ? error.userMessage : padrao);
  }

  function aoAssinar(id: string): void {
    assinar.mutate(id, {
      onSuccess: () => toast.success('Evento assinado (A1).', 'Sucesso'),
      onError: (e) => aoErro(e, 'Não foi possível assinar o evento.'),
    });
  }

  function aoTransmitir(): void {
    transmitir.mutate(undefined, {
      onSuccess: (r) => toast.success(`${r.transmitidos} evento(s) transmitido(s).`, 'Lote enviado'),
      onError: (e) => aoErro(e, 'Não foi possível transmitir o lote.'),
    });
  }

  function aoConsultarRetornos(): void {
    retornos.mutate(undefined, {
      onSuccess: (r) => toast.success(`${r.processados} retorno(s) processado(s).`, 'Retornos'),
      onError: (e) => aoErro(e, 'Não foi possível consultar os retornos.'),
    });
  }

  const columns: Column<EventoESocial>[] = [
    { key: 'tipo', header: 'Tipo', render: (e) => rotuloTipoEvento(e.tipo) },
    {
      key: 'estado',
      header: 'Estado',
      render: (e) => (
        <Tag variant={estadoEventoTagVariant(e.estado)}>{rotuloEstado(e.estado)}</Tag>
      ),
    },
    { key: 'protocolo', header: 'Protocolo', render: (e) => e.protocoloLote ?? '—' },
    { key: 'recibo', header: 'Recibo', render: (e) => e.numeroRecibo ?? '—' },
    {
      key: 'acoes',
      header: 'Ações',
      render: (e) => (
        <div className="d-flex" style={{ gap: '0.25rem' }}>
          <Button variant="tertiary" className="small" onClick={() => setXmlEvento(e)}>
            XML
          </Button>
          <Can permission={PERM_RH_GERENCIAR}>
            {podeAssinar(e.estado) && (
              <Button
                variant="secondary"
                className="small"
                onClick={() => aoAssinar(e.id)}
                loading={assinar.isPending && assinar.variables === e.id}
              >
                Assinar
              </Button>
            )}
          </Can>
        </div>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        title="eSocial"
        description="Gere, assine, transmita e acompanhe os eventos do eSocial (leiautes S-1.3)."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
              <Button variant="secondary" onClick={() => setModal('tabela')}>
                Gerar tabela
              </Button>
              <Button variant="secondary" onClick={() => setModal('servidor')}>
                Gerar servidor
              </Button>
              <Button variant="primary" onClick={() => setModal('folha')}>
                Gerar da folha
              </Button>
            </div>
          </Can>
        }
      />

      <Alert variant="warning" title="Transmissão em modo de homologação.">
        A transmissão real ao eSocial depende de credenciais de <strong>Produção Restrita
        (homologação)</strong> do ente. Por ora o gateway é <strong>simulado</strong>: os lotes
        recebem protocolo/recibo fictícios e <strong>nenhum envio tem efeito jurídico</strong>.
      </Alert>

      <Can permission={PERM_RH_GERENCIAR}>
        <Card className="mb-4" header={<strong>Lote e retornos</strong>}>
          <p className="mb-3">
            Eventos assinados aguardando transmissão: <strong>{totalAssinados}</strong>.
          </p>
          <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
            <Button
              variant="primary"
              onClick={aoTransmitir}
              loading={transmitir.isPending}
              disabled={totalAssinados === 0}
            >
              Transmitir lote ({totalAssinados})
            </Button>
            <Button variant="secondary" onClick={aoConsultarRetornos} loading={retornos.isPending}>
              Consultar retornos
            </Button>
          </div>
        </Card>
      </Can>

      <Card className="mb-4">
        <div className="row align-items-end">
          <div className="col-sm-6 col-md-4">
            <FormField label="Estado">
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={filtroEstado}
                  onChange={(e) => setFiltroEstado(e.target.value)}
                  options={FILTRO_ESTADOS}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6 col-md-4">
            <FormField label="Tipo de evento">
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  value={filtroTipo}
                  onChange={(e) => setFiltroTipo(e.target.value)}
                  options={FILTRO_TIPOS}
                />
              )}
            </FormField>
          </div>
        </div>
      </Card>

      <QueryState<EventoESocial[]>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={eventos}
        empty={
          <EmptyState
            icon="fas fa-file-export"
            title="Nenhum evento eSocial"
            description="Gere eventos de tabela, de servidor ou da folha para começar."
          />
        }
      >
        {(linhas) => (
          <Card>
            <DataTable
              caption="Eventos eSocial do ente"
              columns={columns}
              rows={linhas}
              rowKey={(e) => e.id}
            />
          </Card>
        )}
      </QueryState>

      <GerarEventoTabelaModal open={modal === 'tabela'} onClose={() => setModal(null)} />
      <GerarEventoServidorModal open={modal === 'servidor'} onClose={() => setModal(null)} />
      <GerarPeriodicosFolhaModal open={modal === 'folha'} onClose={() => setModal(null)} />
      <EventoXmlModal evento={xmlEvento} onClose={() => setXmlEvento(null)} />
    </>
  );
}
