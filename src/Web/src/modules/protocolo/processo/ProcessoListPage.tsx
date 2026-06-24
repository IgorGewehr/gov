// Tela de LISTA/CONSULTA de Processos por setor (query ListarProcessosDoSetor).
// Busca sob demanda (enabled), DataTable com ordenação + estados loading/vazio/erro,
// filtro por situação no cliente, link para o detalhe (por NUP) e abertura do
// formulário de autuação (command AutuarProcesso).
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
} from '../../../components/ui';
import type { Column, SelectOption } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import { useProcessosDoSetor } from './processo.api';
import type { ProcessoResumo, SituacaoProcesso } from './processo.api';
import { SITUACAO_LABEL, situacaoTagVariant } from './processo.helpers';
import { ProcessoFormModal } from './ProcessoFormModal';
import { ProtocoloSubNav } from '../ProtocoloSubNav';

const FILTRO_SITUACAO_OPCOES: SelectOption[] = [
  { value: '', label: 'Todas as situações' },
  { value: 'Autuado', label: 'Autuado' },
  { value: 'EmTramitacao', label: 'Em tramitação' },
  { value: 'Sobrestado', label: 'Sobrestado' },
  { value: 'Arquivado', label: 'Arquivado' },
];

export function ProcessoListPage() {
  const [setorId, setSetorId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [filtroSituacao, setFiltroSituacao] = useState<'' | SituacaoProcesso>('');
  const [formAberto, setFormAberto] = useState(false);

  const query = useProcessosDoSetor(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(setorId.trim());
  }

  const linhas = useMemo<ProcessoResumo[] | undefined>(() => {
    if (!query.data) return query.data;
    if (filtroSituacao === '') return query.data;
    return query.data.filter((p) => p.situacao === filtroSituacao);
  }, [query.data, filtroSituacao]);

  const columns: Column<ProcessoResumo>[] = [
    {
      key: 'nup',
      header: 'NUP',
      sortAccessor: (p) => p.nup,
      render: (p) => (
        <Link to={`/protocolo/processos/${encodeURIComponent(p.nup)}`}>{p.nup}</Link>
      ),
    },
    {
      key: 'classificacao',
      header: 'Classificação',
      sortAccessor: (p) => p.classificacao,
      render: (p) => p.classificacao,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (p) => p.situacao,
      render: (p) => <Tag variant={situacaoTagVariant(p.situacao)}>{SITUACAO_LABEL[p.situacao]}</Tag>,
    },
    {
      key: 'autuacao',
      header: 'Autuação',
      sortAccessor: (p) => p.dataAutuacao,
      render: (p) => formatarData(p.dataAutuacao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (p) => (
        <Link className="br-button tertiary small" to={`/protocolo/processos/${encodeURIComponent(p.nup)}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Protocolo"
        title="Processos administrativos"
        description="Consulte os processos por setor responsável e acompanhe o trâmite do PAE."
        actions={
          <Can permission="protocolo.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Autuar processo
              </Button>
            </Toolbar>
          </Can>
        }
      />
      <ProtocoloSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" disabled={setorId.trim() === ''} loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-md">
                <FormField label="Setor responsável (identificador)" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={setorId}
                      onChange={(e) => setSetorId(e.target.value)}
                      placeholder="00000000-0000-0000-0000-000000000000"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-auto">
                <FormField label="Filtrar por situação">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      options={FILTRO_SITUACAO_OPCOES}
                      value={filtroSituacao}
                      onChange={(e) => setFiltroSituacao(e.target.value as '' | SituacaoProcesso)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o identificador do setor responsável e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Processos do setor ${consultaAtiva}`}
          columns={columns}
          rows={linhas}
          rowKey={(p) => p.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhum processo encontrado"
              description="Este setor não possui processos na situação selecionada."
            />
          }
        />
      )}

      <ProcessoFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        setorIdParaInvalidar={consultaAtiva}
      />
    </>
  );
}
