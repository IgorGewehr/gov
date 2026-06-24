// Extrato de uma conta da tesouraria (GET /financas/tesouraria/contas/{id}/extrato). Mostra
// recebimentos, pagamentos e transferências com saldo após cada movimento; permite registrar
// movimento e conciliar manualmente (casamento com o extrato bancário; import OFX = M10).
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
  Tag,
  Toolbar,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { FinancasSubNav } from '../FinancasSubNav';
import { useExtratoConta, useConciliarMovimento, hojeIso } from './tesouraria.api';
import type { MovimentoFinanceiro } from './tesouraria.api';
import { MovimentoModal } from './MovimentoModal';

function tipoLabel(tipo: string): string {
  switch (tipo) {
    case 'Recebimento':
      return 'Recebimento';
    case 'Pagamento':
      return 'Pagamento';
    case 'TransferenciaEntrada':
      return 'Transf. recebida';
    case 'TransferenciaSaida':
      return 'Transf. enviada';
    default:
      return tipo;
  }
}

function ehEntrada(tipo: string): boolean {
  return tipo === 'Recebimento' || tipo === 'TransferenciaEntrada';
}

export function ExtratoContaPage() {
  const { contaId = '' } = useParams<{ contaId: string }>();
  const [de, setDe] = useState('');
  const [ate, setAte] = useState('');
  const [filtroDe, setFiltroDe] = useState<string | undefined>(undefined);
  const [filtroAte, setFiltroAte] = useState<string | undefined>(undefined);
  const [modal, setModal] = useState<'recebimento' | 'pagamento' | null>(null);

  const query = useExtratoConta(contaId, filtroDe, filtroAte, contaId !== '');
  const conciliar = useConciliarMovimento(contaId);

  function filtrar(event: FormEvent): void {
    event.preventDefault();
    setFiltroDe(de || undefined);
    setFiltroAte(ate || undefined);
  }

  const columns: Column<MovimentoFinanceiro>[] = [
    { key: 'data', header: 'Data', sortAccessor: (m) => m.data, render: (m) => formatarData(m.data) },
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (m) => m.tipo,
      render: (m) => <Tag variant={ehEntrada(m.tipo) ? 'success' : 'warning'}>{tipoLabel(m.tipo)}</Tag>,
    },
    { key: 'historico', header: 'Histórico', render: (m) => m.historico },
    { key: 'documento', header: 'Documento', render: (m) => m.documento ?? '—' },
    {
      key: 'valor',
      header: 'Valor',
      align: 'end',
      sortAccessor: (m) => m.valor,
      render: (m) => (ehEntrada(m.tipo) ? formatarMoeda(m.valor) : `- ${formatarMoeda(m.valor)}`),
    },
    {
      key: 'saldoApos',
      header: 'Saldo após',
      align: 'end',
      sortAccessor: (m) => m.saldoApos,
      render: (m) => formatarMoeda(m.saldoApos),
    },
    {
      key: 'conciliado',
      header: 'Conciliação',
      render: (m) =>
        m.conciliado ? (
          <Tag variant="success">Conciliado</Tag>
        ) : (
          <Can permission="financas.gerenciar">
            <Button
              variant="tertiary"
              onClick={() =>
                conciliar.mutate({ movimentoId: m.movimentoId, dataConciliacao: hojeIso() })
              }
              loading={conciliar.isPending}
            >
              Conciliar
            </Button>
          </Can>
        ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Extrato da conta"
        description="Movimentos da conta da tesouraria com saldo corrente."
        actions={
          <Can permission="financas.gerenciar">
            <Toolbar>
              <Button variant="secondary" onClick={() => setModal('pagamento')}>
                <i className="fas fa-arrow-up" aria-hidden="true" /> Pagamento
              </Button>
              <Button variant="primary" onClick={() => setModal('recebimento')}>
                <i className="fas fa-arrow-down" aria-hidden="true" /> Recebimento
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={filtrar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Filtrar
              </Button>
            }
          >
            <FormField label="De">
              {({ id, describedBy }) => (
                <Input id={id} type="date" aria-describedby={describedBy} value={de}
                  onChange={(e) => setDe(e.target.value)} />
              )}
            </FormField>
            <FormField label="Até">
              {({ id, describedBy }) => (
                <Input id={id} type="date" aria-describedby={describedBy} value={ate}
                  onChange={(e) => setAte(e.target.value)} />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Extrato"
        columns={columns}
        rows={query.data ?? []}
        rowKey={(m) => m.movimentoId}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-receipt"
            title="Sem movimento"
            description="Nenhum movimento nesta conta no período informado."
          />
        }
      />

      <div className="mt-2">
        <Link className="br-button secondary" to="/financas/tesouraria/contas">
          <i className="fas fa-arrow-left" aria-hidden="true" /> Contas
        </Link>
      </div>

      <MovimentoModal
        open={modal !== null}
        onClose={() => setModal(null)}
        contaId={contaId}
        tipo={modal ?? 'recebimento'}
      />
    </>
  );
}
