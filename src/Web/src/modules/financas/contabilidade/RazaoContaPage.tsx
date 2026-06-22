// Razão de uma conta contábil num exercício (GET /financas/contabilidade/contas/{contaId}/razao
// ?exercicio=). Mostra o movimento mês a mês (saldo anterior · débitos · créditos · saldo atual)
// da conta selecionada. Acessível via links do Plano de Contas, Balancete e Lançamentos.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarMoeda } from '../../../i18n/format';
import { exercicioCorrente } from '../financas.helpers';
import { FinancasSubNav } from '../FinancasSubNav';
import { useRazaoConta } from './contabilidade.api';
import type { LinhaBalancete } from './contabilidade.api';
import { MESES } from './contabilidade.helpers';

function nomeMes(indice: number): string {
  return MESES[indice]?.label ?? String(indice + 1);
}

export function RazaoContaPage() {
  const { contaId = '' } = useParams<{ contaId: string }>();
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [consulta, setConsulta] = useState<number>(exercicioCorrente());

  const query = useRazaoConta(contaId, consulta, contaId !== '');
  const codigoConta = query.data?.[0]?.codigoConta;
  const tituloConta = query.data?.[0]?.titulo;

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ex = Number(exercicio);
    if (Number.isInteger(ex) && ex >= 2000) setConsulta(ex);
  }

  const columns: Column<LinhaBalancete & { mes: number }>[] = [
    {
      key: 'mes',
      header: 'Mês',
      sortAccessor: (l) => l.mes,
      render: (l) => nomeMes(l.mes - 1),
    },
    {
      key: 'saldoAnterior',
      header: 'Saldo anterior',
      align: 'end',
      sortAccessor: (l) => l.saldoAnterior,
      render: (l) => formatarMoeda(l.saldoAnterior),
    },
    {
      key: 'debitos',
      header: 'Débitos',
      align: 'end',
      sortAccessor: (l) => l.totalDebitos,
      render: (l) => formatarMoeda(l.totalDebitos),
    },
    {
      key: 'creditos',
      header: 'Créditos',
      align: 'end',
      sortAccessor: (l) => l.totalCreditos,
      render: (l) => formatarMoeda(l.totalCreditos),
    },
    {
      key: 'saldoAtual',
      header: 'Saldo atual',
      align: 'end',
      sortAccessor: (l) => l.saldoAtual,
      render: (l) => formatarMoeda(l.saldoAtual),
    },
  ];

  // O razão retorna uma linha por mês; numeramos pela ordem para exibir o período.
  const linhas = (query.data ?? []).map((l, i) => ({ ...l, mes: i + 1 }));

  return (
    <>
      <PageHeader
        title="Razão da conta"
        description={
          codigoConta ? `${codigoConta} — ${tituloConta}` : 'Movimento mensal da conta no exercício.'
        }
        actions={
          <Link className="br-button secondary" to="/financas/contabilidade/plano-de-contas">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Plano de contas
          </Link>
        }
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col-12 col-md-auto">
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
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      <DataTable
        caption={`Razão da conta no exercício ${consulta}`}
        columns={columns}
        rows={linhas}
        rowKey={(l) => `${l.contaId}-${l.mes}`}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-book"
            title="Sem movimento"
            description="Não há lançamentos para esta conta no exercício informado."
          />
        }
      />
    </>
  );
}
