// Tela de LISTA/CONSULTA de Familias por territorio (query ObterFamiliasDoTerritorio).
// Busca sob demanda (enabled), DataTable com colunas ordenaveis + filtro de situacao
// client-side, estados loading/vazio/erro, link para detalhe e abertura do formulario
// de referenciamento (command ReferenciarFamilia). gov.br DS + WCAG AA + i18n/format.
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
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useFamiliasDoTerritorio, SITUACAO_LABEL, situacaoTagVariant } from './familia.api';
import type { FamiliaResumo, SituacaoFamilia } from './familia.api';
import { FamiliaFormModal } from './FamiliaFormModal';
import { AssistenciaSocialSubNav } from '../AssistenciaSocialSubNav';
import { Can } from '../../../auth/Can';

const FILTRO_SITUACAO_OPCOES = [
  { value: '', label: 'Todas as situações' },
  { value: 'Referenciada', label: SITUACAO_LABEL.Referenciada },
  { value: 'AtualizacaoVencida', label: SITUACAO_LABEL.AtualizacaoVencida },
  { value: 'Regularizada', label: SITUACAO_LABEL.Regularizada },
];

export function FamiliaListPage() {
  const [territorio, setTerritorio] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [filtroSituacao, setFiltroSituacao] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const query = useFamiliasDoTerritorio(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(territorio.trim());
  }

  const linhasFiltradas = useMemo(() => {
    if (!query.data) return query.data;
    if (filtroSituacao === '') return query.data;
    return query.data.filter((f) => f.situacao === (filtroSituacao as SituacaoFamilia));
  }, [query.data, filtroSituacao]);

  const columns: Column<FamiliaResumo>[] = [
    {
      key: 'nis',
      header: 'NIS (mascarado)',
      sortAccessor: (f) => f.nisMascarado,
      render: (f) => f.nisMascarado,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (f) => f.situacao,
      render: (f) => <Tag variant={situacaoTagVariant(f.situacao)}>{SITUACAO_LABEL[f.situacao]}</Tag>,
    },
    {
      key: 'rendaPerCapita',
      header: 'Renda per capita',
      align: 'end',
      sortAccessor: (f) => f.rendaPerCapita,
      render: (f) => formatarMoeda(f.rendaPerCapita),
    },
    {
      key: 'referenciamento',
      header: 'Referenciamento',
      sortAccessor: (f) => f.dataReferenciamento,
      render: (f) => formatarData(f.dataReferenciamento),
    },
    {
      key: 'atualizacao',
      header: 'Última atualização',
      sortAccessor: (f) => f.dataUltimaAtualizacaoCadastral,
      render: (f) => formatarData(f.dataUltimaAtualizacaoCadastral),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (f) => (
        <Link
          className="br-button tertiary small"
          to={`/assistenciasocial/familias/${f.id}`}
          state={{ familia: f }}
        >
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <AssistenciaSocialSubNav />
      <PageHeader
        eyebrow="Assistência Social"
        title="Famílias (SUAS)"
        description="Consulte as famílias referenciadas por território (CRAS) e gerencie a gestão socioeconômica."
        actions={
          <Can permission="assistenciasocial.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Referenciar família
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button
                variant="primary"
                type="submit"
                disabled={territorio.trim() === ''}
                loading={query.isFetching}
              >
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm">
                <FormField label="Território de cobertura (CRAS)" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={territorio}
                      onChange={(e) => setTerritorio(e.target.value)}
                      placeholder="Ex.: Território Centro"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-auto">
                <FormField label="Situação">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      options={FILTRO_SITUACAO_OPCOES}
                      value={filtroSituacao}
                      onChange={(e) => setFiltroSituacao(e.target.value)}
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
          description="Informe o território de cobertura do CRAS e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Famílias do território ${consultaAtiva}`}
          columns={columns}
          rows={linhasFiltradas}
          rowKey={(f) => f.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-users"
              title="Nenhuma família encontrada"
              description="Não há famílias referenciadas neste território para o filtro selecionado."
            />
          }
        />
      )}

      <FamiliaFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        territorioInicial={consultaAtiva}
      />
    </>
  );
}
