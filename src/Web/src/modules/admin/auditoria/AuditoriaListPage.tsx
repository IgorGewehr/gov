// Visualizador da Trilha de Auditoria (somente admin — permissão
// "admin.auditoria.ver"). Tabela paginada (GET /api/admin/auditoria) com filtros
// por entidade, usuário e ação, e expand por linha para inspecionar oldValues /
// newValues em JSON formatado/legível (auditoria imutável — CLAUDE.md §6).
// Segue o PADRÃO-OURO de protocolo/ProcessoListPage.tsx (form de filtro em Card,
// estados loading/vazio/erro, paginação 1-based) e o gating de auth/Can.tsx.
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Tag,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarDataHora } from '../../../i18n/format';
import { useHasPermission } from '../../../auth/Can';
import { PERM_AUDITORIA_VER } from '../admin.permissoes';
import { useAuditoria } from './auditoria.api';
import type { AuditoriaFiltro, AuditoriaItem } from './auditoria.api';
import { acaoLabel, acaoTagVariant, formatarJson } from './auditoria.helpers';

const TAMANHO_PAGINA = 20;

interface FiltrosForm {
  entidade: string;
  usuario: string;
  acao: string;
}

const FILTROS_VAZIOS: FiltrosForm = { entidade: '', usuario: '', acao: '' };

export function AuditoriaListPage() {
  const podeVer = useHasPermission(PERM_AUDITORIA_VER);

  const [campos, setCampos] = useState<FiltrosForm>(FILTROS_VAZIOS);
  const [aplicados, setAplicados] = useState<FiltrosForm>(FILTROS_VAZIOS);
  const [pagina, setPagina] = useState(1);
  const [expandido, setExpandido] = useState<string | null>(null);

  const filtro = useMemo<AuditoriaFiltro>(
    () => ({
      entidade: aplicados.entidade || undefined,
      usuario: aplicados.usuario || undefined,
      acao: aplicados.acao || undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [aplicados, pagina],
  );

  const query = useAuditoria(filtro, podeVer);

  function aplicarFiltros(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setExpandido(null);
    setAplicados({
      entidade: campos.entidade.trim(),
      usuario: campos.usuario.trim(),
      acao: campos.acao.trim(),
    });
  }

  function limpar(): void {
    setCampos(FILTROS_VAZIOS);
    setAplicados(FILTROS_VAZIOS);
    setPagina(1);
    setExpandido(null);
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const columns: Column<AuditoriaItem>[] = [
    {
      key: 'timestamp',
      header: 'Data/Hora',
      sortAccessor: (i) => i.timestampUtc,
      render: (i) => formatarDataHora(i.timestampUtc),
    },
    {
      key: 'usuario',
      header: 'Usuário',
      sortAccessor: (i) => i.userId ?? '',
      render: (i) => i.userId ?? '—',
    },
    {
      key: 'acao',
      header: 'Ação',
      sortAccessor: (i) => i.action,
      render: (i) => <Tag variant={acaoTagVariant(i.action)}>{acaoLabel(i.action)}</Tag>,
    },
    {
      key: 'entidade',
      header: 'Entidade',
      sortAccessor: (i) => i.entityName,
      render: (i) => (
        <span>
          {i.entityName}
          <span className="text-down-01 d-block text-secondary">{i.entityId}</span>
        </span>
      ),
    },
    {
      key: 'detalhes',
      header: 'Detalhes',
      align: 'end',
      render: (i) => (
        <Button
          variant="tertiary"
          className="small"
          aria-expanded={expandido === i.id}
          aria-controls={`auditoria-detalhe-${i.id}`}
          onClick={() => setExpandido((atual) => (atual === i.id ? null : i.id))}
        >
          <i className={`fas ${expandido === i.id ? 'fa-chevron-up' : 'fa-chevron-down'}`} aria-hidden="true" />{' '}
          {expandido === i.id ? 'Ocultar' : 'Ver valores'}
        </Button>
      ),
    },
  ];

  if (!podeVer) {
    return (
      <>
        <PageHeader title="Trilha de auditoria" description="Histórico imutável de alterações do órgão." />
        <Alert variant="warning">
          Você não possui permissão para consultar a trilha de auditoria.
        </Alert>
      </>
    );
  }

  const item = query.data?.itens.find((i) => i.id === expandido) ?? null;
  const oldJson = item ? formatarJson(item.oldValues) : null;
  const newJson = item ? formatarJson(item.newValues) : null;

  return (
    <>
      <PageHeader
        title="Trilha de auditoria"
        description="Histórico imutável de alterações do órgão (quem, quando, o quê) — para o Tribunal de Contas."
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={aplicarFiltros}>
          <div className="row align-items-end">
            <div className="col-12 col-md">
              <FormField label="Entidade">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={campos.entidade}
                    onChange={(e) => setCampos((c) => ({ ...c, entidade: e.target.value }))}
                    placeholder="Ex.: Processo"
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md">
              <FormField label="Usuário">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={campos.usuario}
                    onChange={(e) => setCampos((c) => ({ ...c, usuario: e.target.value }))}
                    placeholder="Identificador do usuário"
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md">
              <FormField label="Ação">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={campos.acao}
                    onChange={(e) => setCampos((c) => ({ ...c, acao: e.target.value }))}
                    placeholder="Ex.: Modified"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3 d-flex">
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Filtrar
              </Button>
              <Button variant="tertiary" type="button" className="ml-2" onClick={limpar}>
                Limpar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      <DataTable
        caption="Registros da trilha de auditoria"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(i) => i.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-clipboard-list"
            title="Nenhum registro encontrado"
            description="Não há registros de auditoria para os filtros informados."
          />
        }
      />

      {item && (
        <div id={`auditoria-detalhe-${item.id}`} className="mt-3">
          <Card>
          <h2 className="text-up-01 text-semi-bold mb-3">
            Valores do registro — {item.entityName} ({acaoLabel(item.action)})
          </h2>
          {item.ipAddress && (
            <p className="text-down-01 text-secondary mb-3">Origem (IP): {item.ipAddress}</p>
          )}
          <div className="row">
            <div className="col-12 col-md-6 mb-3">
              <h3 className="text-base text-semi-bold mb-1">Valores anteriores</h3>
              {oldJson ? (
                <pre className="bg-gray-2 p-3 text-down-01" style={{ overflowX: 'auto' }}>
                  {oldJson}
                </pre>
              ) : (
                <p className="text-secondary">Sem valores anteriores.</p>
              )}
            </div>
            <div className="col-12 col-md-6 mb-3">
              <h3 className="text-base text-semi-bold mb-1">Valores posteriores</h3>
              {newJson ? (
                <pre className="bg-gray-2 p-3 text-down-01" style={{ overflowX: 'auto' }}>
                  {newJson}
                </pre>
              ) : (
                <p className="text-secondary">Sem valores posteriores.</p>
              )}
            </div>
          </div>
          </Card>
        </div>
      )}

      {totalPaginas > 1 && (
        <nav className="d-flex align-items-center justify-content-between mt-3" aria-label="Paginação da auditoria">
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} registro(s)
          </span>
          <div className="d-flex">
            <Button
              variant="secondary"
              className="small"
              disabled={pagina <= 1 || query.isFetching}
              onClick={() => {
                setExpandido(null);
                setPagina((p) => Math.max(1, p - 1));
              }}
            >
              <i className="fas fa-chevron-left" aria-hidden="true" /> Anterior
            </Button>
            <Button
              variant="secondary"
              className="small ml-2"
              disabled={pagina >= totalPaginas || query.isFetching}
              onClick={() => {
                setExpandido(null);
                setPagina((p) => Math.min(totalPaginas, p + 1));
              }}
            >
              Próxima <i className="fas fa-chevron-right" aria-hidden="true" />
            </Button>
          </div>
        </nav>
      )}
    </>
  );
}
