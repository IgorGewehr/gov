// Boletim de Caixa/Banco (GET /financas/tesouraria/boletim?data=). Fechamento diário por conta:
// saldo anterior · recebimentos · pagamentos · saldo do dia, com totais consolidados.
import { useState } from 'react';
import type { FormEvent } from 'react';
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
import { FinancasSubNav } from '../FinancasSubNav';
import { useBoletimCaixaBanco, hojeIso } from './tesouraria.api';
import type { LinhaBoletimConta } from './tesouraria.api';

export function BoletimCaixaBancoPage() {
  const [data, setData] = useState(hojeIso());
  const [consulta, setConsulta] = useState(hojeIso());
  const query = useBoletimCaixaBanco(consulta);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    if (data !== '') setConsulta(data);
  }

  const columns: Column<LinhaBoletimConta>[] = [
    { key: 'nome', header: 'Conta', sortAccessor: (l) => l.nome, render: (l) => l.nome },
    { key: 'tipo', header: 'Espécie', render: (l) => l.tipo },
    {
      key: 'saldoAnterior',
      header: 'Saldo anterior',
      align: 'end',
      sortAccessor: (l) => l.saldoAnterior,
      render: (l) => formatarMoeda(l.saldoAnterior),
    },
    {
      key: 'recebimentos',
      header: 'Recebimentos',
      align: 'end',
      sortAccessor: (l) => l.recebimentos,
      render: (l) => formatarMoeda(l.recebimentos),
    },
    {
      key: 'pagamentos',
      header: 'Pagamentos',
      align: 'end',
      sortAccessor: (l) => l.pagamentos,
      render: (l) => formatarMoeda(l.pagamentos),
    },
    {
      key: 'saldoDia',
      header: 'Saldo do dia',
      align: 'end',
      sortAccessor: (l) => l.saldoDia,
      render: (l) => formatarMoeda(l.saldoDia),
    },
  ];

  const b = query.data;

  return (
    <>
      <PageHeader
        title="Boletim de Caixa/Banco"
        description="Fechamento diário da tesouraria por conta (Lei 4.320/64)."
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Gerar boletim
              </Button>
            }
          >
            <FormField label="Data" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid}
                  value={data} onChange={(e) => setData(e.target.value)} />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption={`Boletim de Caixa/Banco — ${formatarData(consulta)}`}
        columns={columns}
        rows={b?.contas ?? []}
        rowKey={(l) => l.contaId}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-clipboard-list"
            title="Sem contas"
            description="Não há contas para consolidar neste dia."
          />
        }
      />

      {b && b.contas.length > 0 && (
        <Card className="mt-4">
          <dl className="row">
            <dt className="col-sm-6">Total saldo anterior</dt>
            <dd className="col-sm-6 text-right">{formatarMoeda(b.totalSaldoAnterior)}</dd>
            <dt className="col-sm-6">Total recebimentos</dt>
            <dd className="col-sm-6 text-right">{formatarMoeda(b.totalRecebimentos)}</dd>
            <dt className="col-sm-6">Total pagamentos</dt>
            <dd className="col-sm-6 text-right">{formatarMoeda(b.totalPagamentos)}</dd>
            <dt className="col-sm-6">
              <strong>Total saldo do dia</strong>
            </dt>
            <dd className="col-sm-6 text-right">
              <strong>{formatarMoeda(b.totalSaldoDia)}</strong>
            </dd>
          </dl>
        </Card>
      )}
    </>
  );
}
