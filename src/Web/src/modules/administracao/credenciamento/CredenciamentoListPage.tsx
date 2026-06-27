// Tela de LISTA (consulta ListarCredenciamentos). Filtro por situação (ou todas),
// DataTable com colunas ordenáveis + estados loading/vazio/erro, link para o
// detalhe e abertura do formulário de criação de edital (mutation).
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
import { formatarData } from '../../../i18n/format';
import { useCredenciamentos } from './credenciamento.api';
import type { CredenciamentoResumo, SituacaoCredenciamento } from './credenciamento.api';
import { HIPOTESE_LABEL, SITUACAO_LABEL, FILTRO_SITUACAO_OPTIONS, situacaoTagVariant } from './credenciamento.helpers';
import { CredenciamentoFormModal } from './CredenciamentoFormModal';
import { AdministracaoSubNav } from '../AdministracaoSubNav';

type FiltroSituacao = SituacaoCredenciamento | 'Todas';

export function CredenciamentoListPage() {
  const [situacao, setSituacao] = useState<FiltroSituacao>('Todas');
  const [formAberto, setFormAberto] = useState(false);

  const query = useCredenciamentos(situacao);

  const columns: Column<CredenciamentoResumo>[] = [
    {
      key: 'objeto',
      header: 'Objeto',
      sortAccessor: (c) => c.objeto.toLowerCase(),
      render: (c) => <Link to={`/administracao/credenciamentos/${c.id}`}>{c.objeto}</Link>,
    },
    {
      key: 'hipotese',
      header: 'Hipótese (art. 79)',
      sortAccessor: (c) => c.hipotese,
      render: (c) => HIPOTESE_LABEL[c.hipotese],
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (c) => c.situacao,
      render: (c) => <Tag variant={situacaoTagVariant(c.situacao)}>{SITUACAO_LABEL[c.situacao]}</Tag>,
    },
    {
      key: 'edital',
      header: 'Edital',
      render: (c) => c.numeroEdital ?? '—',
    },
    {
      key: 'credenciados',
      header: 'Credenciados aptos',
      align: 'end',
      sortAccessor: (c) => c.quantidadeCredenciadosAptos,
      render: (c) => c.quantidadeCredenciadosAptos,
    },
    {
      key: 'vigenciaFim',
      header: 'Vigência até',
      sortAccessor: (c) => c.vigenciaFim,
      render: (c) => formatarData(c.vigenciaFim),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (c) => (
        <Link className="br-button secondary small" to={`/administracao/credenciamentos/${c.id}`}>
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
        title="Credenciamento"
        description="Forma auxiliar de contratação por chamamento público permanente, processada por inexigibilidade (Lei 14.133/2021, art. 78, I e art. 79; art. 74, IV)."
        actions={
          <Can permission="administracao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Abrir credenciamento
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
                  options={FILTRO_SITUACAO_OPTIONS}
                  value={situacao}
                  onChange={(e) => setSituacao(e.target.value as FiltroSituacao)}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Editais de credenciamento"
        columns={columns}
        rows={query.data}
        rowKey={(c) => c.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-folder-open"
            title="Nenhum credenciamento encontrado"
            description="Não há editais de credenciamento na situação selecionada."
          />
        }
      />

      <CredenciamentoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
