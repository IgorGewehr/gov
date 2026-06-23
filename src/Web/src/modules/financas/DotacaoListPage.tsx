// Lista de Dotações Orçamentárias por exercício (GET /dotacoes?exercicio=). Filtro de
// exercício, DataTable com estados, link para detalhe e abertura do formulário de criação.
import { useState } from 'react';
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
  Tag,
  Toolbar,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { useDotacoesPorExercicio } from './financas.api';
import type { DotacaoResumo } from './financas.api';
import { exercicioCorrente, situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';
import { DotacaoFormModal } from './DotacaoFormModal';

export function DotacaoListPage() {
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [consultaAtiva, setConsultaAtiva] = useState<number | null>(null);
  const [formAberto, setFormAberto] = useState(false);

  const query = useDotacoesPorExercicio(consultaAtiva ?? 0, consultaAtiva !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const valor = Number(exercicio);
    if (Number.isInteger(valor) && valor >= 2000) setConsultaAtiva(valor);
  }

  const columns: Column<DotacaoResumo>[] = [
    {
      key: 'classificacao',
      header: 'Classificação',
      sortAccessor: (d) => d.classificacao,
      render: (d) => <Link to={`/financas/dotacoes/${d.id}`}>{d.classificacao}</Link>,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (d) => d.situacao,
      render: (d) => <Tag variant={situacaoTagVariant(d.situacao)}>{d.situacao}</Tag>,
    },
    {
      key: 'atualizado',
      header: 'Valor atualizado',
      align: 'end',
      sortAccessor: (d) => d.valorAtualizado,
      render: (d) => formatarMoeda(d.valorAtualizado),
    },
    {
      key: 'saldo',
      header: 'Saldo disponível',
      align: 'end',
      sortAccessor: (d) => d.saldoDisponivel,
      render: (d) => formatarMoeda(d.saldoDisponivel),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (d) => (
        <Link className="br-button tertiary small" to={`/financas/dotacoes/${d.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Dotações orçamentárias"
        description="Créditos orçamentários por exercício (Lei 4.320/64). Base do ciclo da despesa."
        actions={
          <Can permission="financas.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Criar dotação
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <FormField label="Exercício" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="2000"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={exercicio}
                  onChange={(e) => setExercicio(e.target.value)}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {consultaAtiva === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o exercício e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Dotações do exercício ${consultaAtiva}`}
          columns={columns}
          rows={query.data}
          rowKey={(d) => d.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhuma dotação encontrada"
              description="Não há dotações cadastradas para o exercício informado."
            />
          }
        />
      )}

      <DotacaoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
