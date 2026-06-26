// Tela de LISTA (consulta ListarDispensasPorSituacao). Filtro por situação,
// DataTable com colunas ordenáveis + estados loading/vazio/erro, link para o
// detalhe e abertura do formulário de abertura de dispensa (mutation).
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
import { useDispensasPorSituacao } from './dispensa.api';
import type { DispensaResumo, SituacaoDispensa } from './dispensa.api';
import { FUNDAMENTO_LABEL, SITUACAO_LABEL, SITUACAO_OPTIONS, situacaoTagVariant } from './dispensa.helpers';
import { DispensaFormModal } from './DispensaFormModal';
import { AdministracaoSubNav } from '../AdministracaoSubNav';

export function DispensaListPage() {
  const [situacao, setSituacao] = useState<SituacaoDispensa>('Aberta');
  const [formAberto, setFormAberto] = useState(false);

  const query = useDispensasPorSituacao(situacao);

  const columns: Column<DispensaResumo>[] = [
    {
      key: 'objeto',
      header: 'Objeto',
      sortAccessor: (d) => d.objeto.toLowerCase(),
      render: (d) => <Link to={`/administracao/dispensas/${d.id}`}>{d.objeto}</Link>,
    },
    {
      key: 'fundamento',
      header: 'Fundamento',
      sortAccessor: (d) => d.fundamento,
      render: (d) => FUNDAMENTO_LABEL[d.fundamento],
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (d) => d.situacao,
      render: (d) => <Tag variant={situacaoTagVariant(d.situacao)}>{SITUACAO_LABEL[d.situacao]}</Tag>,
    },
    {
      key: 'valor',
      header: 'Valor estimado',
      align: 'end',
      sortAccessor: (d) => d.valorTotalEstimado,
      render: (d) => formatarMoeda(d.valorTotalEstimado),
    },
    {
      key: 'aviso',
      header: 'Aviso',
      render: (d) => d.numeroAviso ?? '—',
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (d) => (
        <Link className="br-button secondary small" to={`/administracao/dispensas/${d.id}`}>
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
        title="Dispensa eletrônica"
        description="Contratação direta por dispensa em razão do valor, na forma eletrônica (Lei 14.133/2021, art. 75, I/II; IN SEGES/ME 67/2021)."
        actions={
          <Can permission="administracao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Abrir dispensa
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
                  onChange={(e) => setSituacao(e.target.value as SituacaoDispensa)}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption={`Dispensas na situação ${SITUACAO_LABEL[situacao]}`}
        columns={columns}
        rows={query.data}
        rowKey={(d) => d.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-folder-open"
            title="Nenhuma dispensa encontrada"
            description={`Não há dispensas na situação "${SITUACAO_LABEL[situacao]}".`}
          />
        }
      />

      <DispensaFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
