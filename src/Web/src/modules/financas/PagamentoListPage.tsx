// Consulta de Ordem de Pagamento (GET /ordens-pagamento/{id}). O contrato expõe consulta
// por identificador; apresentamos em tabela para uniformidade com o padrão de lista.
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
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useOrdemDePagamento } from './financas.api';
import type { OrdemDePagamentoResumo } from './financas.api';
import { situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';
import { PagamentoFormModal } from './PagamentoFormModal';

export function PagamentoListPage() {
  const [ordemId, setOrdemId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const query = useOrdemDePagamento(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(ordemId.trim());
  }

  const linhas: OrdemDePagamentoResumo[] = query.data ? [query.data] : [];

  const columns: Column<OrdemDePagamentoResumo>[] = [
    {
      key: 'numero',
      header: 'Número',
      sortAccessor: (o) => o.numero,
      render: (o) => <Link to={`/financas/pagamentos/${o.id}`}>{o.numero}</Link>,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (o) => o.situacao,
      render: (o) => <Tag variant={situacaoTagVariant(o.situacao)}>{o.situacao}</Tag>,
    },
    {
      key: 'valor',
      header: 'Valor total',
      align: 'end',
      sortAccessor: (o) => o.valorTotal,
      render: (o) => formatarMoeda(o.valorTotal),
    },
    {
      key: 'data',
      header: 'Data do pagamento',
      sortAccessor: (o) => o.dataPagamento,
      render: (o) => formatarData(o.dataPagamento),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (o) => (
        <Link className="br-button tertiary small" to={`/financas/pagamentos/${o.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Pagamentos"
        description="Ordens de pagamento (3º estágio da despesa, Lei 4.320/64)."
        actions={
          <Can permission="financas.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Emitir ordem
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
              <Button variant="primary" type="submit" disabled={ordemId.trim() === ''} loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <FormField label="Identificador da ordem de pagamento" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={ordemId}
                  onChange={(e) => setOrdemId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o identificador da ordem de pagamento e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Ordem de pagamento ${consultaAtiva}`}
          columns={columns}
          rows={linhas}
          rowKey={(o) => o.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhuma ordem encontrada"
              description="Verifique o identificador informado ou emita uma nova ordem."
            />
          }
        />
      )}

      <PagamentoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
