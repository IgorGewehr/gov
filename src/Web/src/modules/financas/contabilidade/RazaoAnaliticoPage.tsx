// Razão ANALÍTICO de uma conta (GET /financas/contabilidade/contas/{id}/razao-analitico
// ?exercicio=). Extrato lançamento-a-lançamento (não saldos mensais) com saldo acumulado —
// exigência do TCE e da rotina contábil.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { exercicioCorrente } from '../financas.helpers';
import { FinancasSubNav } from '../FinancasSubNav';
import { useRazaoAnalitico } from './contabilidade.api';
import type { LinhaRazaoAnalitico } from './contabilidade.api';

export function RazaoAnaliticoPage() {
  const { contaId = '' } = useParams<{ contaId: string }>();
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [consulta, setConsulta] = useState<number>(exercicioCorrente());
  const query = useRazaoAnalitico(contaId, consulta, contaId !== '');

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ex = Number(exercicio);
    if (Number.isInteger(ex) && ex >= 2000) setConsulta(ex);
  }

  const columns: Column<LinhaRazaoAnalitico & { ordem: number }>[] = [
    { key: 'data', header: 'Data', sortAccessor: (l) => l.data, render: (l) => formatarData(l.data) },
    { key: 'historico', header: 'Histórico', render: (l) => l.historico },
    { key: 'origem', header: 'Origem', render: (l) => l.origem },
    {
      key: 'debito',
      header: 'Débito',
      align: 'end',
      sortAccessor: (l) => l.debito,
      render: (l) => (l.debito > 0 ? formatarMoeda(l.debito) : '—'),
    },
    {
      key: 'credito',
      header: 'Crédito',
      align: 'end',
      sortAccessor: (l) => l.credito,
      render: (l) => (l.credito > 0 ? formatarMoeda(l.credito) : '—'),
    },
    {
      key: 'saldoAcumulado',
      header: 'Saldo acumulado',
      align: 'end',
      sortAccessor: (l) => l.saldoAcumulado,
      render: (l) => formatarMoeda(l.saldoAcumulado),
    },
  ];

  const linhas = (query.data ?? []).map((l, i) => ({ ...l, ordem: i }));

  return (
    <>
      <PageHeader
        title="Razão analítico"
        description="Extrato lançamento-a-lançamento da conta, com saldo acumulado."
        actions={
          <Link className="br-button secondary" to="/financas/contabilidade/plano-de-contas">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Plano de contas
          </Link>
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
                <Input id={id} type="number" min="2000" step="1" inputMode="numeric"
                  aria-describedby={describedBy} invalid={invalid}
                  value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption={`Razão analítico da conta no exercício ${consulta}`}
        columns={columns}
        rows={linhas}
        rowKey={(l) => `${l.lancamentoId}-${l.ordem}`}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-book-open"
            title="Sem lançamentos"
            description="Não há partidas para esta conta no exercício informado."
          />
        }
      />
    </>
  );
}
