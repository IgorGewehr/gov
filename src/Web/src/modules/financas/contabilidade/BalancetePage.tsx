// Balancete de verificação por período (GET /financas/contabilidade/balancete?exercicio=&mes=).
// Seletor exercício/mês; tabela conta · saldo anterior · débitos · créditos · saldo atual.
// Destaca quando ΣDébitos ≠ ΣCréditos (partidas dobradas desbalanceadas = erro).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Select,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarMoeda } from '../../../i18n/format';
import { exercicioCorrente } from '../financas.helpers';
import { FinancasSubNav } from '../FinancasSubNav';
import { useBalancete } from './contabilidade.api';
import type { LinhaBalancete } from './contabilidade.api';
import {
  MESES,
  balanceteDesbalanceado,
  totalCreditos,
  totalDebitos,
} from './contabilidade.helpers';

interface Consulta {
  exercicio: number;
  mes: number;
}

export function BalancetePage() {
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [consulta, setConsulta] = useState<Consulta | null>(null);

  const query = useBalancete(consulta?.exercicio ?? 0, consulta?.mes ?? 0, consulta !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ex = Number(exercicio);
    const m = Number(mes);
    if (Number.isInteger(ex) && ex >= 2000 && Number.isInteger(m) && m >= 1 && m <= 12) {
      setConsulta({ exercicio: ex, mes: m });
    }
  }

  const desbalanceado = balanceteDesbalanceado(query.data);
  const somaDebitos = totalDebitos(query.data);
  const somaCreditos = totalCreditos(query.data);

  const columns: Column<LinhaBalancete>[] = [
    {
      key: 'conta',
      header: 'Conta',
      sortAccessor: (l) => l.codigoConta,
      render: (l) => (
        <Link to={`/financas/contabilidade/contas/${l.contaId}/razao`}>
          <span className="text-mono">{l.codigoConta}</span> — {l.titulo}
        </Link>
      ),
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

  return (
    <>
      <PageHeader
        title="Balancete de verificação"
        description="Saldos e movimentos por conta no período. Em partidas dobradas, ΣDébitos deve igualar ΣCréditos."
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
            <div className="col-12 col-md-auto">
              <FormField label="Mês" required>
                {({ id, describedBy, invalid }) => (
                  <Select
                    id={id}
                    options={MESES}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={mes}
                    onChange={(e) => setMes(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto">
              <div className="tg-form-row-acao">
                <Button variant="primary" type="submit" loading={query.isFetching}>
                  Consultar
                </Button>
              </div>
            </div>
          </div>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o exercício e o mês e clique em Consultar."
        />
      ) : (
        <>
          {desbalanceado && (
            <Alert variant="danger" title="Balancete desbalanceado">
              A soma dos débitos ({formatarMoeda(somaDebitos)}) difere da soma dos créditos (
              {formatarMoeda(somaCreditos)}). Verifique os lançamentos do período.
            </Alert>
          )}

          <DataTable
            caption={`Balancete de ${consulta.mes}/${consulta.exercicio}`}
            columns={columns}
            rows={query.data}
            rowKey={(l) => l.contaId}
            loading={query.isLoading}
            error={query.isError ? errorMessage(query.error) : null}
            empty={
              <EmptyState
                icon="fas fa-folder-open"
                title="Sem movimento no período"
                description="Não há lançamentos contábeis no exercício/mês informado."
              />
            }
          />

          {query.data && query.data.length > 0 && (
            <Card className="mt-3">
              <dl className="row mb-0">
                <dt className="col-sm-3 text-gray-60">Total de débitos</dt>
                <dd className="col-sm-3 text-semi-bold mb-2">{formatarMoeda(somaDebitos)}</dd>
                <dt className="col-sm-3 text-gray-60">Total de créditos</dt>
                <dd className="col-sm-3 text-semi-bold mb-2">{formatarMoeda(somaCreditos)}</dd>
              </dl>
            </Card>
          )}
        </>
      )}
    </>
  );
}
