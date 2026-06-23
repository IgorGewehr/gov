// Tela de LISTA (consulta ListarLicitacoesPorSituacao). Filtro por situação,
// DataTable com colunas ordenáveis + estados loading/vazio/erro, link para o
// detalhe e abertura do formulário de abertura de licitação (mutation).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  PageHeader,
  Select,
  Tag,
  Toolbar,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import { useLicitacoesPorSituacao } from './licitacao.api';
import type { LicitacaoResumo, SituacaoLicitacao } from './licitacao.api';
import {
  MODALIDADE_LABEL,
  SITUACAO_LABEL,
  SITUACAO_OPTIONS,
  situacaoTagVariant,
} from './licitacao.helpers';
import { LicitacaoFormModal } from './LicitacaoFormModal';
import { AdministracaoSubNav } from '../AdministracaoSubNav';

export function LicitacaoListPage() {
  const [situacao, setSituacao] = useState<SituacaoLicitacao>('Aberta');
  const [formAberto, setFormAberto] = useState(false);

  const query = useLicitacoesPorSituacao(situacao);

  const columns: Column<LicitacaoResumo>[] = [
    {
      key: 'objeto',
      header: 'Objeto',
      sortAccessor: (l) => l.objeto.toLowerCase(),
      render: (l) => (
        <Link to={`/administracao/licitacoes/${l.id}`}>{l.objeto}</Link>
      ),
    },
    {
      key: 'modalidade',
      header: 'Modalidade',
      sortAccessor: (l) => l.modalidade,
      render: (l) => MODALIDADE_LABEL[l.modalidade],
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (l) => l.situacao,
      render: (l) => <Tag variant={situacaoTagVariant(l.situacao)}>{SITUACAO_LABEL[l.situacao]}</Tag>,
    },
    {
      key: 'valor',
      header: 'Valor estimado',
      align: 'end',
      sortAccessor: (l) => l.valorEstimado,
      render: (l) => formatarMoeda(l.valorEstimado),
    },
    {
      key: 'pncp',
      header: 'Edital PNCP',
      render: (l) => l.numeroEditalPncp ?? '—',
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (l) => (
        <Link className="br-button secondary small" to={`/administracao/licitacoes/${l.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <AdministracaoSubNav />
      <PageHeader
        eyebrow="Compras e Licitações"
        title="Licitações"
        description="Procedimentos de seleção competitiva sob a Lei 14.133/2021 (NLLC)."
        actions={
          <Can permission="administracao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Abrir licitação
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={(e) => e.preventDefault()}>
          <FormRow
            acao={
              <Button variant="secondary" onClick={() => query.refetch()} loading={query.isFetching}>
                <i className="fas fa-rotate" aria-hidden="true" /> Atualizar
              </Button>
            }
          >
            <FormField label="Filtrar por situação">
              {({ id, describedBy }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  options={SITUACAO_OPTIONS}
                  value={situacao}
                  onChange={(e) => setSituacao(e.target.value as SituacaoLicitacao)}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption={`Licitações na situação ${SITUACAO_LABEL[situacao]}`}
        columns={columns}
        rows={query.data}
        rowKey={(l) => l.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-folder-open"
            title="Nenhuma licitação encontrada"
            description={`Não há licitações na situação "${SITUACAO_LABEL[situacao]}".`}
          />
        }
      />

      <LicitacaoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
