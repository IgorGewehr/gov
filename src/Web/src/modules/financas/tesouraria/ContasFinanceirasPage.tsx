// Tesouraria caixa-banco — lista de contas com saldo corrente (GET /financas/tesouraria/contas).
// Permite abrir conta, registrar movimentos e transferir entre contas. Coração operacional
// diário do financeiro (Lei 4.320/64). Mantém UMA entrada na Sidebar via FinancasSubNav.
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
  Toolbar,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import { situacaoTagVariant } from '../financas.helpers';
import { FinancasSubNav } from '../FinancasSubNav';
import { useContasFinanceiras } from './tesouraria.api';
import type { ContaFinanceira } from './tesouraria.api';
import { AbrirContaModal } from './AbrirContaModal';
import { TransferenciaModal } from './TransferenciaModal';

export function ContasFinanceirasPage() {
  const [abrirAberto, setAbrirAberto] = useState(false);
  const [transfAberto, setTransfAberto] = useState(false);
  const query = useContasFinanceiras();

  const columns: Column<ContaFinanceira>[] = [
    {
      key: 'nome',
      header: 'Conta',
      sortAccessor: (c) => c.nome,
      render: (c) => <Link to={`/financas/tesouraria/contas/${c.id}`}>{c.nome}</Link>,
    },
    {
      key: 'tipo',
      header: 'Espécie',
      sortAccessor: (c) => c.tipo,
      render: (c) => <Tag variant="info">{c.tipo}</Tag>,
    },
    {
      key: 'banco',
      header: 'Banco/Ag./Conta',
      render: (c) => (c.banco ? `${c.banco} / ${c.agencia} / ${c.conta}` : '—'),
    },
    {
      key: 'saldo',
      header: 'Saldo',
      align: 'end',
      sortAccessor: (c) => c.saldo,
      render: (c) => formatarMoeda(c.saldo),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (c) => c.situacao,
      render: (c) => <Tag variant={situacaoTagVariant(c.situacao)}>{c.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (c) => (
        <Link className="br-button tertiary small" to={`/financas/tesouraria/contas/${c.id}`}>
          Extrato
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Tesouraria — Caixa e Banco"
        description="Contas bancárias e caixas com saldo e movimentação financeira diária."
        actions={
          <Can permission="financas.gerenciar">
            <Toolbar>
              <Button variant="secondary" onClick={() => setTransfAberto(true)}>
                <i className="fas fa-right-left" aria-hidden="true" /> Transferir
              </Button>
              <Button variant="primary" onClick={() => setAbrirAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Abrir conta
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <FinancasSubNav />

      <Card>
        <DataTable
          caption="Contas da tesouraria"
          columns={columns}
          rows={query.data ?? []}
          rowKey={(c) => c.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-building-columns"
              title="Nenhuma conta"
              description="Abra a primeira conta bancária ou caixa da tesouraria."
            />
          }
        />
      </Card>

      <AbrirContaModal open={abrirAberto} onClose={() => setAbrirAberto(false)} />
      <TransferenciaModal
        open={transfAberto}
        onClose={() => setTransfAberto(false)}
        contas={query.data ?? []}
      />
    </>
  );
}
