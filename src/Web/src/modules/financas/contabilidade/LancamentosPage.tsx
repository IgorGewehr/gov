// Lançamentos contábeis. O backend (M2) não expõe um GET de lista de lançamentos; esta tela
// concentra o registro de lançamento manual (POST /financas/contabilidade/lancamentos, gated)
// e lista as contas analíticas (lançáveis), cada uma com link para o seu Razão — onde os
// lançamentos do exercício aparecem mês a mês.
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  Toolbar,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { usePlanoDeContas } from './contabilidade.api';
import type { ContaContabil } from './contabilidade.api';
import { contaEhAnalitica } from './contabilidade.helpers';
import { FinancasSubNav } from '../FinancasSubNav';
import { LancamentoManualModal } from './LancamentoManualModal';

export function LancamentosPage() {
  const query = usePlanoDeContas();
  const [aberto, setAberto] = useState(false);

  const contasAnaliticas = useMemo(
    () => (query.data ?? []).filter(contaEhAnalitica),
    [query.data],
  );

  const columns: Column<ContaContabil>[] = [
    {
      key: 'codigo',
      header: 'Conta',
      sortAccessor: (c) => c.codigo,
      render: (c) => (
        <Link to={`/financas/contabilidade/contas/${c.id}/razao`}>
          <span className="text-mono">{c.codigo}</span> — {c.titulo}
        </Link>
      ),
    },
    {
      key: 'naturezaSaldo',
      header: 'Natureza do saldo',
      sortAccessor: (c) => c.naturezaSaldo,
      render: (c) => c.naturezaSaldo,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (c) => (
        <Link className="br-button tertiary small" to={`/financas/contabilidade/contas/${c.id}/razao`}>
          <i className="fas fa-book" aria-hidden="true" /> Ver razão
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Lançamentos contábeis"
        description="Registre lançamentos manuais e acesse o razão de cada conta analítica."
        actions={
          <Can permission="financas.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setAberto(true)} disabled={contasAnaliticas.length === 0}>
                <i className="fas fa-plus" aria-hidden="true" /> Novo lançamento manual
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <FinancasSubNav />

      <Card>
        <DataTable
          caption="Contas analíticas (lançáveis)"
          columns={columns}
          rows={contasAnaliticas}
          rowKey={(c) => c.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-book"
              title="Nenhuma conta analítica"
              description="Semeie o plano de contas para habilitar o registro de lançamentos."
            />
          }
        />
      </Card>

      <LancamentoManualModal open={aberto} onClose={() => setAberto(false)} contas={contasAnaliticas} />
    </>
  );
}
