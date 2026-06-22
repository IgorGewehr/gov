// Tela de LISTA/CONSULTA (Query 6.1 — ListarDocumentosDoProcesso).
// Consulta por processoId (sob demanda), DataTable com colunas + ordenacao + filtro de
// situacao no cliente, estados loading/vazio/erro e abertura do formulario de juntada.
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Select,
  Tag,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import { useDocumentosDoProcesso } from './documento.api';
import type { DocumentoResumo } from './documento.api';
import {
  criticidadeTagVariant,
  rotuloTipoAssinatura,
  situacaoTagVariant,
} from './documento.helpers';
import { DocumentoFormModal } from './DocumentoFormModal';

const FILTRO_SITUACAO = [
  { value: '', label: 'Todas as situacoes' },
  { value: 'Rascunho', label: 'Rascunho' },
  { value: 'Juntado', label: 'Juntado' },
  { value: 'Assinado', label: 'Assinado' },
  { value: 'SemEfeito', label: 'Sem efeito' },
];

export function DocumentoListPage() {
  const [processoId, setProcessoId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [filtroSituacao, setFiltroSituacao] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const query = useDocumentosDoProcesso(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(processoId.trim());
  }

  const linhas = useMemo(() => {
    const dados = query.data ?? [];
    return filtroSituacao === '' ? dados : dados.filter((d) => d.situacao === filtroSituacao);
  }, [query.data, filtroSituacao]);

  const columns: Column<DocumentoResumo>[] = [
    {
      key: 'situacao',
      header: 'Situacao',
      sortAccessor: (d) => d.situacao,
      render: (d) => <Tag variant={situacaoTagVariant(d.situacao)}>{d.situacao}</Tag>,
    },
    {
      key: 'criticidade',
      header: 'Criticidade',
      sortAccessor: (d) => d.criticidade,
      render: (d) => <Tag variant={criticidadeTagVariant(d.criticidade)}>{d.criticidade}</Tag>,
    },
    {
      key: 'assinatura',
      header: 'Assinatura',
      sortAccessor: (d) => d.tipoAssinatura ?? '',
      render: (d) => rotuloTipoAssinatura(d.tipoAssinatura),
    },
    {
      key: 'juntada',
      header: 'Data de juntada',
      sortAccessor: (d) => d.dataJuntada ?? '',
      render: (d) => (d.dataJuntada ? formatarData(d.dataJuntada) : '—'),
    },
    {
      key: 'hash',
      header: 'Hash (SHA-256)',
      render: (d) => (
        <code className="text-down-02" title={d.hash}>
          {d.hash.slice(0, 12)}…
        </code>
      ),
    },
    {
      key: 'acoes',
      header: 'Acoes',
      render: (d) => (
        <Link
          className="br-button tertiary small"
          to={`/protocolo/processos/${consultaAtiva}/documentos/${d.id}`}
        >
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Documentos do processo"
        description="Consulte os documentos juntados a um processo administrativo eletronico (PAE)."
        actions={
          <Can permission="protocolo.gerenciar">
            <Button
              variant="primary"
              onClick={() => setFormAberto(true)}
              disabled={consultaAtiva === ''}
            >
              <i className="fas fa-plus" aria-hidden="true" /> Juntar documento
            </Button>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Identificador do processo" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={processoId}
                    onChange={(e) => setProcessoId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button
                variant="primary"
                type="submit"
                disabled={processoId.trim() === ''}
                loading={query.isFetching}
              >
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faca uma consulta"
          description="Informe o identificador do processo e clique em Consultar."
        />
      ) : (
        <>
          <div className="row align-items-end mb-3">
            <div className="col-sm-4">
              <FormField label="Filtrar por situacao">
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    options={FILTRO_SITUACAO}
                    value={filtroSituacao}
                    onChange={(e) => setFiltroSituacao(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>

          <DataTable
            caption={`Documentos do processo ${consultaAtiva}`}
            columns={columns}
            rows={linhas}
            rowKey={(d) => d.id}
            loading={query.isLoading}
            error={query.isError ? errorMessage(query.error) : null}
            empty={
              <EmptyState
                icon="fas fa-folder-open"
                title="Nenhum documento encontrado"
                description="Este processo nao possui documentos juntados para o filtro atual."
              />
            }
          />
        </>
      )}

      <DocumentoFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        processoIdInicial={consultaAtiva}
      />
    </>
  );
}
